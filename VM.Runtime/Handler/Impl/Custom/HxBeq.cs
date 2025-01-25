using VM.Runtime.Util;
using System.Linq;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxBeq : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var values = new object[] { vmContext.Stack.Pop().GetObject(), vmContext.Stack.Pop().GetObject() }.Reverse().ToArray();
            if (values[0].Equals(values[1]))
            {
                vmContext.Index = (int)instruction.Operand.GetObject();
            }
            else
            {
                vmContext.Index++;
            }
        }
    }
}