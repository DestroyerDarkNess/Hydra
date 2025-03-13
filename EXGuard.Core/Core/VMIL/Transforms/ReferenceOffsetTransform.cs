using EXGuard.Core.AST.IL;

namespace EXGuard.Core.VMIL.Transforms
{
    public class ReferenceOffsetTransform : ITransform
    {
        public void Initialize(ILTransformer tr) { }

        public void Transform(ILTransformer tr)
        {
            tr.Instructions.VisitInstrs(VisitInstr, tr);
        }

        private void VisitInstr(ILInstrList instrs, ILInstruction instr, ref int index, ILTransformer tr)
        {
            if(instr.OpCode == ILOpCode.PUSHI_DWORD && instr.Operand is IHasOffset)
            {
                var relBase = new ILInstruction(ILOpCode.PUSHR_QWORD, ILRegister.IP, instr);
                instr.OpCode = ILOpCode.PUSHI_DWORD;
                instr.Operand = new ILRelReference((IHasOffset) instr.Operand, relBase);

                instrs.Replace(index, new[]
                {
                    relBase,
                    instr,
                    new ILInstruction(ILOpCode.ADD_QWORD, null, instr)
                });
            }
        }
    }
}