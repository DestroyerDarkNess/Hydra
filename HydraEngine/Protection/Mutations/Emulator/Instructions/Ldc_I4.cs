using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Emulator.Instructions
{
    internal class Ldc_I4 : InstructionHandler
    {
        internal override OpCode OpCode => OpCodes.Ldc_I4;

        internal override void Emulate(InstructionEmulator emulator, Instruction instr)
        {
            emulator.Push(instr.GetLdcI4Value());
        }
    }
}
