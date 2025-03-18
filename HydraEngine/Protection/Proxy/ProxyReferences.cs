using Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EXGuard.Core;
using HydraEngine.Protection.Renamer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Proxy
{
    public class ProxyReferences : Models.Protection
    {
        public ProxyReferences() : base("Protection.Proxy.ProxyReferences", "Renamer Phase", "Description for Renamer Phase") { }

        public readonly List<MethodDef> ProxyMethods = new List<MethodDef>();

        public bool Unsafe { get; set; } = false;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {

                //foreach (var type in Module.Types.ToArray())
                //{
                //    if (!AnalyzerPhase.CanRename(type)) continue;
                //    foreach (var method in type.Methods.ToArray())
                //    {
                //        if (!ProxyReferences_Helper.CanObfuscate(method)) continue;
                //        if (ProxyMethods.Contains(method)) continue;

                //        //if (method.IsConstructor) continue;
                //        //if (!method.HasBody || !method.Body.HasInstructions || method.DeclaringType.IsGlobalModuleType) continue;

                //        //if (method.HasGenericParameters) continue;
                //        //if (method.IsPinvokeImpl) continue;
                //        //if (method.IsUnmanagedExport) continue;

                //        if (!AnalyzerPhase.CanRename(method, type)) continue;

                //        //if (method.HasClosureReferences()) continue;

                //        //if (method.Body.Instructions.Any(instr => IsAccessingNonPublicMember(instr, type))) continue;

                //        ProcessMethodInstructions(Module, type, method);
                //    }
                //}

                EXGuard.Core.EXECProtections.RPNormal.Execute(Module);


                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        private bool IsAccessingNonPublicMember(Instruction instr, TypeDef declaringType)
        {
            if (instr.OpCode == OpCodes.Ldfld || instr.OpCode == OpCodes.Ldflda || instr.OpCode == OpCodes.Stfld)
            {
                var field = instr.Operand as IField;
                var fieldDef = field?.ResolveFieldDef();
                if (fieldDef?.DeclaringType == declaringType && !fieldDef.IsPublic)
                    return true;
            }

            if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
            {
                var method = instr.Operand as IMethod;
                var methodDef = method?.ResolveMethodDef();
                if (methodDef?.DeclaringType == declaringType && !methodDef.IsPublic)
                    return true;
            }

            return false;
        }

        public void ProcessMethodInstructions(ModuleDefMD ctx, TypeDef type, MethodDef method)
        {
            foreach (var instruction in method.Body.Instructions.ToArray())
            {
                switch (instruction.OpCode)
                {
                    case OpCode op when op == OpCodes.Newobj:
                        if (Unsafe) ProcessNewObjInstruction(ctx, type, method, instruction);
                        break;
                    case OpCode op when op == OpCodes.Call:
                        ProcessCallInstruction(ctx, type, method, instruction);
                        break;
                }
            }
        }

        private void ProcessNewObjInstruction(ModuleDefMD ctx, TypeDef type, MethodDef method, Instruction instruction)
        {
            if (!(instruction.Operand is IMethodDefOrRef methodDefOrRef)) return;
            if (ShouldSkipMethodProcessing(methodDefOrRef, method)) return;

            var methodDef = ProxyReferences_Helper.GenerateMethod(ctx, methodDefOrRef, method);
            if (methodDef == null) return;

            UpdateMethodMetadata(method, instruction, methodDef);
        }

        private void ProcessCallInstruction(ModuleDefMD ctx, TypeDef type, MethodDef method, Instruction instruction)
        {
            if (!(instruction.Operand is MemberRef methodReference)) return;
            if (ShouldSkipMemberRefProcessing(method, methodReference)) return;

            var methodDef = ProxyReferences_Helper.GenerateMethod(ctx, type, methodReference,
                methodReference.HasThis,
                methodReference.FullName.StartsWith("System.Void"));

            if (methodDef != null)
            {
                UpdateMethodMetadata(method, instruction, methodDef);
            }
        }

        private bool ShouldSkipMethodProcessing(IMethodDefOrRef methodDefOrRef, MethodDef method)
        {
            return methodDefOrRef.IsMethodSpec
                || method.Name == ".ctor" && methodDefOrRef.Name != ".ctor"
                || methodDefOrRef.DeclaringType is TypeSpec
                || methodDefOrRef.MethodSig.ParamsAfterSentinel?.Count > 0
                || method.DeclaringType.IsValueType && methodDefOrRef.MethodSig.HasThis;
        }

        private bool ShouldSkipMemberRefProcessing(MethodDef method, MemberRef methodReference)
        {
            return methodReference.DeclaringType is TypeSpec
                || methodReference.MethodSig.ParamsAfterSentinel?.Count > 0
                || method.DeclaringType.IsValueType && methodReference.MethodSig.HasThis
                || ProxyReferences_Helper._dontObfuscateKeywords.Any(k =>
                    methodReference.Name.Contains(k) ||
                    methodReference.FullName.Contains(k))
                || methodReference.FullName.Contains("bool")
                || methodReference.FullName.Contains("Collections.Generic")
                || methodReference.Name.Contains("ToString")
                || methodReference.FullName.Contains("Thread::Start")
                || methodReference.Name.Contains("Properties.Settings")
                || methodReference.FullName.Contains("System.Boolean")
                || methodReference.FullName.Contains("ctor");
        }

        private void UpdateMethodMetadata(MethodDef originalMethod, Instruction instruction, MethodDef newMethod)
        {
            originalMethod.DeclaringType.Methods.Add(newMethod);
            ProxyMethods.Add(newMethod);
            Protector.usedMethods.Add(newMethod);
            instruction.Operand = newMethod;
            instruction.OpCode = OpCodes.Call;
        }

    }

    public static class ProxyReferences_Helper
    {
        public static readonly HashSet<string> _dontObfuscateKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ToArray", "set_foregroundcolor", "get_conte", "GetTypeFromHandle", "TypeFromHandle",
            "GetFunctionPointer", "get_value", "GetIndex", "set_IgnoreProtocal", "Split", "WithAuthor",
            "Match", "ClearAllHeaders", "Post", "GetChannel", "op_Implicit", "invoke", "get_Task",
            "get_ContentType", "ADD", "op_Equality", "op_Inequality", "Contains", "FreeHGlobal",
            "get_Module", "ResolveMethod", ".ctor", "ReadLine", "Dispose", "Next", "Async", "GetAwaiter",
            "SetException", "Exception", "Enter", "ReadLines", "UnaryOperation", "BinaryOperation",
            "Close", "WithTitle", "Format", "get_Memeber", "set_IgnoreProtocallErrors", "MoveNext",
            "Getinstances", "Build", "Serialize", "Exists", "UseCommandsNext", "Delay"
        };

        public static bool CanObfuscate(MethodDef methodDef)
        {
            return !methodDef.DeclaringType.IsGlobalModuleType
                && !methodDef.Name.Contains("Dispose")
                && methodDef.HasBody
                && methodDef.Body.HasInstructions;
        }

        private static void AddDebugAssert(ModuleDefMD Module, CilBody body)
        {
            var debugAssert = Module.Import(typeof(Debug).GetMethod("Assert", new[] { typeof(bool) }));
            body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, debugAssert));
            body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_1));
        }

        public static MethodDef GenerateMethod(ModuleDefMD ctx, TypeDef declaringType, object targetMethod, bool hasThis = false, bool isVoid = false)
        {
            var methodReference = (MemberRef)targetMethod;
            var methodSig = MethodSig.CreateStatic(methodReference.ReturnType);

            var methodDefinition = new MethodDefUser(
                GGeneration.GenerateGuidStartingWithLetter(),
                methodSig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.FamANDAssem |
                MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.HideBySig)
            {
                Body = new CilBody()
            };

            if (hasThis)
            {
                methodDefinition.MethodSig.Params.Add(
                    declaringType.Module.Import(declaringType.ToTypeSig(true)));
            }

            methodDefinition.MethodSig.Params.AddRange(methodReference.MethodSig.Params);
            methodDefinition.Parameters.UpdateParameterTypes();

            foreach (var parameter in methodDefinition.Parameters)
                methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, parameter));

            methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Call, methodReference));
            methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            AddDebugAssert(ctx, methodDefinition.Body);
            DnlibUtils.EnsureNoInlining(methodDefinition);

            return methodDefinition;
        }

        public static MethodDef GenerateMethod(ModuleDefMD ctx, IMethod targetMethod, MethodDef md)
        {
            var methodSig = MethodSig.CreateStatic(md.Module.Import(targetMethod.DeclaringType.ToTypeSig(true)));

            var methodDef = new MethodDefUser(
                GGeneration.GenerateGuidStartingWithLetter(),
                methodSig,
                MethodImplAttributes.Managed | MethodImplAttributes.IL,
                MethodAttributes.Public | MethodAttributes.FamANDAssem |
                MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.HideBySig)
            {
                IsHideBySig = true,
                Body = new CilBody()
            };

            for (int x = 0; x < targetMethod.MethodSig.Params.Count; x++)
            {
                methodDef.ParamDefs.Add(new ParamDefUser(GGeneration.GenerateGuidStartingWithLetter(), (ushort)(x + 1)));
                methodDef.MethodSig.Params.Add(targetMethod.MethodSig.Params[x]);
            }

            methodDef.Parameters.UpdateParameterTypes();

            foreach (var parameter in methodDef.Parameters)
                methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, parameter));

            methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, targetMethod));
            methodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            AddDebugAssert(ctx, methodDef.Body);
            DnlibUtils.EnsureNoInlining(methodDef);

            return methodDef;
        }
    }

    internal class FixedReferenceProxy
    {

    }

}
