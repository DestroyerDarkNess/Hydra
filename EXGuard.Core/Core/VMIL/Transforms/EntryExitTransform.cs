using EXGuard.Core.AST.IL;

namespace EXGuard.Core.VMIL.Transforms
{
    public class EntryExitTransform : ITransform
    {
        public void Initialize(ILTransformer tr) { }

        public void Transform(ILTransformer tr)
        {
            tr.Instructions.VisitInstrs(VisitInstr, tr);
        }

        private void VisitInstr(ILInstrList instrs, ILInstruction instr, ref int index, ILTransformer tr)
        {
            if(instr.OpCode == ILOpCode.__ENTRY)
            {
                instrs.RemoveAt(index);
                index--;
            }
            else if(instr.OpCode == ILOpCode.__EXIT)
            {
                instrs[index] = new ILInstruction(ILOpCode.RET, null, instr);
            }
        }
    }
}