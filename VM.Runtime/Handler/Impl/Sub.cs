using VM.Runtime.Util;
using System.Linq;

namespace VM.Runtime.Handler.Impl
{
    public class Sub : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var values = new object[] { vmContext.Stack.Pop().GetObject(), vmContext.Stack.Pop().GetObject() };
            var result = Factory.CreateArithmethicFactory(HxOpCodes.ASub, values.Reverse().ToArray()); //Not Sure

            vmContext.Stack.Push(result);
            vmContext.Index++;
        }
    }
}