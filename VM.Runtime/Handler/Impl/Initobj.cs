using VM.Runtime.Util;
using System;
using System.Runtime.Serialization;

namespace VM.Runtime.Handler.Impl
{
    public class Initobj : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction) {

            Value val = vmContext.Stack.Pop();
            var type = Helper.ResolveType((int)instruction.Operand.GetObject());
            object obj = null;

            if (type.IsValueType)
                if (Nullable.GetUnderlyingType(type) == null)
                    obj = FormatterServices.GetUninitializedObject(type);

            vmContext.Stack.Push(obj);
            vmContext.Index++;
        }
    }
}