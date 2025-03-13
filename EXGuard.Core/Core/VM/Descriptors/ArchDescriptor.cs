using EXGuard.Core.Services;

namespace EXGuard.Core.VM
{
    public class ArchDescriptor
    {
        internal ArchDescriptor(RandomGenerator randomGenerator)
        {
            OpCodes = new OpCodeDescriptor(randomGenerator);
            Flags = new FlagDescriptor(randomGenerator);
            Registers = new RegisterDescriptor(randomGenerator);
        }

        public OpCodeDescriptor OpCodes
        {
            get;
        }

        public FlagDescriptor Flags
        {
            get;
        }

        public RegisterDescriptor Registers
        {
            get;
        }
    }
}