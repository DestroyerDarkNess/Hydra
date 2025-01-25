using VM.Runtime.Util;
using System.Linq;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxClt : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var values = new object[] { vmContext.Stack.Pop().GetObject(), vmContext.Stack.Pop().GetObject() };
            var result = (bool)Factory.CreateArithmethicFactory(HxOpCodes.HxClt, values.Reverse().ToArray());
            vmContext.Stack.Push(result ? 1 : 0);

            vmContext.Index++;
        }
    }
}