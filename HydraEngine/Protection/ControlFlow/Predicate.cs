using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace HydraEngine.Protection.ControlFlow
{
    internal class Predicate : IPredicate
    {
        private readonly ModuleDefMD ctx;

        private bool inited;

        private int xorKey;

        public Predicate(ModuleDefMD ctx)
        {
            this.ctx = ctx;
        }

        public void Inititalize(CilBody body)
        {
            if (!inited)
            {
                xorKey = new Random().Next();
                inited = true;
            }
        }

        public int GetSwitchKey(int key)
        {
            return key ^ xorKey;
        }

        public void EmitSwitchLoad(IList<Instruction> instrs)
        {
            instrs.Add(Instruction.Create(OpCodes.Ldc_I4, xorKey));
            instrs.Add(Instruction.Create(OpCodes.Xor));
        }
    }
}

