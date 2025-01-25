using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.CtrlFlow
{
    internal class Block
    {
        public Block()
        {
            Instructions = new List<Instruction>();
        }

        public List<Instruction> Instructions { get; set; }

        public int Number { get; set; }
    }
}
