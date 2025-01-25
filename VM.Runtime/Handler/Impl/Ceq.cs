using VM.Runtime.Util;
using System.Linq;

namespace VM.Runtime.Handler.Impl
{
    public class Ceq : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var values = new object[] { vmContext.Stack.Pop().GetObject(), vmContext.Stack.Pop().GetObject() };
            var result = (bool)Factory.CreateArithmethicFactory(HxOpCodes.ACeq, values.Reverse().ToArray()); //Not Sure

            vmContext.Stack.Push(result ? 1 : 0);

            vmContext.Index++;
        }
    }
}