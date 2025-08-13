using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;

namespace HydraEngine.Protection.Calli
{
    public class CallObfuscation : Models.Protection
    {
        public CallObfuscation()
            : base("Protection.Calli.CallObfuscation",
                   "Call Obfuscation",
                   "...")
        {
            ManualReload = true;
        }

        public override async Task<bool> Execute(string moduledef)
        {
            try
            {
                var moduleDefinition = ModuleDefinition.FromFile(moduledef);

                // Asegurarse de que el módulo permita código nativo.
                // Removemos ILOnly y configuramos PEKind y MachineType apropiadamente.
                //module.Attributes &= ~DotNetDirectoryFlags.ILOnly;

                //bool isx86 = module.MachineType == MachineType.I386;
                //if (isx86)
                //{
                //    module.PEKind = OptionalHeaderMagic.PE32;
                //    module.MachineType = MachineType.I386;
                //    module.Attributes |= DotNetDirectoryFlags.Bit32Required;
                //}
                //else
                //{
                //    module.PEKind = OptionalHeaderMagic.PE32Plus;
                //    module.MachineType = MachineType.Amd64;
                //}

                var identifierCache = new Dictionary<IMethodDescriptor, int>();
                int currentIndex = 0;

                var globalConstructor = moduleDefinition.GetOrCreateModuleConstructor();

                var functionPointerArray = CreateFunctionPointerArray(moduleDefinition);

                moduleDefinition.GetOrCreateModuleType().Fields.Add(functionPointerArray);

                var importer = new ReferenceImporter(moduleDefinition);

                if (globalConstructor.CilMethodBody == null)
                {
                    globalConstructor.CilMethodBody = new CilMethodBody(globalConstructor);
                }

                // Remove any exit flow codes used before.
                if (globalConstructor.CilMethodBody.Instructions.Any(i => i.OpCode.Code is CilCode.Ret))
                    foreach (var ret in globalConstructor.CilMethodBody.Instructions
                        .Where(i => i.OpCode.Code is CilCode.Ret).ToArray())
                        globalConstructor.CilMethodBody.Instructions.Remove(ret);

                var getTypeFromHandle = GetCorLibMethod(moduleDefinition,
                    "System", nameof(Type),
                    nameof(Type.GetTypeFromHandle), "System.RuntimeTypeHandle");
                var getModule = GetCorLibMethod(moduleDefinition,
                    "System", nameof(Type),
                    $"get_{nameof(Type.Module)}", Array.Empty<string>());
                var resolveMethod = GetCorLibMethod(moduleDefinition,
                    "System.Reflection", nameof(Module),
                    nameof(Module.ResolveMethod), "System.Int32");
                var getMethodHandle = GetCorLibMethod(moduleDefinition,
                    "System.Reflection", nameof(MethodBase),
                    $"get_{nameof(MethodBase.MethodHandle)}", Array.Empty<string>());
                var getFunctionPointer = GetCorLibMethod(moduleDefinition,
                    "System", nameof(RuntimeMethodHandle),
                    nameof(RuntimeMethodHandle.GetFunctionPointer), Array.Empty<string>());

                var functionPointerType = getFunctionPointer.DeclaringType;
                if (functionPointerType == null)
                    throw new InvalidOperationException("DeclaringType of getFunctionPointer is null.");

                var loadAddress =
                    new CilLocalVariable(importer.ImportTypeSignature(functionPointerType.ToTypeSignature()));
                globalConstructor.CilMethodBody.LocalVariables.Add(loadAddress);

                var methods = moduleDefinition
                    .GetAllTypes()
                    .SelectMany(t => t.Methods)
                    .Where(m => m.CilMethodBody != null)
                    .ToArray();

                foreach (var method in methods)
                {
                    var declaringType = method.DeclaringType;
                    string typeName = declaringType?.Name?.Value ?? string.Empty;
                    string methodName = method.Name?.Value ?? string.Empty;
                    bool hasCompilerGeneratedAttr =
                        method.CustomAttributes.Any(ca => ca.Constructor?.DeclaringType?.FullName ==
                            "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                        || declaringType?.CustomAttributes.Any(ca => ca.Constructor?.DeclaringType?.FullName ==
                            "System.Runtime.CompilerServices.CompilerGeneratedAttribute") == true;
                    bool isAnonToString = methodName == "ToString" && typeName.Contains("<>f__AnonymousType");
                    bool isStateMachineMoveNext = methodName == "MoveNext" && (typeName.Contains("d__") || typeName.Contains("<>c__DisplayClass"));
                    if (hasCompilerGeneratedAttr || isAnonToString || isStateMachineMoveNext || typeName.StartsWith("<>"))
                        continue;
                    if (method.CilMethodBody.ExceptionHandlers.Count > 0)
                        continue;

                    var instructions = method.CilMethodBody.Instructions;
                    instructions.ExpandMacros();
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        var instruction = instructions[i];

                        if (!(instruction.OpCode == CilOpCodes.Call ||
                              instruction.OpCode == CilOpCodes.Callvirt)) continue;

                        if (!(instruction.Operand is IMethodDescriptor methodDescriptor)) continue;

                        if (methodDescriptor is MethodSpecification methodSpec)
                        {
                            continue;
                        }
                        if (methodDescriptor is MethodDefinition methodDef)
                        {
                            continue;
                        }

                        if (methodDescriptor.DeclaringType is TypeSpecification) continue;
                        if (methodDescriptor.Signature != null && methodDescriptor.Signature.IncludeSentinel) continue;

                        var resolvedType = methodDescriptor.DeclaringType?.Resolve();
                        if (resolvedType == null || resolvedType.IsDelegate) continue;

                        if (resolvedType.IsValueType && methodDescriptor.Signature.HasThis) continue;

                        bool hasForbiddenPrefix = false;
                        if (i - 1 >= 0)
                        {
                            var prevInstr = instructions[i - 1];
                            if (prevInstr.OpCode == CilOpCodes.Constrained || prevInstr.OpCode == CilOpCodes.Tailcall)
                                hasForbiddenPrefix = true;
                        }
                        if (hasForbiddenPrefix) continue;

                        if (!identifierCache.ContainsKey(methodDescriptor))
                        {
                            identifierCache[methodDescriptor] = currentIndex++;
                            var arrayStoreExpression = new[] {
                            new CilInstruction(CilOpCodes.Ldsfld, functionPointerArray)
                        }.Concat(MutateI4(identifierCache[methodDescriptor])).Concat(new[] {
                            new CilInstruction(CilOpCodes.Ldtoken, moduleDefinition.GetModuleType()),
                            new CilInstruction(CilOpCodes.Call, importer.ImportMethod(getTypeFromHandle)),
                            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(getModule))
                        }.Concat(MutateI4(methodDescriptor.MetadataToken.ToInt32())).Concat(new[] {
                            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(resolveMethod)),
                            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(getMethodHandle)),
                            new CilInstruction(CilOpCodes.Stloc, loadAddress),
                            new CilInstruction(CilOpCodes.Ldloca, loadAddress),
                            new CilInstruction(CilOpCodes.Call, importer.ImportMethod(getFunctionPointer)),
                            new CilInstruction(CilOpCodes.Stelem_I)
                        }));
                            globalConstructor.CilMethodBody.Instructions.AddRange(arrayStoreExpression);
                        }

                        // Importar la firma del método antes de construir el StandAloneSignature para calli
                        var importedSig = importer.ImportMethodSignature(methodDescriptor.Signature);

                        var calliList = new List<CilInstruction>
                    {
                        new CilInstruction(CilOpCodes.Ldsfld, functionPointerArray)
                    };
                        calliList.AddRange(MutateI4(identifierCache[methodDescriptor]));
                        calliList.Add(new CilInstruction(CilOpCodes.Ldelem_I));
                        calliList.Add(new CilInstruction(CilOpCodes.Calli, importedSig.MakeStandAloneSignature()));

                        var calliExpression = calliList.ToArray();

                        // Insertar calli y neutralizar la instrucción original
                        instructions.InsertRange(i, calliExpression);
                        instruction.OpCode = CilOpCodes.Nop;
                        instruction.Operand = null;
                        i += calliExpression.Length;
                    }
                    instructions.OptimizeMacros();
                }

                globalConstructor.CilMethodBody.Instructions.InsertRange(0,
                    MutateI4(currentIndex).ToArray()
                        .Concat(new[] { new CilInstruction(CilOpCodes.Newarr, moduleDefinition.CorLibTypeFactory.IntPtr.Type) })
                        .Concat(new[] { new CilInstruction(CilOpCodes.Stsfld, functionPointerArray) }));

                globalConstructor.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
                globalConstructor.CilMethodBody.Instructions.OptimizeMacros();
                globalConstructor.CilMethodBody.InitializeLocals = true;

                MemoryStream outputAssembly = new MemoryStream();
                moduleDefinition.Write(outputAssembly, new ManagedPEImageBuilder(MetadataBuilderFlags.PreserveAll));

                TempModule = outputAssembly;

                if (TempModule == null) throw new Exception("MemoryStream is null");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                this.Errors = ex;
                return false;
            }
        }

        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            string TempRenamer = Path.Combine(Path.GetTempPath(), module.Name);
            try { module.Write(TempRenamer); } catch (Exception Ex) { this.Errors = Ex; }

            return Execute(TempRenamer);
        }

        private static FieldDefinition CreateFunctionPointerArray(
            ModuleDefinition moduleDefinition)
        {
            return new FieldDefinition(((char)new Random().Next('a', 'z')).ToString(),
                FieldAttributes.Assembly | FieldAttributes.Static,
                FieldSignature.CreateStatic(moduleDefinition.CorLibTypeFactory.IntPtr.MakeSzArrayType()));
        }

        private static IMethodDescriptor GetCorLibMethod(
            ModuleDefinition moduleDefinition,
            string ns,
            string typename,
            string methodName,
            params string[] parametersFullName)
        {
            var importer = new ReferenceImporter(moduleDefinition);
            var typeRef = new TypeReference(moduleDefinition.CorLibTypeFactory.CorLibScope, ns, typename);

            var resolvedReference = importer.ImportType(typeRef).Resolve();

            if (resolvedReference == null) return null;

            foreach (var method in resolvedReference.Methods)
            {
                if (method.Name != methodName) continue;

                string[] typeNames = method.Parameters.Select(p => p.ParameterType.FullName).ToArray();

                if (!StringEquals(parametersFullName, typeNames)) continue;

                return method;
            }

            return null;

            bool StringEquals(IReadOnlyCollection<string> a, IReadOnlyList<string> b)
            {
                if (a.Count != b.Count) return false;
                return !a.Where((t, x) => t != b[x]).Any();
            }
        }

        private static IEnumerable<CilInstruction> MutateI4(
            int value)
        {
            var expression = new List<CilInstruction>();
            var random = new Random();

            expression.AddRange(Mutate(value));

            foreach (var loadI4 in expression.Where(i => i.IsLdcI4()).ToArray())
            {
                int insertIndex = expression.IndexOf(loadI4);
                expression.InsertRange(insertIndex, Mutate(loadI4.GetLdcI4Constant()));
                expression.Remove(loadI4);
            }

            return expression;

            IEnumerable<CilInstruction> Mutate(int i32Value)
            {
                var instructions = new List<CilInstruction>();
                switch (random.Next(3))
                {
                    case 0:
                        int subI32 = random.Next();
                        instructions.AddRange(new[] {
                            new CilInstruction(CilOpCodes.Ldc_I4, i32Value - subI32),
                            new CilInstruction(CilOpCodes.Ldc_I4, subI32),
                            new CilInstruction(CilOpCodes.Add)
                        });
                        break;

                    case 1:
                        int addI32 = random.Next();
                        instructions.AddRange(new[] {
                            new CilInstruction(CilOpCodes.Ldc_I4, i32Value + addI32),
                            new CilInstruction(CilOpCodes.Ldc_I4, addI32),
                            new CilInstruction(CilOpCodes.Sub)
                        });
                        break;

                    case 2:
                        int xorI32 = random.Next();
                        instructions.AddRange(new[] {
                            new CilInstruction(CilOpCodes.Ldc_I4, i32Value ^ xorI32),
                            new CilInstruction(CilOpCodes.Ldc_I4, xorI32),
                            new CilInstruction(CilOpCodes.Xor)
                        });
                        break;
                }

                return instructions;
            }
        }
    }
}