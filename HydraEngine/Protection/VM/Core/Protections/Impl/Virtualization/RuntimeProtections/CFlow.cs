using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VM.Core.Protections.Impl.Virtualization.RuntimeProtections
{
    internal static class CFlow
    {
        internal static void Execute(ModuleDefMD Module)
        {
            foreach (TypeDef Type in Module.GetTypes().Where(T => T.HasMethods).ToArray())
                foreach (MethodDef Method in Type.Methods.Where(M => M.HasBody && M.Body.HasInstructions && M.Body.Instructions.Count() > 1 && !M.IsSetter && !M.IsGetter).ToArray())
                {
                    if (Method == Module.GlobalType.FindOrCreateStaticConstructor()) continue;
                    //Method.Body.SimplifyBranches();
                    Method.Body.SimplifyMacros(Method.Parameters);
                    List<Block> blocks = BlockParser.ParseMethod(Method);
                    blocks = Randomize(blocks);
                    Method.Body.Instructions.Clear();
                    Local local = new Local(Module.CorLibTypes.Int32);
                    Method.Body.Variables.Add(local);
                    Instruction target = Instruction.Create(OpCodes.Nop);
                    Instruction instr = Instruction.Create(OpCodes.Br, target);
                    foreach (Instruction instruction in Calc(0))
                        Method.Body.Instructions.Add(instruction);
                    Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
                    Method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, instr));
                    Method.Body.Instructions.Add(target);
                    foreach (Block block in blocks)
                        if (block != blocks.Single(x => x.Number == blocks.Count - 1))
                        {
                            Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                            foreach (Instruction instruction in Calc(block.Number))
                                Method.Body.Instructions.Add(instruction);
                            Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
                            Instruction instruction4 = Instruction.Create(OpCodes.Nop);
                            Method.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, instruction4));
                            foreach (Instruction instruction in block.Instructions)
                                Method.Body.Instructions.Add(instruction);
                            foreach (Instruction instruction in Calc(block.Number + 1))
                                Method.Body.Instructions.Add(instruction);

                            Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
                            Method.Body.Instructions.Add(instruction4);
                        }

                    Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                    foreach (Instruction instruction in Calc(blocks.Count - 1))
                        Method.Body.Instructions.Add(instruction);
                    Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
                    Method.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, instr));
                    Method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, blocks.Single(x => x.Number == blocks.Count - 1).Instructions[0]));
                    Method.Body.Instructions.Add(instr);
                    foreach (Instruction lastBlock in blocks.Single(x => x.Number == blocks.Count - 1).Instructions)
                        Method.Body.Instructions.Add(lastBlock);
                }
        }

        public class Block
        {
            public Block() => Instructions = new List<Instruction>();
            public List<Instruction> Instructions { get; set; }

            public int Number { get; set; }
            public int Next { get; set; }
        }
        public class BlockParser
        {
            public static List<Block> ParseMethod(MethodDef method)
            {
                List<Block> blocks = new List<Block>();
                List<Instruction> body = new List<Instruction>(method.Body.Instructions);

                Block block = new Block();
                int Id = 0;
                int usage = 0;
                block.Number = Id;
                block.Instructions.Add(Instruction.Create(OpCodes.Nop));
                blocks.Add(block);
                block = new Block();
                Stack<ExceptionHandler> handlers = new Stack<ExceptionHandler>();
                foreach (Instruction instruction in method.Body.Instructions)
                {
                    foreach (ExceptionHandler eh in method.Body.ExceptionHandlers)
                        if (eh.HandlerStart == instruction || eh.TryStart == instruction || eh.FilterStart == instruction)
                            handlers.Push(eh);

                    foreach (ExceptionHandler eh in method.Body.ExceptionHandlers)
                        if (eh.HandlerEnd == instruction || eh.TryEnd == instruction)
                            handlers.Pop();

                    int stacks, pops;
                    instruction.CalculateStackUsage(out stacks, out pops);
                    block.Instructions.Add(instruction);
                    usage += stacks - pops;
                    if (stacks == 0)
                        if (instruction.OpCode != OpCodes.Nop)
                            if ((usage == 0 || instruction.OpCode == OpCodes.Ret) && handlers.Count == 0)
                            {
                                block.Number = ++Id;
                                blocks.Add(block);
                                block = new Block();
                            }
                }

                return blocks;
            }
        }
        public static List<Block> Randomize(List<Block> input)
        {
            List<Block> ret = new List<Block>();
            foreach (Block group in input)
                ret.Insert(new Random().Next(0, ret.Count), group);
            return ret;
        }

        public static List<Instruction> Calc(int value)
        {
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, value));
            return instructions;
        }
    }
}