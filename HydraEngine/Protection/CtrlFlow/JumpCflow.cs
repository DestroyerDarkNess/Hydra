using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.CtrlFlow
{
    internal class JumpCflow
    {
        public static void Execute(ModuleDefMD module)
        {
            foreach (var type in module.Types)
            {
                foreach (var meth in type.Methods.ToArray())
                {
                    if (!meth.HasBody || !meth.Body.HasInstructions || meth.Body.HasExceptionHandlers) continue;
                    for (var i = 0; i < meth.Body.Instructions.Count - 2; i++)
                    {
                        var inst = meth.Body.Instructions[i + 1];
                        meth.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Ldstr, Core.Randomizer.GenerateRandomString()));
                        meth.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Br_S, inst));
                        i += 2;
                    }
                }
            }
        }
    }
}
