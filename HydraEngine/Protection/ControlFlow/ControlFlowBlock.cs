using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System.Linq;

namespace HydraEngine.Protection.ControlFlow
{
    public class ControlFlowBlock
    {
        public readonly Instruction Footer;

        public readonly Instruction Header;

        public readonly int Id;

        public readonly ControlFlowBlockType Type;

        public IList<ControlFlowBlock> Sources { get; private set; }

        public IList<ControlFlowBlock> Targets { get; private set; }

        internal ControlFlowBlock(int id, ControlFlowBlockType type, Instruction header, Instruction footer)
        {
            Id = id;
            Type = type;
            Header = header;
            Footer = footer;
            Sources = new List<ControlFlowBlock>();
            Targets = new List<ControlFlowBlock>();
        }

        public override string ToString()
        {
            return string.Format("Block {0} => {1} {2}", Id, Type, string.Join(", ", Targets.Select((ControlFlowBlock block) => block.Id.ToString()).ToArray()));
        }
    }
}

