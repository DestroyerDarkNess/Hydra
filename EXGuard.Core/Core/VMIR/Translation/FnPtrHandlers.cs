using System.Diagnostics;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.AST.IR;
using EXGuard.Core.AST.ILAST;

namespace EXGuard.Core.VMIR.Translation
{
    public class LdftnHandler : ITranslationHandler
    {
        public Code ILCode => Code.Ldftn;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            var retVar = tr.Context.AllocateVRegister(expr.Type.Value);
            var ecallId = tr.VM.Runtime.VMCall.LDFTN;
            var methodId = (int)tr.VM.Data.GetId((IMethod)expr.Operand);

            tr.Instructions.Add(new IRInstruction(IROpCode.PUSH, IRConstant.FromI4(0)));
            tr.Instructions.Add(new IRInstruction(IROpCode.VCALL, IRConstant.FromI4(ecallId), IRConstant.FromI4(methodId)));
            tr.Instructions.Add(new IRInstruction(IROpCode.POP, retVar));
            return retVar;
        }
    }

    public class LdvirtftnHandler : ITranslationHandler
    {
        public Code ILCode => Code.Ldvirtftn;

        public IIROperand Translate(ILASTExpression expr, IRTranslator tr)
        {
            Debug.Assert(expr.Arguments.Length == 1);
            var retVar = tr.Context.AllocateVRegister(expr.Type.Value);
            var obj = tr.Translate(expr.Arguments[0]);

            var method = (IMethod) expr.Operand;
            var methodId = (int) tr.VM.Data.GetId(method);

            var ecallId = tr.VM.Runtime.VMCall.LDFTN;
            tr.Instructions.Add(new IRInstruction(IROpCode.PUSH, obj));
            tr.Instructions.Add(new IRInstruction(IROpCode.VCALL, IRConstant.FromI4(ecallId), IRConstant.FromI4(methodId)));
            tr.Instructions.Add(new IRInstruction(IROpCode.POP, retVar));
            return retVar;
        }
    }
}