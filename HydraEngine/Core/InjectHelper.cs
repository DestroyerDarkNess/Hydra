using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Configuration.Assemblies;

namespace HydraEngine.Core
{
    public class newInjector
    {
        private List<IDnlibDef> Members { get; set; }
        private Type RuntimeType { get; set; }

        public IDnlibDef FindMember(string name)
        {
            foreach (var member in Members)
                if (member.Name == name)
                    return member;
            throw new Exception("Error to find member.");
        }

        public newInjector(ModuleDefMD module, Type type, bool injectType = true)
        {
            RuntimeType = type;
            Members = new List<IDnlibDef>();
            if (injectType)
                InjectType(module);
        }

        public void InjectType(ModuleDefMD module)
        {
            var typeModule = ModuleDefMD.Load(RuntimeType.Module);
            var typeDefs = typeModule.ResolveTypeDef(MDToken.ToRID(RuntimeType.MetadataToken));
            Members.AddRange(InjectHelper.Inject(typeDefs, module.GlobalType, module).ToList());
        }

        public void injectMethod(string Namespace, string Name, ModuleDefMD module, MethodDef method)
        {
            TypeDef newClass = new TypeDefUser(Namespace, Name, module.CorLibTypes.Object.TypeDefOrRef)
            {
                Attributes = TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass
            };
            module.Types.Add(newClass);
            method.DeclaringType = null;
            newClass.Methods.Add(method);
        }

        public void injectMethods(string Namespace, string Name, ModuleDefMD module, MethodDef[] methods)
        {
            TypeDef newClass = new TypeDefUser(Namespace, Name, module.CorLibTypes.Object.TypeDefOrRef)
            {
                Attributes = TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass
            };
            module.Types.Add(newClass);
            foreach (var m in methods)
            {
                m.DeclaringType = null;
                newClass.Methods.Add(m);
            }
        }

        public void Rename()
        {
            foreach (var mem in Members)
            {
                if (mem is MethodDef method)
                {
                    if (method.HasImplMap)
                        continue;
                    if (method.DeclaringType.IsDelegate)
                        continue;
                }
                mem.Name = string.Format("<{0}>Hydra_{1}", Randomizer.GenerateRandomString(), "Guard");
            }
        }
    }

    public static class InjectHelper
    {
        public static void AddAttributeToType(TypeDef type, ICustomAttributeType attributeType)
        {
            var customAttribute = new CustomAttribute((ICustomAttributeType)attributeType);

            var attributeConstructor = customAttribute.Constructor;

            bool hasAttribute = type.CustomAttributes.Any(attr =>
                attr.Constructor.FullName == attributeConstructor.FullName);

            if (!hasAttribute)
            {
                type.CustomAttributes.Add(customAttribute);
            }
        }

        public static void AddAttributeToMethod(MethodDef method, ICustomAttributeType attributeType)
        {
            var customAttribute = new CustomAttribute((ICustomAttributeType)attributeType);
            AddAttributeToMethod(method, customAttribute);
        }

        public static void AddAttributeToMethod(MethodDef method, CustomAttribute customAttribute)
        {
            var attributeConstructor = customAttribute.Constructor;

            bool hasAttribute = method.CustomAttributes.Any(attr =>
                attr.Constructor.FullName == attributeConstructor.FullName);

            if (!hasAttribute)
            {
                method.CustomAttributes.Add(customAttribute);
            }
        }

        private static TypeDefUser Clone(TypeDef origin)
        {
            var ret = new TypeDefUser(origin.Namespace, origin.Name)
            {
                Attributes = origin.Attributes
            };

            if (origin.ClassLayout != null)
                ret.ClassLayout = new ClassLayoutUser(origin.ClassLayout.PackingSize, origin.ClassSize);

            foreach (var genericParam in origin.GenericParameters)
                ret.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));

            return ret;
        }

        private static MethodDefUser Clone(MethodDef origin)
        {
            var ret = new MethodDefUser(origin.Name, null, origin.ImplAttributes, origin.Attributes);

            foreach (var genericParam in origin.GenericParameters)
                ret.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));

            return ret;
        }

        public static void MoveMethod(MethodDef method)
        {
            if (method == null)
            {
                return;
            }
            if (!method.HasBody)
            {
                return;
            }
            if (!method.Body.HasInstructions)
            {
                return;
            }
            List<Instruction> instructions = new List<Instruction>();
            instructions.AddRange(method.Body.Instructions);
            MethodDef newMethod = new MethodDefUser(string.Format("<Hydra>_{0}", Randomizer.GenerateRandomString(Randomizer.BaseChars2, 10)), method.MethodSig, method.Attributes) { Body = new CilBody(method.Body.InitLocals, new List<Instruction>(), new List<ExceptionHandler>(), new List<Local>()) { MaxStack = method.Body.MaxStack } };
            foreach (ParamDef paramDef in method.ParamDefs)
            {
                newMethod.ParamDefs.Add(new ParamDefUser(paramDef.Name, paramDef.Sequence, paramDef.Attributes));
            }
            newMethod.Parameters.UpdateParameterTypes();
            int index = 0;
            if (method.HasParamDefs && method.Parameters != null && method.Parameters.Count > 0)
            {
                newMethod.Parameters[index].CreateParamDef();
                newMethod.Parameters[index].Name = method.Parameters[index].Name;
                newMethod.Parameters[index].Type = method.Parameters[index].Type;
                index++;
            }
            newMethod.Parameters.UpdateParameterTypes();
            foreach (CustomAttribute ca in method.CustomAttributes)
            {
                newMethod.CustomAttributes.Add(new CustomAttribute((ICustomAttributeType)ca.Constructor));
            }
            if (method.ImplMap != null)
            {
                newMethod.ImplMap = new ImplMapUser(new ModuleRefUser(method.Module, method.ImplMap.Module.Name), method.ImplMap.Name, method.ImplMap.Attributes);
            }
            Dictionary<object, object> bodyMap = new Dictionary<object, object>();
            foreach (Local local in method.Body.Variables)
            {
                Local newLocal = new Local(local.Type);
                newMethod.Body.Variables.Add(newLocal);
                newLocal.Name = local.Name;
                bodyMap[local] = newLocal;
            }
            foreach (Instruction instr in method.Body.Instructions)
            {
                Instruction newInstr = new Instruction(instr.OpCode, instr.Operand)
                {
                    SequencePoint = instr.SequencePoint
                };
                switch (newInstr.Operand)
                {
                    case IType type:
                        newInstr.Operand = type;
                        break;

                    case IMethod theMethod:
                        newInstr.Operand = theMethod;
                        break;

                    case IField field:
                        newInstr.Operand = field;
                        break;
                }
                newMethod.Body.Instructions.Add(newInstr);
                bodyMap[instr] = newInstr;
            }
            foreach (Instruction instr in newMethod.Body.Instructions)
            {
                if (instr.Operand != null && bodyMap.ContainsKey(instr.Operand))
                {
                    instr.Operand = bodyMap[instr.Operand];
                }
                else if (instr.Operand is Instruction[] theInstructions)
                {
                    instr.Operand = theInstructions.Select(target => (Instruction)bodyMap[target]).ToArray();
                }
            }
            foreach (ExceptionHandler eh in method.Body.ExceptionHandlers)
            {
                newMethod.Body.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
                {
                    CatchType = eh.CatchType == null ? null : eh.CatchType,
                    TryStart = (Instruction)bodyMap[eh.TryStart],
                    TryEnd = (Instruction)bodyMap[eh.TryEnd],
                    HandlerStart = (Instruction)bodyMap[eh.HandlerStart],
                    HandlerEnd = (Instruction)bodyMap[eh.HandlerEnd],
                    FilterStart = eh.FilterStart == null ? null : (Instruction)bodyMap[eh.FilterStart]
                });
            }
            newMethod.Body.SimplifyMacros(newMethod.Parameters);
            method.DeclaringType.Methods.Add(newMethod);
            method.Body.Instructions.Clear();
            if (method.HasParamDefs && method.Parameters != null && method.Parameters.Count > 0)
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                int current = 0;
                foreach (Parameter parameter in method.Parameters)
                {
                    if (parameter.Name != null && parameter.Name != "")
                    {
                        if (current == 0)
                        {
                            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        }
                        else if (current == 1)
                        {
                            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                        }
                        else if (current == 2)
                        {
                            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
                        }
                        else if (current == 3)
                        {
                            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
                        }
                        else
                        {
                            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, current));
                        }
                        current++;
                    }
                }
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, newMethod));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, newMethod));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
        }

        private static FieldDefUser Clone(FieldDef origin)
        {
            var ret = new FieldDefUser(origin.Name, null, origin.Attributes);
            return ret;
        }

        private static TypeDef PopulateContext(TypeDef typeDef, InjectContext ctx)
        {
            TypeDef ret;
            IDnlibDef existing;
            if (!ctx.Mep.TryGetValue(typeDef, out existing))
            {
                ret = Clone(typeDef);
                ctx.Mep[typeDef] = ret;
            }
            else
                ret = (TypeDef)existing;

            foreach (var nestedType in typeDef.NestedTypes)
                ret.NestedTypes.Add(PopulateContext(nestedType, ctx));

            foreach (var method in typeDef.Methods)
                ret.Methods.Add((MethodDef)(ctx.Mep[method] = Clone(method)));

            foreach (var field in typeDef.Fields)
                ret.Fields.Add((FieldDef)(ctx.Mep[field] = Clone(field)));

            return ret;
        }

        private static void CopyTypeDef(TypeDef typeDef, InjectContext ctx)
        {
            var newTypeDef = (TypeDef)ctx.Mep[typeDef];

            newTypeDef.BaseType = ctx.Importer.Import(typeDef.BaseType);

            foreach (var iface in typeDef.Interfaces)
                newTypeDef.Interfaces.Add(new InterfaceImplUser(ctx.Importer.Import(iface.Interface)));
        }

        private static void CopyMethodDef(MethodDef methodDef, InjectContext ctx)
        {
            var newMethodDef = (MethodDef)ctx.Mep[methodDef];

            newMethodDef.Signature = ctx.Importer.Import(methodDef.Signature);
            newMethodDef.Parameters.UpdateParameterTypes();

            if (methodDef.ImplMap != null)
                newMethodDef.ImplMap = new ImplMapUser(new ModuleRefUser(ctx.TargetModule, methodDef.ImplMap.Module.Name), methodDef.ImplMap.Name, methodDef.ImplMap.Attributes);

            foreach (var ca in methodDef.CustomAttributes)
                newMethodDef.CustomAttributes.Add(new CustomAttribute((ICustomAttributeType)ctx.Importer.Import(ca.Constructor)));

            if (!methodDef.HasBody)
                return;
            newMethodDef.Body = new CilBody(methodDef.Body.InitLocals, new List<Instruction>(),
                new List<ExceptionHandler>(), new List<Local>())
            { MaxStack = methodDef.Body.MaxStack };

            var bodyMap = new Dictionary<object, object>();

            foreach (var local in methodDef.Body.Variables)
            {
                var newLocal = new Local(ctx.Importer.Import(local.Type));
                newMethodDef.Body.Variables.Add(newLocal);
                newLocal.Name = local.Name;
                newLocal.Attributes = local.Attributes;

                bodyMap[local] = newLocal;
            }

            foreach (var instr in methodDef.Body.Instructions)
            {
                var newInstr = new Instruction(instr.OpCode, instr.Operand)
                {
                    SequencePoint = instr.SequencePoint
                };

                if (newInstr.Operand is IType)
                {
                    IType type = (IType)newInstr.Operand;
                    newInstr.Operand = ctx.Importer.Import(type);
                }
                else if (newInstr.Operand is IMethod)
                {
                    IMethod method = (IMethod)newInstr.Operand;
                    newInstr.Operand = ctx.Importer.Import(method);
                }
                else if (newInstr.Operand is IField)
                {
                    IField field = (IField)newInstr.Operand;
                    newInstr.Operand = ctx.Importer.Import(field);
                }

                newMethodDef.Body.Instructions.Add(newInstr);
                bodyMap[instr] = newInstr;
            }

            foreach (var instr in newMethodDef.Body.Instructions)
            {
                if (instr.Operand != null && bodyMap.ContainsKey(instr.Operand))
                    instr.Operand = bodyMap[instr.Operand];
                else if (instr.Operand is Instruction[])
                {
                    Instruction[] v = instr.Operand as Instruction[];
                    instr.Operand = v.Select(target => (Instruction)bodyMap[target]).ToArray();
                }
            }

            foreach (var eh in methodDef.Body.ExceptionHandlers)
                newMethodDef.Body.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
                {
                    CatchType = eh.CatchType == null ? null : ctx.Importer.Import(eh.CatchType),
                    TryStart = (Instruction)bodyMap[eh.TryStart],
                    TryEnd = (Instruction)bodyMap[eh.TryEnd],
                    HandlerStart = (Instruction)bodyMap[eh.HandlerStart],
                    HandlerEnd = (Instruction)bodyMap[eh.HandlerEnd],
                    FilterStart = eh.FilterStart == null ? null : (Instruction)bodyMap[eh.FilterStart]
                });

            newMethodDef.Body.SimplifyMacros(newMethodDef.Parameters);
        }

        private static void CopyFieldDef(FieldDef fieldDef, InjectContext ctx)
        {
            var newFieldDef = (FieldDef)ctx.Mep[fieldDef];

            newFieldDef.Signature = ctx.Importer.Import(fieldDef.Signature);
        }

        private static void Copy(TypeDef typeDef, InjectContext ctx, bool copySelf)
        {
            if (copySelf)
                CopyTypeDef(typeDef, ctx);

            foreach (var nestedType in typeDef.NestedTypes)
                Copy(nestedType, ctx, true);

            foreach (var method in typeDef.Methods)
                CopyMethodDef(method, ctx);

            foreach (var field in typeDef.Fields)
                CopyFieldDef(field, ctx);
        }

        public static TypeDef Inject(TypeDef typeDef, ModuleDef target)
        {
            var ctx = new InjectContext(target);
            PopulateContext(typeDef, ctx);
            Copy(typeDef, ctx, true);
            return (TypeDef)ctx.Mep[typeDef];
        }

        public static MethodDef Inject(MethodDef methodDef, ModuleDef target)
        {
            var ctx = new InjectContext(target)
            {
                Mep =
                {
                    [methodDef] = Clone(methodDef)
                }
            };
            CopyMethodDef(methodDef, ctx);
            return (MethodDef)ctx.Mep[methodDef];
        }

        public static IEnumerable<IDnlibDef> Inject(TypeDef typeDef, TypeDef newType, ModuleDef target)
        {
            var ctx = new InjectContext(target)
            {
                Mep =
                {
                    [typeDef] = newType
                }
            };
            PopulateContext(typeDef, ctx);
            Copy(typeDef, ctx, false);
            return ctx.Mep.Values.Except(new[] { newType });
        }

        public static bool removeReference(ModuleDefMD module)
        {
            string currentAssemblyName = typeof(InjectHelper).Assembly.GetName().Name;
            var selfReference = module.GetAssemblyRefs().FirstOrDefault(assRef => System.IO.Path.GetFileNameWithoutExtension(assRef.Name) == currentAssemblyName);

            if (selfReference != null)
            {
                module.ResolveAssemblyRef(selfReference.Rid);
                return true;
            }
            else
            {
                return false;
            }
        }

        private class InjectContext : ImportMapper
        {
            public readonly Dictionary<IDnlibDef, IDnlibDef> Mep = new Dictionary<IDnlibDef, IDnlibDef>();

            public readonly ModuleDef TargetModule;

            public InjectContext(ModuleDef target)
            {
                TargetModule = target;
                Importer = new Importer(target, ImporterOptions.TryToUseTypeDefs, new GenericParamContext(), this);
            }

            public Importer Importer { get; }

            public override ITypeDefOrRef Map(ITypeDefOrRef typeDefOrRef)
            {
                return typeDefOrRef is TypeDef typeDef && Mep.ContainsKey(typeDef) ? Mep[typeDef] as TypeDef : null;
            }

            public override IMethod Map(MethodDef methodDef)
            {
                return Mep.ContainsKey(methodDef) ? Mep[methodDef] as MethodDef : null;
            }

            public override IField Map(FieldDef fieldDef)
            {
                return Mep.ContainsKey(fieldDef) ? Mep[fieldDef] as FieldDef : null;
            }
        }
    }
}