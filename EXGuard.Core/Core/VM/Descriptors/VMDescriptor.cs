using EXGuard.Core.Services;

namespace EXGuard.Core.VM
{
    public class VMDescriptor
    {
        public VMDescriptor(IVMSettings settings)
        {
            Settings = settings;

            RandomGenerator = new RandomGenerator(32);
            Architecture = new ArchDescriptor(RandomGenerator);
            Runtime = new RuntimeDescriptor(RandomGenerator);
            Data = new DataDescriptor((Virtualizer)settings, RandomGenerator);
        }

        internal RandomGenerator RandomGenerator
        {
            get;
        }

        public IVMSettings Settings
        {
            get;
        }

        public ArchDescriptor Architecture
        {
            get;
        }

        public RuntimeDescriptor Runtime
        {
            get;
        }

        public DataDescriptor Data
        {
            get;
            private set;
        }

        public void ResetData()
        {
            Data = new DataDescriptor((Virtualizer)Settings, RandomGenerator);
        }
    }
}