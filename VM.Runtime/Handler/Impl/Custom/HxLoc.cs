using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxLoc : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var str = (string) instruction.Operand.GetObject();
            var prefix = Helper.ReadPrefix(str);
            var idx = int.Parse(str.Substring(1));

            if (prefix == 0)
            {
                vmContext.Stack.Push(vmContext.Locals.Get(idx));
                //vmContext.Stack.Push(vmContext.Locals.Get(idx).GetObject());
            }
            else
            {
                var item = vmContext.Stack.Pop();
                vmContext.Locals.Update(idx, item);
            }
                
            vmContext.Index++;
        }
    }
}