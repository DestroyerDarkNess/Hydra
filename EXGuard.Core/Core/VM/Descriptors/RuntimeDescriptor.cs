using EXGuard.Core.Services;

namespace EXGuard.Core.VM
{
    public class RuntimeDescriptor
    {
        internal RuntimeDescriptor(RandomGenerator randomGenerator)
        {
            VMCall = new VMCallDescriptor(randomGenerator);
            VCallOps = new VCallOpsDescriptor(randomGenerator);
            RTFlags = new RTFlagDescriptor(randomGenerator);
        }

        public VMCallDescriptor VMCall
        {
            get;
        }

        public VCallOpsDescriptor VCallOps
        {
            get;
        }

        public RTFlagDescriptor RTFlags
        {
            get;
        }
    }
}