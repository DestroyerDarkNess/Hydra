using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Ldftn : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var method = Helper.ResolveMethod((int)instruction.Operand.GetObject());
            vmContext.Stack.Push(method.MethodHandle.GetFunctionPointer());
            vmContext.Index++;
        }
    }
}