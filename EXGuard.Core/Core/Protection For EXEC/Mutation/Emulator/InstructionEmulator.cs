using System;
using System.Linq;
using System.Collections.Generic;

using dnlib.DotNet.Emit;

using EXGuard.Core.EXECProtections._Mutation.Blocks;

namespace EXGuard.Core.EXECProtections._Mutation.Emulator
{
    internal class InstructionEmulator
    {
        Dictionary<OpCode, InstructionHandler> _instructions;
        Dictionary<Local, object> _locals;
        Stack<object> _stack;
        public InstructionEmulator()
        {
            _instructions = new Dictionary<OpCode, InstructionHandler>();
            _locals = new Dictionary<Local, object>();

            var emuInstructions = typeof(InstructionHandler).Assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(InstructionHandler)) && !t.IsAbstract)
                .Select(t => (InstructionHandler)Activator.CreateInstance(t))
                .ToList();

            foreach (var instrEmu in emuInstructions)
            {
                _instructions.Add(instrEmu.OpCode, instrEmu);
            }

            _stack = new Stack<object>();
        }

        public void Emulate(Instruction instruction) {
            if (_instructions.TryGetValue(instruction.OpCode, out var cilInstr)) {
                cilInstr.Emulate(this, instruction);
            }
        }

        public void Emulate(Block block) {
            foreach (var instr in block.Instructions)
                Emulate(instr);
        }

        public object Pop() => _stack.Pop();
        public void Push(object value) => _stack.Push(value);

        public object GetLocalValue(Local local) => _locals[local];
        public void SetLocalValue(Local local, object value) => _locals[local] = value;
    }
}
