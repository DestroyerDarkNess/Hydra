using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Proxy
{
    public class ProxyString : Models.Protection
    {
        public ProxyString() : base("Protection.Proxy.ProxyString", "Renamer Phase", "Description for Renamer Phase") { }
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                foreach (TypeDef type in module.GetTypes())
                {
                    if (!Analyzer.CanRename(type)) continue;
                    if (type.IsGlobalModuleType) continue;
                    foreach (MethodDef meth in type.Methods)
                    {
                        if (!Analyzer.CanRename(meth)) continue;
                        if (!meth.HasBody) continue;
                        var instr = meth.Body.Instructions;
                        foreach (var t in instr)
                        {
                            if (t.OpCode != OpCodes.Ldstr) continue;
                            var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
                            var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
                            var meth1 = new MethodDefUser(Core.Randomizer.GenerateRandomString(BaseChars, 20),
                                MethodSig.CreateStatic(module.CorLibTypes.String),
                                methImplFlags, methFlags);
                            module.GlobalType.Methods.Add(meth1);
                            meth1.Body = new CilBody();
                            meth1.Body.Variables.Add(new Local(module.CorLibTypes.String));
                            meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, t.Operand.ToString()));
                            meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                            t.OpCode = OpCodes.Call;
                            t.Operand = meth1;
                        }
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

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        private bool CanRename(TypeDef type)
        {
            // Excluir tipos especiales de VB.NET y C#
            if (string.IsNullOrEmpty(type.Namespace)) return false;
            if (type.IsRuntimeSpecialName || type.IsGlobalModuleType || type.IsSpecialName || type.IsWindowsRuntime || type.IsInterface) return false;
            if (type.Namespace.StartsWith("Microsoft.VisualBasic")) return false;
            if (type.Namespace.StartsWith("My")) return false;
            if (type.Name == "GeneratedInternalTypeHelper" || type.Name == "Resources" || type.Name == "Settings") return false;

            return true;
        }

        private bool CanRename(MethodDef method)
        {
            // Excluir métodos especiales

            if (!method.HasBody) return false;

            if (method.Name == ".ctor" || method.Name == ".cctor") return false;

            if (method.IsConstructor || method.IsRuntimeSpecialName || method.IsRuntime || method.IsStaticConstructor || method.IsVirtual) return false;

            if (method.DeclaringType.Namespace.StartsWith("Microsoft.VisualBasic")) return false;

            return true;
        }

    }
}
