﻿using dnlib.DotNet.Emit;

namespace EXGuard.Core.EXECProtections._Mutation.Emulator.Instructions
{
    internal class And : InstructionHandler
    {
        internal override OpCode OpCode => OpCodes.And;

        internal override void Emulate(InstructionEmulator emulator, Instruction instr)
        {
            var right = (int)emulator.Pop();
            var left = (int)emulator.Pop();

            emulator.Push(left & right);
        }
    }
}
