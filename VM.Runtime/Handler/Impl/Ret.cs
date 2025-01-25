using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Ret : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            //vmContext.Stack.Push(vmContext.Stack.Count == 0 ? new Value(null) : vmContext.Stack.Pop().GetObject());
            vmContext.Stack.Push(vmContext.Stack.Count == 0 ? new Value(null) : vmContext.Stack.Pop());
            vmContext.Index++;
        }
    }
}