using dnlib.DotNet.Emit;
using System.Collections;
using System.Collections.Generic;

namespace HydraEngine.Protection.ControlFlow
{
    public class ControlFlowGraph : IEnumerable<ControlFlowBlock>, IEnumerable
    {
        private readonly List<ControlFlowBlock> blocks;

        private readonly CilBody body;

        private readonly int[] instrBlocks;

        private readonly Dictionary<Instruction, int> indexMap;

        public int Count => blocks.Count;

        public ControlFlowBlock this[int id] => blocks[id];

        public CilBody Body => body;

        private ControlFlowGraph(CilBody body)
        {
            this.body = body;
            instrBlocks = new int[body.Instructions.Count];
            blocks = new List<ControlFlowBlock>();
            indexMap = new Dictionary<Instruction, int>();
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                indexMap.Add(body.Instructions[i], i);
            }
        }

        IEnumerator<ControlFlowBlock> IEnumerable<ControlFlowBlock>.GetEnumerator()
        {
            return blocks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return blocks.GetEnumerator();
        }

        public ControlFlowBlock GetContainingBlock(int instrIndex)
        {
            return blocks[instrBlocks[instrIndex]];
        }

        public int IndexOf(Instruction instr)
        {
            return indexMap[instr];
        }

        private void PopulateBlockHeaders(HashSet<Instruction> blockHeaders, HashSet<Instruction> entryHeaders)
        {
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction instruction = body.Instructions[i];
                if (instruction.Operand is Instruction)
                {
                    blockHeaders.Add((Instruction)instruction.Operand);
                    if (i + 1 < body.Instructions.Count)
                    {
                        blockHeaders.Add(body.Instructions[i + 1]);
                    }
                }
                else if (instruction.Operand is Instruction[])
                {
                    Instruction[] array = (Instruction[])instruction.Operand;
                    foreach (Instruction item in array)
                    {
                        blockHeaders.Add(item);
                    }
                    if (i + 1 < body.Instructions.Count)
                    {
                        blockHeaders.Add(body.Instructions[i + 1]);
                    }
                }
                else if ((instruction.OpCode.FlowControl == FlowControl.Throw || instruction.OpCode.FlowControl == FlowControl.Return) && i + 1 < body.Instructions.Count)
                {
                    blockHeaders.Add(body.Instructions[i + 1]);
                }
            }
            blockHeaders.Add(body.Instructions[0]);
            foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
            {
                blockHeaders.Add(exceptionHandler.TryStart);
                blockHeaders.Add(exceptionHandler.HandlerStart);
                blockHeaders.Add(exceptionHandler.FilterStart);
                entryHeaders.Add(exceptionHandler.HandlerStart);
                entryHeaders.Add(exceptionHandler.FilterStart);
            }
        }

        private void SplitBlocks(HashSet<Instruction> blockHeaders, HashSet<Instruction> entryHeaders)
        {
            int num = 0;
            int num2 = -1;
            Instruction instruction = null;
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction instruction2 = body.Instructions[i];
                if (blockHeaders.Contains(instruction2))
                {
                    if (instruction != null)
                    {
                        Instruction instruction3 = body.Instructions[i - 1];
                        ControlFlowBlockType controlFlowBlockType = (ControlFlowBlockType)0;
                        if (entryHeaders.Contains(instruction) || instruction == body.Instructions[0])
                        {
                            controlFlowBlockType |= ControlFlowBlockType.Entry;
                        }
                        if (instruction3.OpCode.FlowControl == FlowControl.Return || instruction3.OpCode.FlowControl == FlowControl.Throw)
                        {
                            controlFlowBlockType |= (ControlFlowBlockType)2;
                        }
                        blocks.Add(new ControlFlowBlock(num2, controlFlowBlockType, instruction, instruction3));
                    }
                    num2 = num++;
                    instruction = instruction2;
                }
                instrBlocks[i] = num2;
            }
            if (blocks.Count == 0 || blocks[blocks.Count - 1].Id != num2)
            {
                Instruction instruction4 = body.Instructions[body.Instructions.Count - 1];
                ControlFlowBlockType controlFlowBlockType2 = (ControlFlowBlockType)0;
                if (entryHeaders.Contains(instruction) || instruction == body.Instructions[0])
                {
                    controlFlowBlockType2 |= ControlFlowBlockType.Entry;
                }
                if (instruction4.OpCode.FlowControl == FlowControl.Return || instruction4.OpCode.FlowControl == FlowControl.Throw)
                {
                    controlFlowBlockType2 |= (ControlFlowBlockType)2;
                }
                blocks.Add(new ControlFlowBlock(num2, controlFlowBlockType2, instruction, instruction4));
            }
        }

        private void LinkBlocks()
        {
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction instruction = body.Instructions[i];
                if (instruction.Operand is Instruction)
                {
                    ControlFlowBlock controlFlowBlock = blocks[instrBlocks[i]];
                    ControlFlowBlock controlFlowBlock2 = blocks[instrBlocks[indexMap[(Instruction)instruction.Operand]]];
                    controlFlowBlock2.Sources.Add(controlFlowBlock);
                    controlFlowBlock.Targets.Add(controlFlowBlock2);
                }
                else if (instruction.Operand is Instruction[])
                {
                    Instruction[] array = (Instruction[])instruction.Operand;
                    foreach (Instruction key in array)
                    {
                        ControlFlowBlock controlFlowBlock3 = blocks[instrBlocks[i]];
                        ControlFlowBlock controlFlowBlock4 = blocks[instrBlocks[indexMap[key]]];
                        controlFlowBlock4.Sources.Add(controlFlowBlock3);
                        controlFlowBlock3.Targets.Add(controlFlowBlock4);
                    }
                }
            }
            for (int k = 0; k < blocks.Count; k++)
            {
                if (blocks[k].Footer.OpCode.FlowControl != 0 && blocks[k].Footer.OpCode.FlowControl != FlowControl.Return && blocks[k].Footer.OpCode.FlowControl != FlowControl.Throw)
                {
                    blocks[k].Targets.Add(blocks[k + 1]);
                    blocks[k + 1].Sources.Add(blocks[k]);
                }
            }
        }

        public static ControlFlowGraph Construct(CilBody body)
        {
            ControlFlowGraph controlFlowGraph = new ControlFlowGraph(body);
            if (body.Instructions.Count == 0)
            {
                return controlFlowGraph;
            }
            HashSet<Instruction> blockHeaders = new HashSet<Instruction>();
            HashSet<Instruction> entryHeaders = new HashSet<Instruction>();
            controlFlowGraph.PopulateBlockHeaders(blockHeaders, entryHeaders);
            controlFlowGraph.SplitBlocks(blockHeaders, entryHeaders);
            controlFlowGraph.LinkBlocks();
            return controlFlowGraph;
        }
    }
}

