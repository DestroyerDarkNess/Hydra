using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxLdc : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            vmContext.Stack.Push(instruction.Operand.GetObject());
            vmContext.Index++;
        }
    }
}