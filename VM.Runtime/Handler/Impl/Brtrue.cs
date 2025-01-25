using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Brtrue : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            /*var v = vmContext.Stack.Pop();
            object val = v.GetObject();

            if (v.IsBool())
                val = (bool)v.GetObject() ? 1 : 0;
            else
                val = (int)v.GetObject();

            if (val == null || Convert.ToInt32(val) == 1)
            {
                vmContext.Index = (int)instruction.Operand.GetObject();
            }
            else
            {
                vmContext.Index++;
            }*/
            var val = vmContext.Stack.Pop().GetObject();
            if (val is int Integer && Integer == 1)
                vmContext.Index = (int)instruction.Operand.GetObject();
            else if (val is bool Boolean && Boolean == true)
                vmContext.Index = (int)instruction.Operand.GetObject();
            else
                vmContext.Index++;
        }
    }
}