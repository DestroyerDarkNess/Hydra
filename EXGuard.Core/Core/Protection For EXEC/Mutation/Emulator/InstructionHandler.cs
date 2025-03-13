using dnlib.DotNet.Emit;

namespace EXGuard.Core.EXECProtections._Mutation.Emulator
{
    internal abstract class InstructionHandler {
        internal abstract OpCode OpCode { get; }
        internal abstract void Emulate(InstructionEmulator emulator, Instruction instr);
    }
}
