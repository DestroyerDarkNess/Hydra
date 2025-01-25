using HydraEngine.Protection.VM;

namespace VM.Core.Protections
{
    public abstract class IProtection
	{
        public abstract string Name();
        public abstract void Execute(Virtualizer context);
    }
}