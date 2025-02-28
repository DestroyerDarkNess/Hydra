using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Emulator.Instructions
{
    internal class Add : InstructionHandler
    {
        internal override OpCode OpCode => OpCodes.Add;

        internal override void Emulate(InstructionEmulator emulator, Instruction instr)
        {
            var right = (int)emulator.Pop();
            var left = (int)emulator.Pop();

            emulator.Push(left + right);
        }
    }
}
