using VM.Runtime.Util;
using System.Linq;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxBge : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var obj = (string)instruction.Operand.GetObject();

            var prefix = Helper.ReadPrefix(obj);

            var values = new object[] { vmContext.Stack.Pop().GetObject(), vmContext.Stack.Pop().GetObject() }.Reverse().ToArray();

            bool res = prefix == 0 ? (int)values[0] >= (int)values[1] : unchecked((uint)values[0]) >= unchecked((uint)values[1]);
            /*bool res = false;
            if(prefix == 0)
            {
                res = (int)values[0] >= (int)values[1];
            } else if(prefix == 1)
            {
                res = unchecked((uint)values[0]) >= unchecked((uint)values[1]);
            }*/

            if (res)
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