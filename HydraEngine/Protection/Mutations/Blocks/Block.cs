using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace HydraEngine.Protection.Mutations.Blocks
{
    public class Block
    {
        public List<Instruction> Instructions;
        public List<int> MarkedValues;
        public bool IsSafe;
        public bool IsBranched;
        public bool IsException;
        public Block()
        {
            Instructions = new List<Instruction>();
            MarkedValues = new List<int>();

            IsSafe = true;
            IsBranched = false;
            IsException = false;
        }

        public void Copy(List<Instruction> instructions)
        {
            Instructions.AddRange(instructions);
        }
    }
}
