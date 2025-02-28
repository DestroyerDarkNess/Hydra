using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.CodeEncryption.Stuffs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HydraEngine.Protection.ControlFlow
{
    internal class SwitchMangler
    {
        private struct Trace
        {
            public Dictionary<uint, int> RefCount;

            public Dictionary<uint, List<Instruction>> BrRefs;

            public Dictionary<uint, int> BeforeStack;

            public Dictionary<uint, int> AfterStack;

            private static void Increment(Dictionary<uint, int> counts, uint key)
            {
                if (!counts.TryGetValue(key, out var value))
                {
                    value = 0;
                }
                counts[key] = value + 1;
            }

            public Trace(CilBody body, bool hasReturnValue)
            {
                RefCount = new Dictionary<uint, int>();
                BrRefs = new Dictionary<uint, List<Instruction>>();
                BeforeStack = new Dictionary<uint, int>();
                AfterStack = new Dictionary<uint, int>();
                body.UpdateInstructionOffsets();
                foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
                {
                    BeforeStack[exceptionHandler.TryStart.Offset] = 0;
                    BeforeStack[exceptionHandler.HandlerStart.Offset] = ((exceptionHandler.HandlerType != ExceptionHandlerType.Finally) ? 1 : 0);
                    if (exceptionHandler.FilterStart != null)
                    {
                        BeforeStack[exceptionHandler.FilterStart.Offset] = 1;
                    }
                }
                int stack = 0;
                for (int i = 0; i < body.Instructions.Count; i++)
                {
                    Instruction instruction = body.Instructions[i];
                    if (BeforeStack.ContainsKey(instruction.Offset))
                    {
                        stack = BeforeStack[instruction.Offset];
                    }
                    BeforeStack[instruction.Offset] = stack;
                    instruction.UpdateStack(ref stack, hasReturnValue);
                    AfterStack[instruction.Offset] = stack;
                    switch (instruction.OpCode.FlowControl)
                    {
                        case FlowControl.Branch:
                            {
                                uint offset = ((Instruction)instruction.Operand).Offset;
                                if (!BeforeStack.ContainsKey(offset))
                                {
                                    BeforeStack[offset] = stack;
                                }
                                Increment(RefCount, offset);
                                BrRefs.AddListEntry(offset, instruction);
                                stack = 0;
                                break;
                            }
                        case FlowControl.Call:
                            if (instruction.OpCode.Code == Code.Jmp)
                            {
                                stack = 0;
                            }
                            goto case FlowControl.Break;
                        case FlowControl.Cond_Branch:
                            if (instruction.OpCode.Code == Code.Switch)
                            {
                                Instruction[] array = (Instruction[])instruction.Operand;
                                foreach (Instruction instruction2 in array)
                                {
                                    if (!BeforeStack.ContainsKey(instruction2.Offset))
                                    {
                                        BeforeStack[instruction2.Offset] = stack;
                                    }
                                    Increment(RefCount, instruction2.Offset);
                                    BrRefs.AddListEntry(instruction2.Offset, instruction);
                                }
                            }
                            else
                            {
                                uint offset = ((Instruction)instruction.Operand).Offset;
                                if (!BeforeStack.ContainsKey(offset))
                                {
                                    BeforeStack[offset] = stack;
                                }
                                Increment(RefCount, offset);
                                BrRefs.AddListEntry(offset, instruction);
                            }
                            goto case FlowControl.Break;
                        case FlowControl.Break:
                        case FlowControl.Meta:
                        case FlowControl.Next:
                            if (i + 1 < body.Instructions.Count)
                            {
                                uint offset = body.Instructions[i + 1].Offset;
                                Increment(RefCount, offset);
                            }
                            break;
                        case FlowControl.Return:
                        case FlowControl.Throw:
                            break;
                        default:
                            throw new Exception();
                    }
                }
            }

            public bool IsBranchTarget(uint offset)
            {
                if (BrRefs.TryGetValue(offset, out var value))
                {
                    return value.Count > 0;
                }
                return false;
            }

            public bool HasMultipleSources(uint offset)
            {
                if (RefCount.TryGetValue(offset, out var value))
                {
                    return value > 1;
                }
                return false;
            }
        }

        private static RandomGenerator random;

        private static Random rnd = new Random();

        public ModuleDefMD ctx { get; set; }

        private static OpCode InverseBranch(OpCode opCode)
        {
            switch (opCode.Code)
            {
                case Code.Brfalse:
                    return OpCodes.Brtrue;
                case Code.Brtrue:
                    return OpCodes.Brfalse;
                case Code.Beq:
                    return OpCodes.Bne_Un;
                case Code.Bge:
                    return OpCodes.Blt;
                case Code.Bgt:
                    return OpCodes.Ble;
                case Code.Ble:
                    return OpCodes.Bgt;
                case Code.Blt:
                    return OpCodes.Bge;
                case Code.Bne_Un:
                    return OpCodes.Beq;
                case Code.Bge_Un:
                    return OpCodes.Blt_Un;
                case Code.Bgt_Un:
                    return OpCodes.Ble_Un;
                case Code.Ble_Un:
                    return OpCodes.Bgt_Un;
                case Code.Blt_Un:
                    return OpCodes.Bge_Un;
                default:
                    throw new NotSupportedException();
            }
        }


        protected static IEnumerable<BlockParser.InstrBlock> GetAllBlocks(BlockParser.ScopeBlock scope)
        {
            foreach (BlockParser.BlockBase child in scope.Children)
            {
                if (!(child is BlockParser.InstrBlock))
                {
                    foreach (BlockParser.InstrBlock allBlock in GetAllBlocks((BlockParser.ScopeBlock)child))
                    {
                        yield return allBlock;
                    }
                }
                else
                {
                    yield return (BlockParser.InstrBlock)child;
                }
            }
        }

        public void Mangle(CilBody body, BlockParser.ScopeBlock root, ModuleDefMD Module, MethodDef Method, TypeSig retType)
        {
            random = new RandomGenerator(default(Guid).ToString(), default(Guid).ToString());
            this.ctx = Module;
            Trace trace = new Trace(body, retType.RemoveModifiers().ElementType != ElementType.Void);
            Local local = new Local(Method.Module.CorLibTypes.UInt32);
            body.Variables.Add(local);
            body.InitLocals = true;
            body.MaxStack += 2;
            IPredicate predicate = null;
            foreach (BlockParser.InstrBlock block in GetAllBlocks(root))
            {
                LinkedList<Instruction[]> statements = SplitStatements(block, trace);
                if (Method.IsInstanceConstructor)
                {
                    List<Instruction> list = new List<Instruction>();
                    while (statements.First != null)
                    {
                        list.AddRange(statements.First.Value);
                        Instruction instruction = statements.First.Value.Last();
                        statements.RemoveFirst();
                        if (instruction.OpCode == OpCodes.Call && ((IMethod)instruction.Operand).Name == ".ctor")
                        {
                            break;
                        }
                    }
                    statements.AddFirst(list.ToArray());
                }
                if (statements.Count < 3)
                {
                    continue;
                }
                int[] array = Enumerable.Range(0, statements.Count).ToArray();
                Shuffle(array);
                int[] array2 = new int[array.Length];
                int i;
                for (i = 0; i < array2.Length; i++)
                {
                    int num = random.NextInt32() & 0x7FFFFFFF;
                    array2[i] = num - num % statements.Count + array[i];
                }
                Dictionary<Instruction, int> dictionary = new Dictionary<Instruction, int>();
                LinkedListNode<Instruction[]> linkedListNode = statements.First;
                i = 0;
                while (linkedListNode != null)
                {
                    if (i != 0)
                    {
                        dictionary[linkedListNode.Value[0]] = array2[i];
                    }
                    i++;
                    linkedListNode = linkedListNode.Next;
                }
                HashSet<Instruction> statementLast = new HashSet<Instruction>(statements.Select((Instruction[] st) => st.Last()));
                Func<IList<Instruction>, bool> func = (IList<Instruction> instrs) => instrs.Any(delegate (Instruction instr)
                {
                    if (trace.HasMultipleSources(instr.Offset))
                    {
                        return true;
                    }
                    if (trace.BrRefs.TryGetValue(instr.Offset, out var value8))
                    {
                        if (value8.Any((Instruction src) => src.Operand is Instruction[]))
                        {
                            return true;
                        }
                        if (value8.Any((Instruction src) => src.Offset <= statements.First.Value.Last().Offset || src.Offset >= block.Instructions.Last().Offset))
                        {
                            return true;
                        }
                        if (value8.Any((Instruction src) => statementLast.Contains(src)))
                        {
                            return true;
                        }
                    }
                    return false;
                });
                Instruction instruction2 = new Instruction(OpCodes.Switch);
                List<Instruction> list2 = new List<Instruction>();
                if (predicate != null)
                {
                    predicate.Inititalize(body);
                    list2.Add(Instruction.CreateLdcI4(predicate.GetSwitchKey(array2[1])));
                    predicate.EmitSwitchLoad(list2);
                }
                else
                {
                    list2.Add(Instruction.CreateLdcI4(array2[1]));
                }
                list2.Add(Instruction.Create(OpCodes.Ldc_I4, array2[1]));
                list2.Add(Instruction.Create(OpCodes.Stloc, local));
                list2.Add(Instruction.Create(OpCodes.Ldc_I4, random.NextInt32()));
                list2.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
                list2.Add(Instruction.Create(OpCodes.Xor));
                list2.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
                list2.Add(Instruction.Create(OpCodes.Add));
                list2.Add(Instruction.Create(OpCodes.Stloc, local));
                Shuffler.confuse(list2);
                list2.Add(Instruction.Create(OpCodes.Dup));
                list2.Add(Instruction.Create(OpCodes.Stloc, local));
                list2.Add(Instruction.Create(OpCodes.Ldc_I4, statements.Count));
                list2.Add(Instruction.Create(OpCodes.Rem_Un));
                list2.Add(instruction2);
                AddJump(list2, statements.Last.Value[0], Method);
                AddJunk(list2, Method);
                Instruction[] array3 = new Instruction[statements.Count];
                linkedListNode = statements.First;
                i = 0;
                while (linkedListNode.Next != null)
                {
                    List<Instruction> list3 = new List<Instruction>(linkedListNode.Value);
                    if (i != 0)
                    {
                        List<Instruction> collection = list3.ToList();
                        list3.Clear();
                        Instruction instruction3 = new Instruction(OpCodes.Nop);
                        list3.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                        list3.Add(Instruction.Create(OpCodes.Brfalse, instruction3));
                        list3.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                        list3.Add(Instruction.Create(OpCodes.Brtrue, instruction3));
                        list3.AddRange(collection);
                        list3.Add(instruction3);
                        bool flag = func(list3);
                        bool flag2 = false;
                        if (list3.Last().IsBr())
                        {
                            Instruction key = (Instruction)list3.Last().Operand;
                            if (!trace.IsBranchTarget(list3.Last().Offset) && dictionary.TryGetValue(key, out var value))
                            {
                                int num2 = predicate?.GetSwitchKey(value) ?? value;
                                list3.RemoveAt(list3.Count - 1);
                                if (flag)
                                {
                                    list3.Add(Instruction.Create(OpCodes.Ldc_I4, num2));
                                }
                                else
                                {
                                    int num3 = array2[i];
                                    int num4 = random.NextInt32();
                                    list3.Add(Instruction.Create(OpCodes.Ldloc, local));
                                    list3.Add(Instruction.CreateLdcI4(num4));
                                    list3.Add(Instruction.Create(OpCodes.Add));
                                    list3.Add(Instruction.Create(OpCodes.Ldc_I4, (num3 + num4) ^ num2));
                                    list3.Add(Instruction.Create(OpCodes.Xor));
                                    list3.Add(Instruction.CreateLdcI4(num4));
                                    list3.Add(Instruction.Create(OpCodes.Add));
                                    list3.Add(Instruction.CreateLdcI4(num4));
                                    list3.Add(Instruction.Create(OpCodes.Sub));
                                    Shuffler.confuse(list3);
                                }
                                AddJump(list3, list2[1], Method);
                                AddJunk(list3, Method);
                                array3[array[i]] = list3[0];
                                flag2 = true;
                            }
                        }
                        else if (list3.Last().IsConditionalBranch())
                        {
                            Instruction key2 = (Instruction)list3.Last().Operand;
                            if (!trace.IsBranchTarget(list3.Last().Offset) && dictionary.TryGetValue(key2, out var value2))
                            {
                                int num5 = array2[i + 1];
                                OpCode opCode = list3.Last().OpCode;
                                list3.RemoveAt(list3.Count - 1);
                                if (random.NextBoolean())
                                {
                                    opCode = InverseBranch(opCode);
                                    int num6 = value2;
                                    value2 = num5;
                                    num5 = num6;
                                }
                                int num7 = array2[i];
                                int num8 = 0;
                                int num9 = 0;
                                if (!flag)
                                {
                                    num8 = random.NextInt32();
                                    random.NextInt32();
                                    num9 = num7 + num8;
                                }
                                Instruction instruction4 = Instruction.CreateLdcI4(num9 ^ (predicate?.GetSwitchKey(value2) ?? value2));
                                Instruction item = Instruction.CreateLdcI4(num9 ^ (predicate?.GetSwitchKey(num5) ?? num5));
                                Instruction instruction5 = Instruction.Create(OpCodes.Pop);
                                list3.Add(Instruction.Create(opCode, instruction4));
                                list3.Add(item);
                                list3.Add(Instruction.Create(OpCodes.Dup));
                                list3.Add(Instruction.Create(OpCodes.Br, instruction5));
                                list3.Add(instruction4);
                                list3.Add(Instruction.Create(OpCodes.Dup));
                                list3.Add(instruction5);
                                if (!flag)
                                {
                                    list3.Add(Instruction.Create(OpCodes.Ldloc, local));
                                    list3.Add(Instruction.CreateLdcI4(num8));
                                    list3.Add(Instruction.Create(OpCodes.Add));
                                    list3.Add(Instruction.Create(OpCodes.Xor));
                                    list3.Add(Instruction.CreateLdcI4(num8));
                                    list3.Add(Instruction.Create(OpCodes.Add));
                                    list3.Add(Instruction.CreateLdcI4(num8));
                                    list3.Add(Instruction.Create(OpCodes.Sub));
                                    Shuffler.confuse(list3);
                                }
                                AddJump(list3, list2[1], Method);
                                AddJunk(list3, Method);
                                array3[array[i]] = list3[0];
                                flag2 = true;
                            }
                        }
                        if (!flag2)
                        {
                            int num10 = predicate?.GetSwitchKey(array2[i + 1]) ?? array2[i + 1];
                            if (!func(list3))
                            {
                                Instruction instruction6 = Instruction.Create(OpCodes.Nop);
                                int num11 = array2[i];
                                int num12 = random.NextInt32();
                                list3.Add(Instruction.Create(OpCodes.Ldloc, local));
                                int value3 = random.NextInt32();
                                list3.Add(Instruction.CreateLdcI4(value3));
                                list3.Add(Instruction.Create(OpCodes.Add));
                                list3.Add(Instruction.CreateLdcI4(value3));
                                list3.Add(Instruction.Create(OpCodes.Sub));
                                list3.Add(Instruction.CreateLdcI4(num12));
                                list3.Add(Instruction.Create(OpCodes.Add));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, (num11 + num12) ^ num10));
                                list3.Add(Instruction.Create(OpCodes.Xor));
                                list3.Add(Instruction.CreateLdcI4(num12));
                                list3.Add(Instruction.Create(OpCodes.Add));
                                list3.Add(Instruction.CreateLdcI4(num12));
                                list3.Add(Instruction.Create(OpCodes.Sub));
                                Shuffler.cflowShuffle(list3);
                                int value4 = random.NextInt32();
                                list3.Add(Instruction.Create(OpCodes.Dup));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, value4));
                                list3.Add(Instruction.Create(OpCodes.Ceq));
                                list3.Add(Instruction.CreateLdcI4(num12));
                                list3.Add(Instruction.Create(OpCodes.Sub));
                                list3.Add(Instruction.CreateLdcI4(num12));
                                list3.Add(Instruction.Create(OpCodes.Xor));
                                Instruction instruction7 = Instruction.Create(OpCodes.Nop);
                                list3.Add(Instruction.Create(OpCodes.Brfalse_S, instruction7));
                                int value5 = random.NextInt32();
                                Local local2 = new Local(Method.Module.CorLibTypes.Int32);
                                body.Variables.Add(local2);
                                list3.Add(Instruction.CreateLdcI4(random.NextInt32()));
                                list3.Add(Instruction.Create(OpCodes.Stloc, local2));
                                list3.Add(Instruction.Create(OpCodes.Ldloc, local2));
                                list3.Add(Instruction.CreateLdcI4(value5));
                                list3.Add(Instruction.Create(OpCodes.Add));
                                list3.Add(Instruction.Create(OpCodes.Stloc, local2));
                                list3.Add(instruction7);
                                Shuffler.confuse(list3);
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, num11));
                                list3.Add(Instruction.Create(OpCodes.Stloc, local));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, num12));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
                                list3.Add(Instruction.Create(OpCodes.Xor));
                                list3.Add(Instruction.Create(OpCodes.Stloc, local));
                                list3.Add(Instruction.Create(OpCodes.Ldloc, local));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
                                list3.Add(Instruction.Create(OpCodes.Bne_Un, instruction6));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, num12));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
                                list3.Add(Instruction.Create(OpCodes.Xor));
                                list3.Add(Instruction.Create(OpCodes.Stloc, local));
                                list3.Add(instruction6);
                                Shuffler.confuse(list3);
                            }
                            else
                            {
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, num10));
                            }
                            AddJump(list3, list2[1], Method);
                            AddJunk(list3, Method);
                            array3[array[i]] = list3[0];
                        }
                    }
                    else
                    {
                        array3[array[i]] = list2[0];
                    }
                    linkedListNode.Value = list3.ToArray();
                    linkedListNode = linkedListNode.Next;
                    i++;
                }
                array3[array[i]] = linkedListNode.Value[0];
                instruction2.Operand = array3;
                Instruction[] value6 = statements.First.Value;
                statements.RemoveFirst();
                Instruction[] value7 = statements.Last.Value;
                statements.RemoveLast();
                List<Instruction[]> list4 = statements.ToList();
                Shuffle(list4);
                block.Instructions.Clear();
                block.Instructions.AddRange(value6);
                block.Instructions.AddRange(list2);
                foreach (Instruction[] item2 in list4)
                {
                    block.Instructions.AddRange(item2);
                }
                block.Instructions.AddRange(value7);
            }
        }

        private LinkedList<Instruction[]> SplitStatements(BlockParser.InstrBlock block, Trace trace)
        {
            LinkedList<Instruction[]> linkedList = default(LinkedList<Instruction[]>);
            List<Instruction> list = default(List<Instruction>);
            bool flag6 = default(bool);
            int num3 = default(int);
            bool flag5 = default(bool);
            bool flag2 = default(bool);
            Instruction[] array = default(Instruction[]);
            Instruction instruction = default(Instruction);
            Instruction instruction2 = default(Instruction);
            bool flag = default(bool);
            HashSet<Instruction> hashSet = default(HashSet<Instruction>);
            FlowControl flowControl2 = default(FlowControl);
            int num5 = default(int);
            Instruction item = default(Instruction);
            Instruction[] array2 = default(Instruction[]);
            bool flag3 = default(bool);
            FlowControl flowControl = default(FlowControl);
            LinkedList<Instruction[]> result = default(LinkedList<Instruction[]>);
            bool flag4 = default(bool);
            while (true)
            {
                int num = 1758301838;
                while (true)
                {
                    uint num2 = 1758301838u;
                    num2 = 2533766834u;
                    int num4;
                    int num6;
                    switch ((num2 = (uint)(num >> 0)) % 65)
                    {
                        case 20u:
                        case 63u:
                            num = 1139108210;
                            continue;
                        case 62u:
                            linkedList.AddLast(list.ToArray());
                            num = (((int)num2 + -1524965430) ^ -1211519232) - 0;
                            continue;
                        case 27u:
                        case 61u:
                            flag6 = num3 < block.Instructions.Count;
                            num = 452787199;
                            continue;
                        case 60u:
                            flag5 = list.Count > 0;
                            num = (((int)num2 + -1030955589) ^ -1179683267) << 0;
                            continue;
                        case 59u:
                            if (!flag6)
                            {
                                num = (int)((num2 + 470890995) ^ 0x15C06373) >> 0;
                                continue;
                            }
                            goto case 3u;
                        case 3u:
                            num = 1239590207;
                            continue;
                        case 58u:
                            if (flag2)
                            {
                                num = (((int)num2 + -1536809407) ^ -895963496) - 0;
                                continue;
                            }
                            goto case 14u;
                        case 14u:
                            array = instruction.Operand as Instruction[];
                            num = 900749752;
                            continue;
                        case 57u:
                            num3++;
                            num = (int)(((num2 + 268319629) ^ 0x1C95253D) - 0);
                            continue;
                        case 56u:
                            num4 = (trace.HasMultipleSources(block.Instructions[num3 + 1].Offset) ? 1 : 0);
                            goto IL_00f2;
                        case 55u:
                            num = (int)(((num2 + 707265091) ^ 0x17B81EDD) << 0);
                            continue;
                        case 54u:
                            instruction2 = instruction.Operand as Instruction;
                            num = (int)((num2 + 583561026) ^ 0x60FE6D77 ^ 0);
                            continue;
                        case 53u:
                            list.Clear();
                            num = ((int)num2 + -2071238116) ^ -1116572966 ^ 0;
                            continue;
                        case 52u:
                            linkedList = new LinkedList<Instruction[]>();
                            num = (int)(num2 + 1661375474) ^ -2092023454 ^ 0;
                            continue;
                        case 51u:
                            num = (((int)num2 + -863337981) ^ -2019252370) >> 0;
                            continue;
                        case 50u:
                            if (flag)
                            {
                                num = (((int)num2 + -100986504) ^ 0x2C107F9A) >> 0;
                                continue;
                            }
                            goto case 9u;
                        case 9u:
                        case 21u:
                        case 45u:
                            hashSet.Remove(instruction);
                            num = 1457370637;
                            continue;
                        case 49u:
                            num6 = ((hashSet.Count == 0) ? 1 : 0);
                            goto IL_01b5;
                        case 48u:
                            num = (int)(((num2 + 1960461734) ^ 0xB62D383Au) << 0);
                            continue;
                        case 47u:
                            if (instruction.OpCode.OpCodeType != OpCodeType.Prefix)
                            {
                                num = ((int)num2 + -661434340) ^ 0x203105CF ^ 0;
                                continue;
                            }
                            goto IL_01fa;
                        case 46u:
                            num = 428697825;
                            continue;
                        case 44u:
                            hashSet = new HashSet<Instruction>();
                            num = (int)((num2 + 1270048165) ^ 0x4D8A52CB ^ 0);
                            continue;
                        case 43u:
                            num = (int)(((num2 + 1608343532) ^ 0xAF38681Au) - 0);
                            continue;
                        case 40u:
                        case 42u:
                            if ((uint)(flowControl2 - 7) > 1u)
                            {
                                num = 647071759;
                                continue;
                            }
                            goto case 46u;
                        case 41u:
                            num5++;
                            num = (int)(((num2 + 649143086) ^ 0x6E7E80D1) + 0);
                            continue;
                        case 39u:
                            item = array2[num5];
                            num = 707670358;
                            continue;
                        case 19u:
                        case 38u:
                            if (num5 >= array2.Length)
                            {
                                num = 1548432968;
                                continue;
                            }
                            goto case 39u;
                        case 37u:
                            instruction = block.Instructions[num3];
                            num = (int)(num2 + 1704553265) ^ -377090949 ^ 0;
                            continue;
                        case 36u:
                            array2 = array;
                            num = (int)((num2 + 1892383293) ^ 0xC116874Cu) >> 0;
                            continue;
                        case 8u:
                        case 35u:
                            if (flowControl2 != FlowControl.Cond_Branch)
                            {
                                num = 1621957000;
                                continue;
                            }
                            goto case 46u;
                        case 34u:
                            num = (((int)num2 + -290999538) ^ 0x797BAE69) >> 0;
                            continue;
                        case 33u:
                            num = (((int)num2 + -40093842) ^ 0x53AECA03) - 0;
                            continue;
                        case 32u:
                            flag3 = array != null;
                            num = (int)(((num2 + 329939053) ^ 0x2A139A2E) << 0);
                            continue;
                        case 31u:
                            flowControl2 = flowControl;
                            num = (int)(((num2 + 403315589) ^ 0x49954FD4) - 0);
                            continue;
                        case 29u:
                            result = linkedList;
                            num = 1918030399;
                            continue;
                        case 28u:
                            if (flag5)
                            {
                                num = (int)(((num2 + 1381583965) ^ 0xBDA06C8Du) << 0);
                                continue;
                            }
                            goto case 29u;
                        case 26u:
                            num = 1502418702;
                            continue;
                        case 25u:
                            num = (int)(((num2 + 824698952) ^ 0x937B11ADu) + 0);
                            continue;
                        case 24u:
                            linkedList.AddLast(list.ToArray());
                            num = (((int)num2 + -1568917589) ^ -1624497680) + 0;
                            continue;
                        case 23u:
                            if (flag4)
                            {
                                num = (((int)num2 + -504175385) ^ 0x289EA948) + 0;
                                continue;
                            }
                            goto case 26u;
                        case 22u:
                            if (trace.AfterStack[instruction.Offset] == 0)
                            {
                                num = ((int)num2 + -534003067) ^ -1681150161 ^ 0;
                                continue;
                            }
                            goto IL_01fa;
                        case 18u:
                            hashSet.Add(item);
                            num = (int)((num2 + 1195113832) ^ 0x3094A185) >> 0;
                            continue;
                        case 17u:
                            num5 = 0;
                            num = (((int)num2 + -554548610) ^ 0x626426A8) - 0;
                            continue;
                        case 16u:
                            num = (int)(((num2 + 234277810) ^ 0x26400DB0) + 0);
                            continue;
                        case 15u:
                            if (flag3)
                            {
                                num = (((int)num2 + -1497151632) ^ 0x6FE5BF93) >> 0;
                                continue;
                            }
                            goto case 20u;
                        case 13u:
                            hashSet.Add(instruction2);
                            num = (int)(((num2 + 67747431) ^ 0x4F234955) + 0);
                            continue;
                        case 12u:
                            flag2 = instruction2 != null;
                            num = ((int)num2 + -1756947900) ^ -828244422 ^ 0;
                            continue;
                        case 11u:
                            num = (((int)num2 + -719845573) ^ 0x6BE16871) + 0;
                            continue;
                        case 10u:
                            flag = trace.AfterStack[instruction.Offset] != 0;
                            num = (((int)num2 + -1904678002) ^ -7420460) - 0;
                            continue;
                        case 7u:
                            if (flowControl2 != 0)
                            {
                                num = (int)((num2 + 483666874) ^ 0x78005008) >> 0;
                                continue;
                            }
                            goto case 46u;
                        case 6u:
                            flowControl = instruction.OpCode.FlowControl;
                            num = (int)((num2 + 1452403640) ^ 0x7B593BF4) >> 0;
                            continue;
                        case 5u:
                            if (num3 + 1 < block.Instructions.Count)
                            {
                                num = (int)((num2 + 757804180) ^ 0x162B3BE4 ^ 0);
                                continue;
                            }
                            num4 = 0;
                            goto IL_00f2;
                        case 4u:
                            list.Add(instruction);
                            num = (int)(((num2 + 1438922508) ^ 0xED97622Bu) << 0);
                            continue;
                        case 2u:
                            num3 = 0;
                            num = (((int)num2 + -1930605743) ^ -1143894714) + 0;
                            continue;
                        case 1u:
                            list = new List<Instruction>();
                            num = (int)((num2 + 1868189554) ^ 0x9AE0B498u) >> 0;
                            continue;
                        case 0u:
                            break;
                        default:
                            {
                                return result;
                            }
                        IL_00f2:
                            num = 600520446;
                            continue;
                        IL_01fa:
                            num6 = 0;
                            goto IL_01b5;
                        IL_01b5:
                            flag4 = (byte)num6 != 0;
                            num = 1243643203;
                            continue;
                    }
                    break;
                }
            }
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int num = list.Count - 1; num > 1; num--)
            {
                int index = new Random().Next(num + 1);
                T value = list[index];
                list[index] = list[num];
                list[num] = value;
            }
        }

        public void AddJump(IList<Instruction> instrs, Instruction target, MethodDef Method)
        {
            if (!Method.Module.IsClr40 && !Method.DeclaringType.HasGenericParameters && !Method.HasGenericParameters && (instrs[0].OpCode.FlowControl == FlowControl.Call || instrs[0].OpCode.FlowControl == FlowControl.Next))
            {
                switch (random.NextInt32(3))
                {
                    case 0:
                        instrs.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                        instrs.Add(Instruction.Create(OpCodes.Brtrue, instrs[0]));
                        break;
                    case 1:
                        instrs.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                        instrs.Add(Instruction.Create(OpCodes.Brfalse, instrs[0]));
                        break;
                    case 2:
                        {
                            bool flag = false;
                            if (random.NextBoolean())
                            {
                                TypeDef typeDef = Method.Module.Types[random.NextInt32(Method.Module.Types.Count)];
                                if (typeDef.HasMethods)
                                {
                                    instrs.Add(Instruction.Create(OpCodes.Ldtoken, typeDef.Methods[random.NextInt32(typeDef.Methods.Count)]));
                                    instrs.Add(Instruction.Create(OpCodes.Box, Method.Module.CorLibTypes.GetTypeRef("System", "RuntimeMethodHandle")));
                                    flag = true;
                                }
                            }
                            if (!flag)
                            {
                                instrs.Add(Instruction.Create(OpCodes.Ldc_I4, (!random.NextBoolean()) ? 1 : 0));
                                instrs.Add(Instruction.Create(OpCodes.Box, Method.Module.CorLibTypes.Int32.TypeDefOrRef));
                            }
                            Instruction item = Instruction.Create(OpCodes.Pop);
                            instrs.Add(Instruction.Create(OpCodes.Brfalse, instrs[0]));
                            instrs.Add(Instruction.Create(OpCodes.Ldc_I4, (!random.NextBoolean()) ? 1 : 0));
                            instrs.Add(item);
                            break;
                        }
                }
            }
            instrs.Add(Instruction.Create(OpCodes.Br, target));
        }

        public void AddJunk(IList<Instruction> instrs, MethodDef Method)
        {
            if (!Method.Module.IsClr40)
            {
                switch (random.NextInt32(6))
                {
                    case 0:
                        instrs.Add(Instruction.Create(OpCodes.Pop));
                        break;
                    case 1:
                        instrs.Add(Instruction.Create(OpCodes.Dup));
                        break;
                    case 2:
                        instrs.Add(Instruction.Create(OpCodes.Throw));
                        break;
                    case 3:
                        instrs.Add(Instruction.Create(OpCodes.Ldarg, new Parameter(255)));
                        break;
                    case 4:
                        instrs.Add(Instruction.Create(OpCodes.Ldloc, new Local(null, null, 255)));
                        break;
                    case 5:
                        instrs.Add(Instruction.Create(OpCodes.Ldtoken, Method));
                        break;
                }
            }
        }
    }
}

