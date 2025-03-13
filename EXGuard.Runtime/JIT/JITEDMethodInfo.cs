using System;

namespace EXGuard.Runtime.JIT
{
    public class JITEDMethodInfo
    {
        public int MethodToken;
        public byte[] ILCode;
        public uint ILCodeSize;
        public uint MaxStack;
    }
}
