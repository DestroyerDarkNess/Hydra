using dnlib.DotNet.Emit;

using EXGuard.Core.AST.IR;
using EXGuard.Core.AST.ILAST;

namespace EXGuard.Core.VMIR
{
    public interface ITranslationHandler
    {
        Code ILCode
        {
            get;
        }

        IIROperand Translate(ILASTExpression expr, IRTranslator tr);
    }
}