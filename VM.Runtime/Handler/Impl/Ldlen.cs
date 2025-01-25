using VM.Runtime.Util;
using System;

namespace VM.Runtime.Handler.Impl
{
    public class Ldlen : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            Array arr = (Array)vmContext.Stack.Pop().GetObject();
            vmContext.Stack.Push(arr.Length);
                
            vmContext.Index++;
        }
    }
}