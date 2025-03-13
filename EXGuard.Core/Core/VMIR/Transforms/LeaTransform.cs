using System.Diagnostics;

using EXGuard.Core.AST.IR;

namespace EXGuard.Core.VMIR.Transforms
{
    public class LeaTransform : ITransform
    {
        public void Initialize(IRTransformer tr) { }

        public void Transform(IRTransformer tr)
        {
            tr.Instructions.VisitInstrs(VisitInstr, tr);
        }

        private void VisitInstr(IRInstrList instrs, IRInstruction instr, ref int index, IRTransformer tr)
        {
            if(instr.OpCode == IROpCode.__LEA)
            {
                var source = (IRPointer) instr.Operand2;
                var target = instr.Operand1;
                Debug.Assert(source.Register == IRRegister.BP);
                instrs.Replace(index, new[]
                {
                    new IRInstruction(IROpCode.MOV, target, IRRegister.BP, instr),
                    new IRInstruction(IROpCode.ADD, target, IRConstant.FromI4(source.Offset), instr)
                });
            }
        }
    }
}