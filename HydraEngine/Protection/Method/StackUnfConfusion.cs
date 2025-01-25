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
     public class StackUnfConfusion : Models.Protection
     {
        public StackUnfConfusion() : base("Protection.Renamer.StackUnfConfusion", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                foreach (TypeDef type in module.Types)
                {
                    if (!Analyzer.CanRename(type)) continue;
                    foreach (MethodDef meth in type.Methods)
                    {
                        if (!Analyzer.CanRename(meth)) continue;
                        if (!meth.HasBody) continue;

                        var body = meth?.Body;
                        var target = body?.Instructions[0];
                        var item = Instruction.Create(OpCodes.Br_S, target);
                        var instruction3 = Instruction.Create(OpCodes.Pop);
                        var random = new Random();
                        Instruction instruction4;
                        int randomValue = random.Next(0, 5);

                        switch (randomValue)
                        {
                            case 0:
                                instruction4 = Instruction.Create(OpCodes.Ldnull);
                                break;
                            case 1:
                                instruction4 = Instruction.Create(OpCodes.Ldc_I4_0);
                                break;
                            case 2:
                                instruction4 = Instruction.Create(OpCodes.Ldstr, "Isolator");
                                break;
                            case 3:
                                instruction4 = Instruction.Create(OpCodes.Ldc_I8, (uint)random.Next());
                                break;
                            default:
                                instruction4 = Instruction.Create(OpCodes.Ldc_I8, (long)random.Next());
                                break;
                        }

                        body?.Instructions.Insert(0, instruction4);
                        body?.Instructions.Insert(1, instruction3);
                        body?.Instructions.Insert(2, item);
                        if (body != null)
                        {
                            foreach (var handler in body.ExceptionHandlers)
                            {
                                if (handler.TryStart == target)
                                {
                                    handler.TryStart = item;
                                }
                                else if (handler.HandlerStart == target)
                                {
                                    handler.HandlerStart = item;
                                }
                                else if (handler.FilterStart == target)
                                {
                                    handler.FilterStart = item;
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
