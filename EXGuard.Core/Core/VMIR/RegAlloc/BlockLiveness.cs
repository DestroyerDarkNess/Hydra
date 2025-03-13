using System.Collections.Generic;

using EXGuard.Core.AST.IR;

namespace EXGuard.Core.VMIR.RegAlloc
{
    public class BlockLiveness
    {
        private BlockLiveness(HashSet<IRVariable> inLive, HashSet<IRVariable> outLive)
        {
            InLive = inLive;
            OutLive = outLive;
        }

        public HashSet<IRVariable> InLive
        {
            get;
        }

        public HashSet<IRVariable> OutLive
        {
            get;
        }

        internal static BlockLiveness Empty()
        {
            return new BlockLiveness(new HashSet<IRVariable>(), new HashSet<IRVariable>());
        }

        internal BlockLiveness Clone()
        {
            return new BlockLiveness(new HashSet<IRVariable>(InLive), new HashSet<IRVariable>(OutLive));
        }
    }
}