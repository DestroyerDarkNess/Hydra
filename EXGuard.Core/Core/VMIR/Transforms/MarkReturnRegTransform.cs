using EXGuard.Core.AST;
using EXGuard.Core.AST.IR;

namespace EXGuard.Core.VMIR.Transforms
{
    public class MarkReturnRegTransform : ITransform
    {
        public void Initialize(IRTransformer tr) { }

        public void Transform(IRTransformer tr)
        {
            tr.Instructions.VisitInstrs(VisitInstr, tr);
        }

        private void VisitInstr(IRInstrList instrs, IRInstruction instr, ref int index, IRTransformer tr)
        {
            var callInfo = instr.Annotation as InstrCallInfo;
            if(callInfo == null || callInfo.ReturnValue == null)
                return;

            if(instr.Operand1 is IRRegister && ((IRRegister) instr.Operand1).SourceVariable == callInfo.ReturnValue) callInfo.ReturnRegister = (IRRegister) instr.Operand1;
            else if(instr.Operand1 is IRPointer && ((IRPointer) instr.Operand1).SourceVariable == callInfo.ReturnValue) callInfo.ReturnSlot = (IRPointer) instr.Operand1;
        }
    }
}