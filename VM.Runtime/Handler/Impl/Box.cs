using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Box : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            Value val = vmContext.Stack.Pop();
            var type = Helper.ResolveType((int)instruction.Operand.GetObject());
            vmContext.Stack.Push(vmContext.Stack.Cast(val, type));
            vmContext.Index++;
        }
    }
}