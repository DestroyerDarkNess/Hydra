using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Method;
using HydraEngine.Protection.Renamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Proxy
{
    public class ProxyClass : Models.Protection
    {
        public ProxyClass() : base("Protection.Proxy.ProxyClass", "Renamer Phase", "Description for Renamer Phase") { }

        public bool Unsafe { get; set; } = false;

        private const string ProxyNamespace = "ProxiedObjects";


        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {

                Dictionary<TypeDef, TypeDef> targetTypes = new Dictionary<TypeDef, TypeDef>();

                foreach (var type in Module.Types.ToArray())
                {
                    if (!AnalyzerPhase.CanRename(type)) continue;

                    if (!type.IsClass || type.IsSealed || type.IsAbstract || type.IsValueType) continue;

                    if (!HasPublicConstructor(type)) continue;

                    if (targetTypes.ContainsKey(type)) continue;

                    //if (!IsWindowsForm(type)) continue;

                    var proxyType = CreateProxyType(type, Module);
                    Module.Types.Add(proxyType);

                    targetTypes.Add(type, proxyType);

                    //Console.WriteLine($"Found target type: {type.FullName}");
                }

                List<MethodDef> Closuremethods = new List<MethodDef>();

                foreach (var type in Module.Types.ToArray())
                {
                    foreach (var method in type.Methods.ToArray())
                    {
                        if (method.HasClosureReferences())
                        {
                            Closuremethods.Add(method);
                        }
                    }
                }

                foreach (var type in Module.Types.ToArray())
                {
                    foreach (var method in type.Methods.ToArray())
                    {
                        if (!method.HasBody || !method.Body.HasInstructions) continue;

                        if (!AnalyzerPhase.CanRename(method, type)) continue;

                        if (method.Body.Instructions.Any(instr => IsAccessingNonPublicMember(instr, type))) continue;

                        var instructions = method.Body.Instructions;
                        for (int i = 0; i < instructions.Count; i++)
                        {
                            if (instructions[i].OpCode == OpCodes.Newobj &&
                                instructions[i].Operand is MethodDef ctor &&
                                targetTypes.ContainsKey(ctor.DeclaringType))
                            {
                                var proxyType = ctor.DeclaringType.Module.Types
                                    .FirstOrDefault(t => t.Namespace == ProxyNamespace &&
                                                       t.Name == $"Proxy_Object_{ctor.DeclaringType.Name}");

                                if (proxyType != null && proxyType.Methods.Any(m => m.IsConstructor))
                                {
                                    var proxyCtor = proxyType.Methods.First(m => m.IsConstructor);
                                    instructions[i].Operand = proxyCtor;
                                }
                            }
                            else if (instructions[i].OpCode == OpCodes.Call &&
                                     instructions[i].Operand is MethodDef staticMethod &&
                                     staticMethod.IsStatic &&
                                     targetTypes.ContainsKey(staticMethod.DeclaringType))
                            {
                                Console.WriteLine($"Found static method call: {staticMethod.FullName}");
                                var proxyType = staticMethod.DeclaringType.Module.Types
                                    .FirstOrDefault(t => t.Namespace == ProxyNamespace &&
                                                       t.Name == $"Proxy_Object_{staticMethod.DeclaringType.Name}");

                                if (proxyType != null)
                                {
                                    if (!Closuremethods.Contains(staticMethod))
                                    {
                                        staticMethod.DeclaringType = proxyType;
                                    }

                                    //var proxyMethod = proxyType.Methods.FirstOrDefault(m => m.Name == staticMethod.Name && m.MethodSig == staticMethod.MethodSig);
                                    //if (proxyMethod != null)
                                    //{
                                    //    instructions[i].Operand = proxyMethod;
                                    //}
                                }
                            }
                        }

                    }
                }

                foreach (var type in targetTypes.Values)
                {
                    type.Namespace = Guid.NewGuid().ToString("N") + Randomizer.GenerateRandomString(10, 30);
                    type.Name = Guid.NewGuid().ToString("N") + Randomizer.GenerateRandomString(10, 30);
                }
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

        private bool HasPublicConstructor(TypeDef type)
        {
            return type.Methods.Any(m => m.IsConstructor &&
                                       m.IsPublic &&
                                       !m.IsStatic);
        }

        private bool IsWindowsForm(TypeDef type)
        {
            return type.BaseType != null && type.BaseType.FullName == "System.Windows.Forms.Form";
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

        private void FixFieldAndMethodAccess(MethodDef method)
        {
            if (!method.HasBody) return;
            var instrs = method.Body.Instructions;

            foreach (var instr in instrs)
            {
                if (instr.Operand is IField fieldRef)
                {
                    var fieldDef = fieldRef.ResolveFieldDef();
                    if (fieldDef != null && fieldDef.IsPrivate)
                    {
                        fieldDef.Attributes &= ~FieldAttributes.FieldAccessMask;
                        fieldDef.Attributes |= FieldAttributes.Public;
                    }
                }

                if (instr.Operand is IMethod methodRef)
                {
                    var targetMethodDef = methodRef.ResolveMethodDef();
                    if (targetMethodDef != null && targetMethodDef.IsPrivate)
                    {
                        targetMethodDef.Attributes &= ~MethodAttributes.MemberAccessMask;
                        targetMethodDef.Attributes |= MethodAttributes.Public;
                    }
                }
            }
        }

        private void FixPrivateAccess(MethodDef method)
        {
            if (method == null || !method.HasBody)
                return;

            foreach (var instr in method.Body.Instructions)
            {
                if (instr.Operand is IField fieldRef)
                {
                    var fieldDef = fieldRef.ResolveFieldDef();
                    if (fieldDef != null && fieldDef.IsPrivate)
                    {
                        fieldDef.Attributes &= ~FieldAttributes.FieldAccessMask;
                        fieldDef.Attributes |= FieldAttributes.Public;
                    }
                }

                else if (instr.Operand is IMethod methodRef)
                {
                    var targetMethodDef = methodRef.ResolveMethodDef();
                    if (targetMethodDef != null && targetMethodDef.IsPrivate)
                    {
                        targetMethodDef.Attributes &= ~MethodAttributes.MemberAccessMask;
                        targetMethodDef.Attributes |= MethodAttributes.Public;
                    }
                }
            }
        }

        private TypeDefUser CreateProxyType(TypeDef originalType, ModuleDef module)
        {
            var proxyName = $"Proxy_Object_{originalType.Name}";
            var proxyType = new TypeDefUser(ProxyNamespace, proxyName,
                module.CorLibTypes.Object.ToTypeDefOrRef())
            {
                BaseType = originalType.ToTypeSig().ToTypeDefOrRef(),
                Attributes = TypeAttributes.Public | TypeAttributes.Class
            };

            var cctors = originalType.Methods.Where(m => m.IsConstructor).ToArray();
            if (cctors.Count() <= 1)
            {
                foreach (var originalCtor in cctors)
                {
                    var proxyCtor = CloneConstructor(originalCtor, proxyType);
                    proxyType.Methods.Add(proxyCtor);
                }
            }


            return proxyType;
        }

        private TypeDefUser CreateProxyTypeAsDelegate(TypeDef originalType, ModuleDefMD module)
        {
            var proxyName = $"Proxy_Object_{originalType.Name}";

            var invokeMethod = new MethodDefUser(
                "Invoke",
                MethodSig.CreateInstance(module.CorLibTypes.Void),
                MethodImplAttributes.Runtime,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual)
            {
                Body = new CilBody()
            };

            string delegateName = "MyHiddenDelegate_" + Guid.NewGuid().ToString("N");
            var delegateType = MethodMover.GetOrCreateStaticContainerDelegate(module, invokeMethod.MethodSig);
            delegateType.Name = delegateName;

            // Crear el proxyType que hereda del originalType
            var proxyType = new TypeDefUser(ProxyNamespace, proxyName,
                module.CorLibTypes.Object.ToTypeDefOrRef())
            {
                BaseType = originalType.ToTypeSig().ToTypeDefOrRef(),
                Attributes = TypeAttributes.Public | TypeAttributes.Class
            };

            // Añadir el delegado como una interfaz
            proxyType.Interfaces.Add(new InterfaceImplUser(delegateType));

            var cctors = originalType.Methods.Where(m => m.IsConstructor).ToArray();
            if (cctors.Count() <= 1)
            {
                foreach (var originalCtor in cctors)
                {
                    var proxyCtor = CloneConstructor(originalCtor, proxyType);
                    proxyType.Methods.Add(proxyCtor);
                }
            }

            return proxyType;
        }


        private MethodDef CloneConstructor(MethodDef originalCtor, TypeDef proxyType)
        {
            var clonedCtor = new MethodDefUser(
                originalCtor.Name,
                originalCtor.MethodSig,
                originalCtor.ImplAttributes,
                originalCtor.Attributes);

            clonedCtor.Body = new CilBody();

            var il = clonedCtor.Body.Instructions;
            int paramCount = originalCtor.Parameters.Count;

            for (int i = 0; i < paramCount; i++)
            {
                var param = originalCtor.Parameters[i];

                if (i == 0)
                    il.Add(OpCodes.Ldarg_0.ToInstruction());
                else if (i == 1)
                    il.Add(OpCodes.Ldarg_1.ToInstruction());
                else if (i == 2)
                    il.Add(OpCodes.Ldarg_2.ToInstruction());
                else if (i == 3)
                    il.Add(OpCodes.Ldarg_3.ToInstruction());
                else if (i <= byte.MaxValue)
                    il.Add(Instruction.Create(OpCodes.Ldarg_S, param));
                else
                    il.Add(Instruction.Create(OpCodes.Ldarg, param));
            }

            // Llamar al constructor base
            clonedCtor.Body.Instructions.Add(OpCodes.Call.ToInstruction(originalCtor));
            clonedCtor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

            return clonedCtor;
        }

    }

}
