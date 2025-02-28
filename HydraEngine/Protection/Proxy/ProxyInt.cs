using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Method;
using System;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Proxy
{
    public class ProxyInt : Models.Protection
    {
        public ProxyInt() : base("Protection.Proxy.ProxyInt", "Renamer Phase", "Description for Renamer Phase") { }

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
                        for (var i = 0; i < instr.Count; i++)
                        {
                            if (meth.Body.Instructions[i].IsLdcI4())
                            {
                                var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
                                var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
                                var meth1 = new MethodDefUser(Core.Randomizer.GenerateRandomString(BaseChars, 20),
                                            MethodSig.CreateStatic(module.CorLibTypes.Int32),
                                            methImplFlags, methFlags);
                                module.GlobalType.Methods.Add(meth1);
                                meth1.Body = new CilBody();
                                meth1.Body.Variables.Add(new Local(module.CorLibTypes.Int32));
                                meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, instr[i].GetLdcI4Value()));
                                meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                                instr[i].OpCode = OpCodes.Call;
                                instr[i].Operand = meth1;

                                if (DynamicInstructions)
                                {
                                    bool Dynamic = new IL2Dynamic().ConvertToDynamic(meth1, module);
                                }
                            }
                            else if (meth.Body.Instructions[i].OpCode == OpCodes.Ldc_R4)
                            {
                                var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
                                var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
                                var meth1 = new MethodDefUser(Core.Randomizer.GenerateRandomString(BaseChars, 20),
                                            MethodSig.CreateStatic(module.CorLibTypes.Double),
                                            methImplFlags, methFlags);
                                module.GlobalType.Methods.Add(meth1);
                                meth1.Body = new CilBody();
                                meth1.Body.Variables.Add(new Local(module.CorLibTypes.Double));
                                meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, (float)meth.Body.Instructions[i].Operand));
                                meth1.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                                instr[i].OpCode = OpCodes.Call;
                                instr[i].Operand = meth1;

                                if (DynamicInstructions)
                                {
                                    bool Dynamic = new IL2Dynamic().ConvertToDynamic(meth1, module);
                                }
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
