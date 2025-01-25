using VM.Runtime.Util;
using System.Linq;

namespace VM.Runtime.Handler.Impl
{
    public class Neg : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var values = new object[] { vmContext.Stack.Pop().GetObject() };
            var result = Factory.CreateArithmethicFactory(HxOpCodes.ANeg, values.Reverse().ToArray()); //Not Sure

            vmContext.Stack.Push(result);
            vmContext.Index++;
        }
    }
}