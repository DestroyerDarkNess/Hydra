using VM.Runtime.Handler;

namespace VM.Runtime.Util
{
    public class HxInstruction
    {
        public HxOpCodes OpCode { get; }
        public Value Operand { get; }

        public HxInstruction(HxOpCodes opcode, Value value = null)
        {
            OpCode = opcode;
            Operand = value ?? null;
        }
    }
}