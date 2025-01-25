using VM.Runtime.Util;
using System;

namespace VM.Runtime.Handler.Impl
{
    public class Newarr : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var mdtoken = (int)instruction.Operand.GetObject();

            var length = (int)vmContext.Stack.Pop().GetObject();
            var type = Helper.ResolveType(mdtoken).MakeArrayType().GetElementType();
            //var type = Helper.ResolveType(mdtoken).GetType();

            vmContext.Stack.Push(Array.CreateInstance(type, length));
            vmContext.Index++;
        }
    }
}