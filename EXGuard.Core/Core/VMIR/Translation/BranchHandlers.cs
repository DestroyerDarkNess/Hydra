using System.Diagnostics;

using dnlib.DotNet.Emit;

using EXGuard.Core.AST;
using EXGuard.Core.CFG;
using EXGuard.Core.AST.IR;
using EXGuard.Core.AST.ILAST;

namespace EXGuard.Core.VMIR.Translation
{
    public class BrHandler : ITranslationHandler
    {
        public Code ILCode => Code.Br;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            tr.Instructions.Add(new IRInstruction(IROpCode.JMP)
            {
                Operand1 = new IRBlockTarget((IBasicBlock) expr.Operand)
            });
            return null;
        }
    }

    public class BrtrueHandler : ITranslationHandler
    {
        public Code ILCode => Code.Brtrue;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);

            var val = tr.Translate(expr.Arguments[0]);
            TranslationHelpers.EmitCompareEq(tr, expr.Arguments[0].Type.Value, val, IRConstant.FromI4(0));
            var tmp = tr.Context.AllocateVRegister(ASTType.I4);
            tr.Instructions.Add(new IRInstruction(IROpCode.__GETF)
            {
                Operand1 = tmp,
                Operand2 = IRConstant.FromI4(1 << tr.Arch.Flags.ZERO)
            });
            tr.Instructions.Add(new IRInstruction(IROpCode.JZ)
            {
                Operand1 = new IRBlockTarget((IBasicBlock) expr.Operand),
                Operand2 = tmp
            });
            return null;
        }
    }

    public class BrfalseHandler : ITranslationHandler
    {
        public Code ILCode => Code.Brfalse;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);

            var val = tr.Translate(expr.Arguments[0]);
            TranslationHelpers.EmitCompareEq(tr, expr.Arguments[0].Type.Value, val, IRConstant.FromI4(0));
            var tmp = tr.Context.AllocateVRegister(ASTType.I4);
            tr.Instructions.Add(new IRInstruction(IROpCode.__GETF)
            {
                Operand1 = tmp,
                Operand2 = IRConstant.FromI4(1 << tr.Arch.Flags.ZERO)
            });
            tr.Instructions.Add(new IRInstruction(IROpCode.JNZ)
            {
                Operand1 = new IRBlockTarget((IBasicBlock) expr.Operand),
                Operand2 = tmp
            });
            return null;
        }
    }

    public class SwitchHandler : ITranslationHandler
    {
        public Code ILCode => Code.Switch;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);

            var val = tr.Translate(expr.Arguments[0]);
            tr.Instructions.Add(new IRInstruction(IROpCode.SWT)
            {
                Operand1 = new IRJumpTable((IBasicBlock[]) expr.Operand),
                Operand2 = val
            });
            return null;
        }
    }
}