using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace HydraEngine.Protection.ControlFlow
{
    internal interface IPredicate
    {
        void Inititalize(CilBody body);

        void EmitSwitchLoad(IList<Instruction> instrs);

        int GetSwitchKey(int key);
    }
}

