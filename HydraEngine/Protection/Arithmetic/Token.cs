using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic_Obfuscation.Arithmetic
{
    public class Token
    {
        private OpCode opCode;
        private object Operand;
        public Token(OpCode opCode, object Operand)
        {
            this.opCode = opCode;
            this.Operand = Operand;
        }
        public Token(OpCode opCode)
        {
            this.opCode = opCode;
            this.Operand = null;
        }
        public OpCode GetOpCode() => opCode;
        public object GetOperand() => Operand;
    }
}
