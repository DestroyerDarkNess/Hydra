using dnlib.DotNet.Emit;

namespace EXGuard.Core.EXECProtections._Mutation.Emulator.Instructions
{
    internal class Stloc : InstructionHandler
    {
        internal override OpCode OpCode => OpCodes.Stloc;

        internal override void Emulate(InstructionEmulator emulator, Instruction instr)
        {
            var value = emulator.Pop();
            emulator.SetLocalValue(instr.Operand as Local, value);
        }
    }
}
