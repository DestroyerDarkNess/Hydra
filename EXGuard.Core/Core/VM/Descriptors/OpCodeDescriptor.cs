using System.Linq;

using EXGuard.Core.VMIL;
using EXGuard.Core.Services;

namespace EXGuard.Core.VM
{
    public class OpCodeDescriptor
    {
        private readonly byte[] opCodeOrder = Enumerable.Range(0, 256).Select(x => (byte) x).ToArray();

        internal OpCodeDescriptor(RandomGenerator randomGenerator)
        {
            randomGenerator.Shuffle(opCodeOrder);
        }

        public byte this[ILOpCode opCode] => opCodeOrder[(int) opCode];
    }
}