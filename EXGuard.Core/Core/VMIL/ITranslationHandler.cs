using EXGuard.Core.VMIR;
using EXGuard.Core.AST.IR;

namespace EXGuard.Core.VMIL
{
    public interface ITranslationHandler
    {
        IROpCode IRCode
        {
            get;
        }

        void Translate(IRInstruction instr, ILTranslator tr);
    }
}