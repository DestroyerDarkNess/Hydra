using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Newobj : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var mdtoken = instruction.Operand.GetObject();
            var constructor = Helper.ResolveConstructor((int)mdtoken);
            var parameters = Helper.GetMethodParameters(vmContext, constructor);

            var inst = constructor.Invoke(parameters.ToArray());

            if (inst != null)
                vmContext.Stack.Push(inst);

            vmContext.Index++;
        }
    }
}