using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace HydraEngine.Protection.ControlFlow
{
    public static class Shuffler
    {
        private static readonly Random rr = new Random();

        private static readonly OpCode[] opCodes = new OpCode[5]
        {
        OpCodes.Add,
        OpCodes.Sub,
        OpCodes.Xor,
        OpCodes.Shr,
        OpCodes.Shl
        };

        public static void confuse(List<Instruction> instructions)
        {
            int num = rr.Next(0, opCodes.Length);
            instructions.Add(Instruction.CreateLdcI4(0));
            instructions.Add(Instruction.Create(opCodes[num]));
        }

        public static void confuse(MethodDef Method, ref int i)
        {
            int num = rr.Next(0, opCodes.Length);
            Method.Body.Instructions.Insert(++i, Instruction.CreateLdcI4(0));
            Method.Body.Instructions.Insert(++i, Instruction.Create(opCodes[num]));
        }

        public static void cflowShuffle(List<Instruction> instructions)
        {
            int num = rr.Next(0, opCodes.Length);
            instructions.Add(Instruction.CreateLdcI4(0));
            instructions.Add(Instruction.Create(opCodes[num]));
        }
    }
}

