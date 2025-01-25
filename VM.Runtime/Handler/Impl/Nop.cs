using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Nop : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction) => vmContext.Index++;
    }
}