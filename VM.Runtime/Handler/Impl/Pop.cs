using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Pop : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            vmContext.Stack.Pop();
            vmContext.Index++;
        }
    }
}