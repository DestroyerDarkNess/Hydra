using VM.Runtime.Util;
using System;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxArray : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var prefix = (int) instruction.Operand.GetObject();

            if (prefix == 0)
            {
                var idx = (int)vmContext.Stack.Pop().GetObject();
                var arr = (Array)vmContext.Stack.Pop().GetObject();

                vmContext.Stack.Push(arr.GetValue(idx));

                /*var castType = Helper.ResolveType((int)instruction.Operand.GetObject());
                vmContext.Stack.Push(Convert.ChangeType((Array)arr.GetValue(idx), castType));*/
            }
            else if (prefix == 1)
            {
                var obj = vmContext.Stack.Pop().GetObject();
                var idx = (int)vmContext.Stack.Pop().GetObject();
                var arr = (Array)vmContext.Stack.Pop().GetObject();

                arr.SetValue(Convert.ChangeType(obj, arr.GetType().GetElementType()), idx);
                
                /*arr.SetValue(obj, idx);*/
            }

            vmContext.Index++;
        }
    }
}