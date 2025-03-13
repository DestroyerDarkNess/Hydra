using System.Diagnostics;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.AST;
using EXGuard.Core.AST.IR;
using EXGuard.Core.AST.ILAST;

namespace EXGuard.Core.VMIR.Translation
{
    public class LdlocHandler : ITranslationHandler
    {
        public Code ILCode => Code.Ldloc;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            var local = tr.Context.ResolveLocal((Local) expr.Operand);
            var ret = tr.Context.AllocateVRegister(local.Type);
            tr.Instructions.Add(new IRInstruction(IROpCode.MOV, ret, local));

            if(local.RawType.ElementType == ElementType.I1 ||
               local.RawType.ElementType == ElementType.I2)
            {
                ret.RawType = local.RawType;
                var r = tr.Context.AllocateVRegister(local.Type);
                tr.Instructions.Add(new IRInstruction(IROpCode.SX, r, ret));
                ret = r;
            }
            return ret;
        }
    }

    public class StlocHandler : ITranslationHandler
    {
        public Code ILCode => Code.Stloc;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);
            tr.Instructions.Add(new IRInstruction(IROpCode.MOV)
            {
                Operand1 = tr.Context.ResolveLocal((Local) expr.Operand),
                Operand2 = tr.Translate(expr.Arguments[0])
            });
            return null;
        }
    }

    public class LdlocaHandler : ITranslationHandler
    {
        public Code ILCode => Code.Ldloca;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            var local = tr.Context.ResolveLocal((Local) expr.Operand);
            var ret = tr.Context.AllocateVRegister(ASTType.ByRef);
            tr.Instructions.Add(new IRInstruction(IROpCode.__LEA, ret, local));
            return ret;
        }
    }
}