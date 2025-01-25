using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Endfinally : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction) => vmContext.Index = (int)instruction.Operand.GetObject();
    }
}