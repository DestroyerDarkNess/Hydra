using EXGuard.Core.VM;
using EXGuard.Core.AST.IL;

namespace EXGuard.Core.VMIL.Transforms
{
    public class SaveInfoTransform : ITransform
    {
        private VMMethodInfo methodInfo;

        public void Initialize(ILTransformer tr)
        {
            methodInfo = tr.VM.Data.LookupInfo(tr.Method);
            methodInfo.RootScope = tr.RootScope;
            tr.VM.Data.SetInfo(tr.Method, methodInfo);
        }

        public void Transform(ILTransformer tr)
        {
            tr.Instructions.VisitInstrs(VisitInstr, tr);
        }

        private void VisitInstr(ILInstrList instrs, ILInstruction instr, ref int index, ILTransformer tr)
        {
            if(instr.Operand is ILRegister)
            {
                var reg = ((ILRegister) instr.Operand).Register;
                if(reg.IsGPR())
                    methodInfo.UsedRegister.Add(reg);
            }
        }
    }
}