using dnlib.DotNet.Emit;
using EXGuard.Core.Helpers.System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SugarGuard.Protector.Protections.ControlFlow
{
    internal static class BlockParser
    {
        public static ScopeBlock ParseBody(CilBody body)
        {
            var ehScopes = new Dictionary<ExceptionHandler, Tuple<ScopeBlock, ScopeBlock, ScopeBlock>>();

            foreach (ExceptionHandler eh in body.ExceptionHandlers)
            {
                var tryBlock = new ScopeBlock(BlockType.Try, eh);
                var handlerType = BlockType.Handler;

                if (eh.HandlerType == ExceptionHandlerType.Finally)
                    handlerType = BlockType.Finally;
                else if (eh.HandlerType == ExceptionHandlerType.Fault)
                    handlerType = BlockType.Fault;

                var handlerBlock = new ScopeBlock(handlerType, eh);

                if (eh.FilterStart != null)
                {
                    var filterBlock = new ScopeBlock(BlockType.Filter, eh);
                    ehScopes[eh] = Tuple.Create(tryBlock, handlerBlock, filterBlock);
                }
                else
                    ehScopes[eh] = Tuple.Create(tryBlock, handlerBlock, (ScopeBlock)null);
            }

            var root = new ScopeBlock(BlockType.Normal, null);
            var scopeStack = new Stack<ScopeBlock>();

            scopeStack.Push(root);

            foreach (Instruction instr in body.Instructions)
            {
                foreach (ExceptionHandler eh in body.ExceptionHandlers)
                {
                    Tuple<ScopeBlock, ScopeBlock, ScopeBlock> ehScope = ehScopes[eh];

                    if (instr == eh.TryEnd)
                        scopeStack.Pop();

                    if (instr == eh.HandlerEnd)
                        scopeStack.Pop();

                    if (eh.FilterStart != null && instr == eh.HandlerStart)
                    {
                        // Filter must precede handler immediately
                        System.Diagnostics.Debug.Assert(scopeStack.Peek().Type == BlockType.Filter);
                        scopeStack.Pop();
                    }
                }

                foreach (ExceptionHandler eh in body.ExceptionHandlers.Reverse())
                {
                    Tuple<ScopeBlock, ScopeBlock, ScopeBlock> ehScope = ehScopes[eh];
                    ScopeBlock parent = scopeStack.Count > 0 ? scopeStack.Peek() : null;

                    if (instr == eh.TryStart)
                    {
                        if (parent != null)
                            parent.Children.Add(ehScope.Item1);
                        scopeStack.Push(ehScope.Item1);
                    }

                    if (instr == eh.HandlerStart)
                    {
                        if (parent != null)
                            parent.Children.Add(ehScope.Item2);
                        scopeStack.Push(ehScope.Item2);
                    }

                    if (instr == eh.FilterStart)
                    {
                        if (parent != null)
                            parent.Children.Add(ehScope.Item3);
                        scopeStack.Push(ehScope.Item3);
                    }
                }

                ScopeBlock scope = scopeStack.Peek();

                var block = scope.Children.LastOrDefault() as InstrBlock;

                if (block == null)
                    scope.Children.Add(block = new InstrBlock());

                block.Instructions.Add(instr);
            }

            foreach (ExceptionHandler eh in body.ExceptionHandlers)
            {
                if (eh.TryEnd == null)
                    scopeStack.Pop();

                if (eh.HandlerEnd == null)
                    scopeStack.Pop();
            }
            System.Diagnostics.Debug.Assert(scopeStack.Count == 1);
            return root;
        }

        internal abstract class BlockBase
        {
            public BlockBase(BlockType type)
            {
                Type = type;
            }

            public BlockType Type { get; private set; }
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
            public ScopeBlock(BlockType type, ExceptionHandler handler) : base(type)
            {
                Handler = handler;
                Children = new List<BlockBase>();
            }

            public ExceptionHandler Handler { get; private set; }

            public List<BlockBase> Children { get; set; }

            public override string ToString()
            {
                var ret = new StringBuilder();
                if (Type == BlockType.Try)
                    ret.Append("try ");
                else if (Type == BlockType.Handler)
                    ret.Append("handler ");
                else if (Type == BlockType.Finally)
                    ret.Append("finally ");
                else if (Type == BlockType.Fault)
                    ret.Append("fault ");
                ret.AppendLine("{");
                foreach (BlockBase child in Children)
                    ret.Append(child);
                ret.AppendLine("}");
                return ret.ToString();
            }

            public Instruction GetFirstInstr()
            {
                BlockBase firstBlock = Children.First();
                if (firstBlock is ScopeBlock)
                    return ((ScopeBlock)firstBlock).GetFirstInstr();
                return ((InstrBlock)firstBlock).Instructions.First();
            }

            public Instruction GetLastInstr()
            {
                BlockBase firstBlock = Children.Last();
                if (firstBlock is ScopeBlock)
                    return ((ScopeBlock)firstBlock).GetLastInstr();
                return ((InstrBlock)firstBlock).Instructions.Last();
            }

            public override void ToBody(CilBody body)
            {
                if (Type != BlockType.Normal)
                {
                    if (Type == BlockType.Try)
                    {
                        Handler.TryStart = GetFirstInstr();
                        Handler.TryEnd = GetLastInstr();
                    }
                    else if (Type == BlockType.Filter)
                    {
                        Handler.FilterStart = GetFirstInstr();
                    }
                    else
                    {
                        Handler.HandlerStart = GetFirstInstr();
                        Handler.HandlerEnd = GetLastInstr();
                    }
                }

                foreach (BlockBase block in Children)
                    block.ToBody(body);
            }
        }

        internal class InstrBlock : BlockBase
        {
            public InstrBlock() : base(BlockType.Normal)
            {
                Instructions = new List<Instruction>();
            }

            public List<Instruction> Instructions { get; set; }

            public override string ToString()
            {
                var ret = new StringBuilder();
                foreach (Instruction instr in Instructions)
                    ret.AppendLine(instr.ToString());
                return ret.ToString();
            }

            public override void ToBody(CilBody body)
            {
                foreach (Instruction instr in Instructions)
                    body.Instructions.Add(instr);
            }
        }
    }

    public class ControlFlowGraph : IEnumerable<ControlFlowBlock>
    {
        readonly List<ControlFlowBlock> blocks;
        readonly CilBody body;
        readonly int[] instrBlocks;
        readonly Dictionary<Instruction, int> indexMap;

        ControlFlowGraph(CilBody body)
        {
            this.body = body;
            instrBlocks = new int[body.Instructions.Count];
            blocks = new List<ControlFlowBlock>();

            indexMap = new Dictionary<Instruction, int>();
            for (int i = 0; i < body.Instructions.Count; i++)
                indexMap.Add(body.Instructions[i], i);
        }

        /// <summary>
        ///     Gets the number of blocks in this CFG.
        /// </summary>
        /// <value>The number of blocks.</value>
        public int Count
        {
            get { return blocks.Count; }
        }

        /// <summary>
        ///     Gets the <see cref="ControlFlowBlock" /> of the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The block with specified id.</returns>
        public ControlFlowBlock this[int id]
        {
            get { return blocks[id]; }
        }

        /// <summary>
        ///     Gets the corresponding method body.
        /// </summary>
        /// <value>The method body.</value>
        public CilBody Body
        {
            get { return body; }
        }

        IEnumerator<ControlFlowBlock> IEnumerable<ControlFlowBlock>.GetEnumerator()
        {
            return blocks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return blocks.GetEnumerator();
        }

        /// <summary>
        ///     Gets the block containing the specified instruction.
        /// </summary>
        /// <param name="instrIndex">The index of instruction.</param>
        /// <returns>The block containing the instruction.</returns>
        public ControlFlowBlock GetContainingBlock(int instrIndex)
        {
            return blocks[instrBlocks[instrIndex]];
        }

        /// <summary>
        ///     Gets the index of the specified instruction.
        /// </summary>
        /// <param name="instr">The instruction.</param>
        /// <returns>The index of instruction.</returns>
        public int IndexOf(Instruction instr)
        {
            return indexMap[instr];
        }

        void PopulateBlockHeaders(HashSet<Instruction> blockHeaders, HashSet<Instruction> entryHeaders)
        {
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction instr = body.Instructions[i];

                if (instr.Operand is Instruction)
                {
                    blockHeaders.Add((Instruction)instr.Operand);
                    if (i + 1 < body.Instructions.Count)
                        blockHeaders.Add(body.Instructions[i + 1]);
                }
                else if (instr.Operand is Instruction[])
                {
                    foreach (Instruction target in (Instruction[])instr.Operand)
                        blockHeaders.Add(target);
                    if (i + 1 < body.Instructions.Count)
                        blockHeaders.Add(body.Instructions[i + 1]);
                }
                else if ((instr.OpCode.FlowControl == FlowControl.Throw || instr.OpCode.FlowControl == FlowControl.Return) &&
                         i + 1 < body.Instructions.Count)
                {
                    blockHeaders.Add(body.Instructions[i + 1]);
                }
            }
            blockHeaders.Add(body.Instructions[0]);
            foreach (ExceptionHandler eh in body.ExceptionHandlers)
            {
                blockHeaders.Add(eh.TryStart);
                blockHeaders.Add(eh.HandlerStart);
                blockHeaders.Add(eh.FilterStart);
                entryHeaders.Add(eh.HandlerStart);
                entryHeaders.Add(eh.FilterStart);
            }
        }

        void SplitBlocks(HashSet<Instruction> blockHeaders, HashSet<Instruction> entryHeaders)
        {
            int nextBlockId = 0;
            int currentBlockId = -1;
            Instruction currentBlockHdr = null;

            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction instr = body.Instructions[i];
                if (blockHeaders.Contains(instr))
                {
                    if (currentBlockHdr != null)
                    {
                        Instruction footer = body.Instructions[i - 1];

                        var type = ControlFlowBlockType.Normal;
                        if (entryHeaders.Contains(currentBlockHdr) || currentBlockHdr == body.Instructions[0])
                            type |= ControlFlowBlockType.Entry;
                        if (footer.OpCode.FlowControl == FlowControl.Return || footer.OpCode.FlowControl == FlowControl.Throw)
                            type |= ControlFlowBlockType.Exit;

                        blocks.Add(new ControlFlowBlock(currentBlockId, type, currentBlockHdr, footer));
                    }

                    currentBlockId = nextBlockId++;
                    currentBlockHdr = instr;
                }

                instrBlocks[i] = currentBlockId;
            }
            if (blocks.Count == 0 || blocks[blocks.Count - 1].Id != currentBlockId)
            {
                Instruction footer = body.Instructions[body.Instructions.Count - 1];

                var type = ControlFlowBlockType.Normal;
                if (entryHeaders.Contains(currentBlockHdr) || currentBlockHdr == body.Instructions[0])
                    type |= ControlFlowBlockType.Entry;
                if (footer.OpCode.FlowControl == FlowControl.Return || footer.OpCode.FlowControl == FlowControl.Throw)
                    type |= ControlFlowBlockType.Exit;

                blocks.Add(new ControlFlowBlock(currentBlockId, type, currentBlockHdr, footer));
            }
        }

        void LinkBlocks()
        {
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction instr = body.Instructions[i];
                if (instr.Operand is Instruction)
                {
                    ControlFlowBlock srcBlock = blocks[instrBlocks[i]];
                    ControlFlowBlock dstBlock = blocks[instrBlocks[indexMap[(Instruction)instr.Operand]]];
                    dstBlock.Sources.Add(srcBlock);
                    srcBlock.Targets.Add(dstBlock);
                }
                else if (instr.Operand is Instruction[])
                {
                    foreach (Instruction target in (Instruction[])instr.Operand)
                    {
                        ControlFlowBlock srcBlock = blocks[instrBlocks[i]];
                        ControlFlowBlock dstBlock = blocks[instrBlocks[indexMap[target]]];
                        dstBlock.Sources.Add(srcBlock);
                        srcBlock.Targets.Add(dstBlock);
                    }
                }
            }
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Footer.OpCode.FlowControl != FlowControl.Branch &&
                    blocks[i].Footer.OpCode.FlowControl != FlowControl.Return &&
                    blocks[i].Footer.OpCode.FlowControl != FlowControl.Throw)
                {
                    blocks[i].Targets.Add(blocks[i + 1]);
                    blocks[i + 1].Sources.Add(blocks[i]);
                }
            }
        }

        /// <summary>
        ///     Constructs a CFG from the specified method body.
        /// </summary>
        /// <param name="body">The method body.</param>
        /// <returns>The CFG of the given method body.</returns>
        public static ControlFlowGraph Construct(CilBody body)
        {
            var graph = new ControlFlowGraph(body);
            if (body.Instructions.Count == 0)
                return graph;

            // Populate block headers
            var blockHeaders = new HashSet<Instruction>();
            var entryHeaders = new HashSet<Instruction>();
            graph.PopulateBlockHeaders(blockHeaders, entryHeaders);

            // Split blocks
            graph.SplitBlocks(blockHeaders, entryHeaders);

            // Link blocks
            graph.LinkBlocks();

            return graph;
        }
    }

    /// <summary>
    ///     The type of Control Flow Block
    /// </summary>
    [Flags]
    public enum ControlFlowBlockType
    {
        /// <summary>
        ///     The block is a normal block
        /// </summary>
        Normal = 0,

        /// <summary>
        ///     There are unknown edges to this block. Usually used at exception handlers / method entry.
        /// </summary>
        Entry = 1,

        /// <summary>
        ///     There are unknown edges from this block. Usually used at filter blocks / throw / method exit.
        /// </summary>
        Exit = 2
    }

    /// <summary>
    ///     A block in Control Flow Graph (CFG).
    /// </summary>
    public class ControlFlowBlock
    {
        /// <summary>
        ///     The footer instruction
        /// </summary>
        public readonly Instruction Footer;

        /// <summary>
        ///     The header instruction
        /// </summary>
        public readonly Instruction Header;

        /// <summary>
        ///     The identifier of this block
        /// </summary>
        public readonly int Id;

        /// <summary>
        ///     The type of this block
        /// </summary>
        public readonly ControlFlowBlockType Type;

        internal ControlFlowBlock(int id, ControlFlowBlockType type, Instruction header, Instruction footer)
        {
            Id = id;
            Type = type;
            Header = header;
            Footer = footer;

            Sources = new List<ControlFlowBlock>();
            Targets = new List<ControlFlowBlock>();
        }

        /// <summary>
        ///     Gets the source blocks of this control flow block.
        /// </summary>
        /// <value>The source blocks.</value>
        public IList<ControlFlowBlock> Sources { get; private set; }

        /// <summary>
        ///     Gets the target blocks of this control flow block.
        /// </summary>
        /// <value>The target blocks.</value>
        public IList<ControlFlowBlock> Targets { get; private set; }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this block.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this block.</returns>
        public override string ToString()
        {
            return string.Format("Block {0} => {1} {2}", Id, Type, string.Join(", ", Targets.Select(block => block.Id.ToString()).ToArray()));
        }
    }
}
