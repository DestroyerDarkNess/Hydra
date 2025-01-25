using VM.Runtime.Util;
using System;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxFld : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            /*var str = (string) instruction.Operand.GetObject();

            var id = Helper.ReadPrefix(str);
            var mdtoken = int.Parse(str.Substring(1));

            var fi = Helper.ResolveField(mdtoken);
            if (id == 0)
            {
                vmContext.Stack.Push(fi.GetValue(vmContext.Stack.Pop().GetObject()));
            } else if(id == 1)
            {
                vmContext.Stack.Push(fi.GetValue(null));
            } else if(id == 2)
            {
                var item = vmContext.Stack.Pop().GetObject();
                fi.SetValue(fi.IsStatic ? null : vmContext.Stack.Pop().GetObject(), item);
            }*/
            var str = (string)instruction.Operand.GetObject();

            var prefix = Helper.ReadPrefix(str);
            var mdtoken = int.Parse(str.Substring(1));
            var field = Helper.ResolveField(mdtoken);

            if (prefix == 0)
            {
                var owner = field.IsStatic ? null : vmContext.Stack.Pop().GetObject();
                vmContext.Stack.Push(field.GetValue(owner));
            }
            else if (prefix == 1)
            {
                var obj = vmContext.Stack.Pop().GetObject();
                var owner = field.IsStatic ? null : vmContext.Stack.Pop().GetObject();

                try
                {
                    field.SetValue(owner, Convert.ChangeType(obj, field.FieldType));
                }
                catch
                {
                    field.SetValue(owner, obj);
                }
            }
            vmContext.Index++;
        }
    }
}