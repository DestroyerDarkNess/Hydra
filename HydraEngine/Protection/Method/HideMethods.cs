using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Method
{
    public class HideMethods : Models.Protection
    {
        public HideMethods() : base("Protection.Method.HideMethods", "Renamer Phase", "Description for Renamer Phase") { }


        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                // Recorrer todos los tipos en el módulo
                foreach (TypeDef type in Module.Types)
                {
                    // Recorrer todos los métodos en el tipo
                    foreach (MethodDef method in type.Methods)
                    {
                        // Aplicar el atributo HideBySig
                        method.Attributes |= MethodAttributes.HideBySig;
                    }
                }

                TypeRef attrRef = Module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CompilerGeneratedAttribute");
                var ctorRef = new MemberRefUser(Module, ".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void), attrRef);
                var attr = new CustomAttribute(ctorRef);

                TypeRef attrRef2 = Module.CorLibTypes.GetTypeRef("System", "EntryPointNotFoundException");
                var ctorRef2 = new MemberRefUser(Module, ".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.String), attrRef2);

                foreach (TypeDef type in Module.Types.ToArray())
                {
                    if (!Analyzer.CanRename(type)) continue;
                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (!Analyzer.CanRename(method)) continue;
                        if (method.IsRuntimeSpecialName || method.IsSpecialName || method.Name == "Invoke") continue;
                        //method.CustomAttributes.Add(attr);
                    HydraEngine.Core.InjectHelper.AddAttributeToMethod(method, attr);
                        method.Name = "<Hydra>" + method.Name;
                    }
                }

                var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
                var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
                var meth1 = new MethodDefUser("Main",
                            MethodSig.CreateStatic(Module.CorLibTypes.Void, Module.CorLibTypes.String),
                            methImplFlags, methFlags);
                if (Module.EntryPoint != null) Module.EntryPoint.DeclaringType.Methods.Add(meth1);
                var body = new CilBody();
                meth1.Body = body;
                meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Protected by Hydra"));
                meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctorRef2));
                meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Throw));

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


        //private bool CanRename(TypeDef type)
        //{
        //    // Excluir tipos especiales de VB.NET y C#
        //    if (string.IsNullOrEmpty(type.Namespace)) return false;
        //    if (type.IsRuntimeSpecialName || type.IsGlobalModuleType || type.IsSpecialName || type.IsWindowsRuntime || type.IsInterface) return false;
        //    if (type.Namespace.StartsWith("Microsoft.VisualBasic")) return false;
        //    if (type.Namespace.StartsWith("My")) return false;
        //    //     if (type.Name.StartsWith("My") || type.Namespace.Contains(".My") || type.Namespace.Contains("My.")) return false;
        //    if (type.Name == "GeneratedInternalTypeHelper" || type.Name == "Resources" || type.Name == "Settings") return false;

        //    return true;
        //}

        //private bool CanRename(MethodDef method)
        //{
        //    // Excluir métodos especiales

        //    if (!method.HasBody) return false;

        //    if (method.Name == ".ctor" || method.Name == ".cctor") return false;

        //    if (method.IsConstructor || method.IsRuntimeSpecialName || method.IsRuntime || method.IsStaticConstructor || method.IsVirtual) return false;

        //    if (method.DeclaringType.Namespace.StartsWith("Microsoft.VisualBasic")) return false;

        //    return true;
        //}
    }
}
