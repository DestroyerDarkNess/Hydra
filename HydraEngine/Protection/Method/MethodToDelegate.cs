using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydraEngine.Protection.Method.Runtime;

namespace HydraEngine.Protection.Method
{
     public class MethodToDelegate : Models.Protection
    {
        public MethodToDelegate() : base("Protection.Method.MethodToDelegate", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
              
                foreach (TypeDef type in Module.Types.ToArray())
                {
                    if (type.Namespace.StartsWith("My")) continue;
                    if (type.IsGlobalModuleType)
                    {
                        continue;
                    }

                    if (!CanRename(type)) continue;

                    if (type.Name == "GeneratedInternalTypeHelper")
                    {
                        continue;
                    }

                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (method.Name == ".ctor" || method.Name == ".cctor") continue;
                        if (!CanRename(method)) continue;
                        if (!method.HasBody)
                        {
                            continue;
                        }

                        if (method.IsVirtual || method.IsSpecialName)
                        {
                            continue;
                        }

                        if (method.Name == ".ctor" || method.Name == ".cctor")
                        {
                            continue;
                        }

                        if (method.IsRuntimeSpecialName || method.IsSpecialName || method.Name == "Invoke") continue;
                        ReplaceMethodWithDelegate(Module, method);
                    }
                }
                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

      
        private bool CanRename(MethodDef method)
        {
            return !method.IsConstructor &&
                   !method.DeclaringType.IsForwarder &&
                   !method.IsFamily &&
                   !method.IsStaticConstructor &&
                   !method.IsRuntimeSpecialName &&
                   !method.DeclaringType.IsGlobalModuleType &&
                   !method.Name.Contains("Hydra");
        }

        private static bool CanRename(TypeDef type)
        {
            if (type.Namespace.Contains("My")) return false;
            return !type.IsGlobalModuleType &&
                   type.Interfaces.Count == 0 &&
                   !type.IsSpecialName &&
                   !type.IsRuntimeSpecialName &&
                   !type.Name.Contains("<HailHydra>");
        }

        private static void ReplaceMethodWithDelegate(ModuleDef module, MethodDef method)
        {
            var delegateType = CreateDelegateType(module, method);
            var createDelegateMethod = typeof(M2D).GetMethod(nameof(M2D.CreateDelegate)).MakeGenericMethod(delegateType);

            var import = module.Import(createDelegateMethod);
            var ilProcessor = method.Body.Instructions;

            ilProcessor.Insert(0, Instruction.Create(OpCodes.Call, import));
            ilProcessor.Insert(1, Instruction.Create(OpCodes.Stsfld, CreateDelegateField(module, method, delegateType)));
        }

        private static FieldDef CreateDelegateField(ModuleDef module, MethodDef method, Type delegateType)
        {
            var delegateField = new FieldDefUser(method.Name + "Delegate_" + Guid.NewGuid().ToString("N"), new FieldSig(module.Import(typeof(Delegate)).ToTypeSig()))
            {
                Attributes = FieldAttributes.Private | FieldAttributes.Static
            };
            module.GlobalType.Fields.Add(delegateField);
            return delegateField;
        }

        private static Type CreateDelegateType(ModuleDef module, MethodDef method)
        {
            var delegateName = method.Name + "Delegate_" + Guid.NewGuid().ToString("N");
            var delegateType = new TypeDefUser("DynamicDelegates", delegateName, module.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
            module.Types.Add(delegateType);

            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.Object, module.CorLibTypes.IntPtr));
            ctor.Attributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
            delegateType.Methods.Add(ctor);

            var invoke = new MethodDefUser("Invoke", method.MethodSig);
            invoke.Attributes = MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.NewSlot;
            delegateType.Methods.Add(invoke);

            delegateType.BaseType = module.CorLibTypes.GetTypeRef("System", "MulticastDelegate");
            return delegateType.GetType();
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
