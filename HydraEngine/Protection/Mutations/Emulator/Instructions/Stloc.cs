using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Emulator.Instructions
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
