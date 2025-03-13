using dnlib.DotNet;

namespace EXGuard.Core.VM
{
    internal class FuncSigDesc
    {
        public readonly int MDToken;
        public readonly MethodDef Method;
        public readonly MethodSig Signature;
        public readonly ITypeDefOrRef DeclaringType;
        public readonly FuncSig FuncSig;

        public FuncSigDesc(MethodDef method, MDToken mdToken)
        {
            Method = method;
            MDToken = mdToken.ToInt32();
            Signature = method.MethodSig;
            DeclaringType = method.DeclaringType;
            FuncSig = new FuncSig();
        }
    }
}
