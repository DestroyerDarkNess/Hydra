using VM.Runtime.Util;
using System;
using System.Reflection;

namespace VM.Runtime.Handler.Impl
{
    public class Castclass : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            TypeInfo conversionType = Helper.ResolveType((int)instruction.Operand.GetObject());
            object o = vmContext.Stack.Pop().GetObject();
            object newo = Convert.ChangeType(o, conversionType);
            vmContext.Stack.Push(newo);
            vmContext.Index++;
        }
    }
}