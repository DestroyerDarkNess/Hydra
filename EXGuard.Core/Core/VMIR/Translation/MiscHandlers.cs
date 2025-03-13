using System.Diagnostics;

using dnlib.DotNet.Emit;

using EXGuard.Core.AST;
using EXGuard.Core.AST.IR;
using EXGuard.Core.AST.ILAST;

namespace EXGuard.Core.VMIR.Translation
{
    public class DupHandler : ITranslationHandler
    {
        public Code ILCode => Code.Dup;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);
            var ret = tr.Context.AllocateVRegister(expr.Type.Value);
            tr.Instructions.Add(new IRInstruction(IROpCode.MOV)
            {
                Operand1 = ret,
                Operand2 = tr.Translate(expr.Arguments[0])
            });
            return ret;
        }
    }

    public class NopHandler : ITranslationHandler
    {
        public Code ILCode => Code.Nop;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            tr.Instructions.Add(new IRInstruction(IROpCode.NOP));
            return null;
        }
    }

    public class BreakHandler : ITranslationHandler
    {
        public Code ILCode => Code.Break;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            var ecallId = tr.VM.Runtime.VMCall.BREAK;
            tr.Instructions.Add(new IRInstruction(IROpCode.VCALL, IRConstant.FromI4(ecallId)));
            return null;
        }
    }

    public class CkfiniteHandler : ITranslationHandler
    {
        public Code ILCode => Code.Ckfinite;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);
            var value = tr.Translate(expr.Arguments[0]);
            var ecallId = tr.VM.Runtime.VMCall.CKFINITE;
            if(value.Type == ASTType.R4)
                tr.Instructions.Add(new IRInstruction(IROpCode.__SETF)
                {
                    Operand1 = IRConstant.FromI4(1 << tr.Arch.Flags.UNSIGNED)
                });
            tr.Instructions.Add(new IRInstruction(IROpCode.VCALL, IRConstant.FromI4(ecallId), value));
            return value;
        }
    }

    public class PopHandler : ITranslationHandler
    {
        public Code ILCode => Code.Pop;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);
            tr.Translate(expr.Arguments[0]);
            return null;
        }
    }
}