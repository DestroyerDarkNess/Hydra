using dnlib.DotNet;

namespace EXGuard.Core.JIT
{
    public class JITEDMethodInfo
    {
        public MethodDef Method;
        public int MethodToken;

        public byte[] ILCode;
        public uint ILCodeSize;
        public uint MaxStack;
    }
}
