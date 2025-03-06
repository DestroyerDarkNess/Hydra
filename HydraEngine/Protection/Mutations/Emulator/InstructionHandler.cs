using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Emulator
{
    internal abstract class InstructionHandler
    {
        internal abstract OpCode OpCode { get; }
        internal abstract void Emulate(InstructionEmulator emulator, Instruction instr);
    }
}
