using VM.Runtime.Util;

namespace VM.Runtime.Handler
{
    public abstract class HxOpCode
    {
        public abstract void Execute(Context vmContext, HxInstruction instruction);
    }
}