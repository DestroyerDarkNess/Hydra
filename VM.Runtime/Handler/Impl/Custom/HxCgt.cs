using VM.Runtime.Util;
using System.Linq;
using System.Linq.Expressions;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxCgt : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var obj = (string)instruction.Operand.GetObject();
            var prefix = Helper.ReadPrefix(obj);

            ExpressionType ET = prefix == 1 ? ExpressionType.NotEqual : ExpressionType.GreaterThan;
            var values = new object[] { vmContext.Stack.Pop().GetObject(), vmContext.Stack.Pop().GetObject() };
            var result = (bool)Factory.CreateArithmethicFactoryM(values.Reverse().ToArray(), ET);
            vmContext.Stack.Push(result ? 1 : 0);

            vmContext.Index++;
        }
    }
}