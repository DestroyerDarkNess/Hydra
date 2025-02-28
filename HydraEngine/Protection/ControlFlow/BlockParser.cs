#define DEBUG
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace HydraEngine.Protection.ControlFlow
{
    internal static class BlockParser
    {
        internal abstract class BlockBase
        {
            public BlockType Type { get; private set; }

            public BlockBase(BlockType type)
            {
                Type = type;
            }

            public abstract void ToBody(CilBody body);
        }

        internal enum BlockType
        {
            Normal,
            Try,
            Handler,
            Finally,
            Filter,
            Fault
        }

        internal class ScopeBlock : BlockBase
        {
            public ExceptionHandler Handler { get; private set; }

            public List<BlockBase> Children { get; set; }

            public ScopeBlock(BlockType type, ExceptionHandler handler)
                : base(type)
            {
                Handler = handler;
                Children = new List<BlockBase>();
            }

            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                if (base.Type == BlockType.Try)
                {
                    stringBuilder.Append("try ");
                }
                else if (base.Type == BlockType.Handler)
                {
                    stringBuilder.Append("handler ");
                }
                else if (base.Type == BlockType.Finally)
                {
                    stringBuilder.Append("finally ");
                }
                else if (base.Type == BlockType.Fault)
                {
                    stringBuilder.Append("fault ");
                }
                stringBuilder.AppendLine("{");
                foreach (BlockBase child in Children)
                {
                    stringBuilder.Append(child);
                }
                stringBuilder.AppendLine("}");
                return stringBuilder.ToString();
            }

            public Instruction GetFirstInstr()
            {
                BlockBase blockBase = Children.First();
                if (blockBase is ScopeBlock)
                {
                    return ((ScopeBlock)blockBase).GetFirstInstr();
                }
                return ((InstrBlock)blockBase).Instructions.First();
            }

            public Instruction GetLastInstr()
            {
                BlockBase blockBase = Children.Last();
                if (blockBase is ScopeBlock)
                {
                    return ((ScopeBlock)blockBase).GetLastInstr();
                }
                return ((InstrBlock)blockBase).Instructions.Last();
            }

            public override void ToBody(CilBody body)
            {
                if (base.Type != 0)
                {
                    if (base.Type == BlockType.Try)
                    {
                        Handler.TryStart = GetFirstInstr();
                        Handler.TryEnd = GetLastInstr();
                    }
                    else if (base.Type == BlockType.Filter)
                    {
                        Handler.FilterStart = GetFirstInstr();
                    }
                    else
                    {
                        Handler.HandlerStart = GetFirstInstr();
                        Handler.HandlerEnd = GetLastInstr();
                    }
                }
                foreach (BlockBase child in Children)
                {
                    child.ToBody(body);
                }
            }
        }

        internal class InstrBlock : BlockBase
        {
            public List<Instruction> Instructions { get; set; }

            public InstrBlock()
                : base(BlockType.Normal)
            {
                Instructions = new List<Instruction>();
            }

            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (Instruction instruction in Instructions)
                {
                    stringBuilder.AppendLine(instruction.ToString());
                }
                return stringBuilder.ToString();
            }

            public override void ToBody(CilBody body)
            {
                foreach (Instruction instruction in Instructions)
                {
                    body.Instructions.Add(instruction);
                }
            }
        }

        public static ScopeBlock ParseBody(CilBody body)
        {
            Dictionary<ExceptionHandler, Tuple<ScopeBlock, ScopeBlock, ScopeBlock>> dictionary = new Dictionary<ExceptionHandler, Tuple<ScopeBlock, ScopeBlock, ScopeBlock>>();
            foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
            {
                ScopeBlock item = new ScopeBlock(BlockType.Try, exceptionHandler);
                BlockType type = BlockType.Handler;
                if (exceptionHandler.HandlerType == ExceptionHandlerType.Finally)
                {
                    type = BlockType.Finally;
                }
                else if (exceptionHandler.HandlerType == ExceptionHandlerType.Fault)
                {
                    type = BlockType.Fault;
                }
                ScopeBlock item2 = new ScopeBlock(type, exceptionHandler);
                if (exceptionHandler.FilterStart != null)
                {
                    ScopeBlock item3 = new ScopeBlock(BlockType.Filter, exceptionHandler);
                    dictionary[exceptionHandler] = Tuple.Create(item, item2, item3);
                }
                else
                {
                    dictionary[exceptionHandler] = Tuple.Create<ScopeBlock, ScopeBlock, ScopeBlock>(item, item2, null);
                }
            }
            ScopeBlock scopeBlock = new ScopeBlock(BlockType.Normal, null);
            Stack<ScopeBlock> stack = new Stack<ScopeBlock>();
            stack.Push(scopeBlock);
            foreach (Instruction instruction in body.Instructions)
            {
                foreach (ExceptionHandler exceptionHandler2 in body.ExceptionHandlers)
                {
                    _ = dictionary[exceptionHandler2];
                    if (instruction == exceptionHandler2.TryEnd)
                    {
                        stack.Pop();
                    }
                    if (instruction == exceptionHandler2.HandlerEnd)
                    {
                        stack.Pop();
                    }
                    if (exceptionHandler2.FilterStart != null && instruction == exceptionHandler2.HandlerStart)
                    {
                        Debug.Assert(stack.Peek().Type == BlockType.Filter);
                        stack.Pop();
                    }
                }
                foreach (ExceptionHandler item4 in body.ExceptionHandlers.Reverse())
                {
                    Tuple<ScopeBlock, ScopeBlock, ScopeBlock> tuple = dictionary[item4];
                    ScopeBlock scopeBlock2 = ((stack.Count > 0) ? stack.Peek() : null);
                    if (instruction == item4.TryStart)
                    {
                        scopeBlock2?.Children.Add(tuple.Item1);
                        stack.Push(tuple.Item1);
                    }
                    if (instruction == item4.HandlerStart)
                    {
                        scopeBlock2?.Children.Add(tuple.Item2);
                        stack.Push(tuple.Item2);
                    }
                    if (instruction == item4.FilterStart)
                    {
                        scopeBlock2?.Children.Add(tuple.Item3);
                        stack.Push(tuple.Item3);
                    }
                }
                ScopeBlock scopeBlock3 = stack.Peek();
                InstrBlock instrBlock = scopeBlock3.Children.LastOrDefault() as InstrBlock;
                if (instrBlock == null)
                {
                    scopeBlock3.Children.Add(instrBlock = new InstrBlock());
                }
                instrBlock.Instructions.Add(instruction);
            }
            foreach (ExceptionHandler exceptionHandler3 in body.ExceptionHandlers)
            {
                if (exceptionHandler3.TryEnd == null)
                {
                    stack.Pop();
                }
                if (exceptionHandler3.HandlerEnd == null)
                {
                    stack.Pop();
                }
            }
            Debug.Assert(stack.Count == 1);
            return scopeBlock;
        }
    }
}


