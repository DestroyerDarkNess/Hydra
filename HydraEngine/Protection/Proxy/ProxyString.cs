using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Method;
using System;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Proxy
{
    public class ProxyString : Models.Protection
    {
        public ProxyString() : base("Protection.Proxy.ProxyString", "Renamer Phase", "Description for Renamer Phase") { }
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public bool DynamicInstructions { get; set; } = false;

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

                            if (DynamicInstructions)
                            {
                                bool Dynamic = new IL2Dynamic().ConvertToDynamic(meth1, module);
                            }

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

    }
}
