using VM.Runtime.Util;

namespace VM.Runtime.Handler.Impl
{
    public class Constrained : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            //Need to be implemented for Callvirt case.
            vmContext.Index++;
        }
    }
}