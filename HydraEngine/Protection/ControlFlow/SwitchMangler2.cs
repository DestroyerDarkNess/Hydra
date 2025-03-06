using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.CodeEncryption.Stuffs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HydraEngine.Protection.ControlFlow
{
    internal class SwitchMangler2
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
                    if (trace.BrRefs.TryGetValue(instr.Offset, out var value5))
                    {
                        if (value5.Any((Instruction src) => src.Operand is Instruction[]))
                        {
                            return true;
                        }
                        if (value5.Any((Instruction src) => src.Offset <= statements.First.Value.Last().Offset || src.Offset >= block.Instructions.Last().Offset))
                        {
                            return true;
                        }
                        if (value5.Any((Instruction src) => statementLast.Contains(src)))
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
                                Instruction.Create(OpCodes.Nop);
                                int num11 = array2[i];
                                int num12 = random.NextInt32();
                                list3.Add(Instruction.Create(OpCodes.Ldloc, local));
                                list3.Add(Instruction.CreateLdcI4(num12));
                                list3.Add(Instruction.Create(OpCodes.Add));
                                list3.Add(Instruction.Create(OpCodes.Ldc_I4, (num11 + num12) ^ num10));
                                list3.Add(Instruction.Create(OpCodes.Xor));
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
                Instruction[] value3 = statements.First.Value;
                statements.RemoveFirst();
                Instruction[] value4 = statements.Last.Value;
                statements.RemoveLast();
                List<Instruction[]> list4 = statements.ToList();
                Shuffle(list4);
                block.Instructions.Clear();
                block.Instructions.AddRange(value3);
                block.Instructions.AddRange(list2);
                foreach (Instruction[] item2 in list4)
                {
                    block.Instructions.AddRange(item2);
                }
                block.Instructions.AddRange(value4);
            }
        }

        private static int[] GenerateArray()
        {
            int[] array = new int[rnd.Next(2, 4)];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = rnd.Next(2, 4);
            }
            return array;
        }

        private static void InjectArray(MethodDef method, int[] array, ref List<Instruction> toInject, Local local)
        {
            List<Instruction> list = new List<Instruction>
        {
            OpCodes.Ldc_I4.ToInstruction(array.Length),
            OpCodes.Newarr.ToInstruction(method.Module.CorLibTypes.UInt32),
            OpCodes.Stloc_S.ToInstruction(local)
        };
            for (int i = 0; i < array.Length; i++)
            {
                if (i == 0)
                {
                    list.Add(OpCodes.Ldloc_S.ToInstruction(local));
                    list.Add(OpCodes.Ldc_I4.ToInstruction(i));
                    list.Add(OpCodes.Ldc_I4.ToInstruction(array[i]));
                    list.Add(OpCodes.Stelem_I4.ToInstruction());
                    list.Add(OpCodes.Nop.ToInstruction());
                    continue;
                }
                int num = array[i];
                list.Add(OpCodes.Ldloc_S.ToInstruction(local));
                list.Add(OpCodes.Ldc_I4.ToInstruction(i));
                list.Add(OpCodes.Ldc_I4.ToInstruction(num));
                int index = list.Count - 1;
                for (int num2 = i - 1; num2 >= 0; num2--)
                {
                    OpCode opCode = null;
                    switch (rnd.Next(0, 2))
                    {
                        case 1:
                            num -= array[num2];
                            opCode = OpCodes.Add;
                            break;
                        case 0:
                            num += array[num2];
                            opCode = OpCodes.Sub;
                            break;
                    }
                    list.Add(OpCodes.Ldloc_S.ToInstruction(local));
                    list.Add(OpCodes.Ldc_I4.ToInstruction(num2));
                    list.Add(OpCodes.Ldelem_I4.ToInstruction());
                    list.Add(opCode.ToInstruction());
                }
                list[index].OpCode = OpCodes.Ldc_I4;
                list[index].Operand = num;
                list.Add(OpCodes.Stelem_I4.ToInstruction());
                list.Add(OpCodes.Nop.ToInstruction());
            }
            for (int j = 0; j < list.Count; j++)
            {
                toInject.Add(list[j]);
            }
        }

        private LinkedList<Instruction[]> SplitStatements(BlockParser.InstrBlock block, Trace trace)
        {
            FlowControl flowControl2 = default(FlowControl);
            LinkedList<Instruction[]> result = default(LinkedList<Instruction[]>);
            LinkedList<Instruction[]> linkedList = default(LinkedList<Instruction[]>);
            List<Instruction> list = default(List<Instruction>);
            bool flag4 = default(bool);
            HashSet<Instruction> hashSet = default(HashSet<Instruction>);
            bool flag5 = default(bool);
            int num3 = default(int);
            FlowControl flowControl = default(FlowControl);
            Instruction instruction = default(Instruction);
            int num4 = default(int);
            Instruction item = default(Instruction);
            Instruction[] array2 = default(Instruction[]);
            Instruction[] array = default(Instruction[]);
            bool flag2 = default(bool);
            Instruction instruction2 = default(Instruction);
            bool flag = default(bool);
            bool flag6 = default(bool);
            bool flag3 = default(bool);
            while (true)
            {
                int num = 1758301840;
                while (true)
                {
                    uint num2 = 1758301840u;
                    num2 = 2533766834u;
                    int num6;
                    int num5;
                    switch ((num2 = (uint)(num << 0)) % 65)
                    {
                        case 4u:
                        case 64u:
                            if (flowControl2 != FlowControl.Cond_Branch)
                            {
                                num = 1621956979;
                                continue;
                            }
                            goto case 22u;
                        case 22u:
                            num = 428697838;
                            continue;
                        case 63u:
                            result = linkedList;
                            num = 1918030352;
                            continue;
                        case 62u:
                            linkedList.AddLast(list.ToArray());
                            num = ((int)num2 + -1524965430) ^ -1211519198 ^ 0;
                            continue;
                        case 61u:
                            if (flag4)
                            {
                                num = (int)(num2 + 1381583965) ^ -1113559892 ^ 0;
                                continue;
                            }
                            goto case 63u;
                        case 60u:
                            hashSet = new HashSet<Instruction>();
                            num = (int)(((num2 + 1270048165) ^ 0x4D8A52CE) + 0);
                            continue;
                        case 59u:
                            if (!flag5)
                            {
                                num = (int)((num2 + 470890995) ^ 0x15C063A7) >> 0;
                                continue;
                            }
                            goto case 28u;
                        case 28u:
                            num = 1239590202;
                            continue;
                        case 1u:
                        case 58u:
                            flag5 = num3 < block.Instructions.Count;
                            num = 452787199;
                            continue;
                        case 57u:
                            if (flowControl2 != 0)
                            {
                                num = (int)((num2 + 483666874) ^ 0x78005059) >> 0;
                                continue;
                            }
                            goto case 22u;
                        case 56u:
                            num = 1502418660;
                            continue;
                        case 55u:
                            num = (int)(((num2 + 707265091) ^ 0x17B81EBB) + 0);
                            continue;
                        case 54u:
                            num = (int)(((num2 + 824698952) ^ 0x937B11B0u) + 0);
                            continue;
                        case 53u:
                            flowControl2 = flowControl;
                            num = (int)(((num2 + 403315589) ^ 0x49954F98) << 0);
                            continue;
                        case 52u:
                            linkedList.AddLast(list.ToArray());
                            num = ((int)num2 + -1568917589) ^ -1624497715 ^ 0;
                            continue;
                        case 51u:
                            num = (((int)num2 + -863337981) ^ -2019252414) - 0;
                            continue;
                        case 50u:
                            list = new List<Instruction>();
                            num = (int)(((num2 + 1868189554) ^ 0x9AE0B49Bu) - 0);
                            continue;
                        case 49u:
                            num6 = ((hashSet.Count == 0) ? 1 : 0);
                            goto IL_0182;
                        case 48u:
                            if (trace.AfterStack[instruction.Offset] == 0)
                            {
                                num = (((int)num2 + -534003067) ^ -1681150015) + 0;
                                continue;
                            }
                            goto IL_01b7;
                        case 47u:
                            if (instruction.OpCode.OpCodeType != OpCodeType.Prefix)
                            {
                                num = (((int)num2 + -661434340) ^ 0x20310229) >> 0;
                                continue;
                            }
                            goto IL_01b7;
                        case 46u:
                            num5 = (trace.HasMultipleSources(block.Instructions[num3 + 1].Offset) ? 1 : 0);
                            goto IL_01f8;
                        case 5u:
                        case 12u:
                        case 45u:
                            hashSet.Remove(instruction);
                            num = 1457370637;
                            continue;
                        case 30u:
                        case 44u:
                            num = 1139108210;
                            continue;
                        case 43u:
                            num = (int)(((num2 + 1608343532) ^ 0xAF386872u) - 0);
                            continue;
                        case 42u:
                            if (num3 + 1 < block.Instructions.Count)
                            {
                                num = (int)(((num2 + 757804180) ^ 0x162B3BEF) + 0);
                                continue;
                            }
                            num5 = 0;
                            goto IL_01f8;
                        case 41u:
                            num4++;
                            num = (int)(((num2 + 649143086) ^ 0x6E7E80D9) - 0);
                            continue;
                        case 40u:
                            hashSet.Add(item);
                            num = (int)((num2 + 1195113832) ^ 0x3094A1EF ^ 0);
                            continue;
                        case 39u:
                            num = (int)(((num2 + 1960461734) ^ 0xB62D3824u) - 0);
                            continue;
                        case 11u:
                        case 38u:
                            if (num4 >= array2.Length)
                            {
                                num = 1548432968;
                                continue;
                            }
                            goto case 10u;
                        case 10u:
                            item = array2[num4];
                            num = 707670380;
                            continue;
                        case 37u:
                            num4 = 0;
                            num = (((int)num2 + -554548610) ^ 0x6264269C) >> 0;
                            continue;
                        case 36u:
                            array2 = array;
                            num = (int)(((num2 + 1892383293) ^ 0xC1168778u) << 0);
                            continue;
                        case 35u:
                            num = (((int)num2 + -40093842) ^ 0x53AECA35) + 0;
                            continue;
                        case 34u:
                            num = (((int)num2 + -290999538) ^ 0x797BAE6E) - 0;
                            continue;
                        case 33u:
                            if (flag2)
                            {
                                num = (((int)num2 + -1497151632) ^ 0x6FE5BF65) >> 0;
                                continue;
                            }
                            goto case 30u;
                        case 32u:
                            instruction = block.Instructions[num3];
                            num = (int)(((num2 + 1704553265) ^ 0xE9860C62u) << 0);
                            continue;
                        case 31u:
                            array = instruction.Operand as Instruction[];
                            num = 900749728;
                            continue;
                        case 29u:
                            hashSet.Add(instruction2);
                            num = (int)(((num2 + 67747431) ^ 0x4F234964) << 0);
                            continue;
                        case 27u:
                            flag = instruction2 != null;
                            num = (((int)num2 + -1756947900) ^ -828244392) - 0;
                            continue;
                        case 26u:
                            instruction2 = instruction.Operand as Instruction;
                            num = (int)((num2 + 583561026) ^ 0x60FE6D14 ^ 0);
                            continue;
                        case 24u:
                            if (flag6)
                            {
                                num = (((int)num2 + -100986504) ^ 0x2C107FBD) >> 0;
                                continue;
                            }
                            goto case 5u;
                        case 23u:
                            flag6 = trace.AfterStack[instruction.Offset] != 0;
                            num = ((int)num2 + -1904678002) ^ -7420451 ^ 0;
                            continue;
                        case 21u:
                            num3 = 0;
                            num = ((int)num2 + -1930605743) ^ -1143894643 ^ 0;
                            continue;
                        case 19u:
                        case 20u:
                            if ((uint)(flowControl2 - 7) > 1u)
                            {
                                num = 647071755;
                                continue;
                            }
                            goto case 22u;
                        case 16u:
                            flag4 = list.Count > 0;
                            num = (((int)num2 + -1030955589) ^ -1179683278) << 0;
                            continue;
                        case 15u:
                            num3++;
                            num = (int)(((num2 + 268319629) ^ 0x1C9525B4) + 0);
                            continue;
                        case 14u:
                            list.Clear();
                            num = (((int)num2 + -2071238116) ^ -1116572946) >> 0;
                            continue;
                        case 13u:
                            if (flag3)
                            {
                                num = (((int)num2 + -504175385) ^ 0x289EA942) - 0;
                                continue;
                            }
                            goto case 56u;
                        case 9u:
                            num = (int)(((num2 + 234277810) ^ 0x26400DAB) << 0);
                            continue;
                        case 8u:
                            flag2 = array != null;
                            num = (int)(((num2 + 329939053) ^ 0x2A139A10) << 0);
                            continue;
                        case 7u:
                            if (flag)
                            {
                                num = (((int)num2 + -1536809407) ^ -895963781) + 0;
                                continue;
                            }
                            goto case 31u;
                        case 6u:
                            num = (((int)num2 + -719845573) ^ 0x6BE168A6) >> 0;
                            continue;
                        case 3u:
                            flowControl = instruction.OpCode.FlowControl;
                            num = (int)(((num2 + 1452403640) ^ 0x7B593BEB) + 0);
                            continue;
                        case 2u:
                            list.Add(instruction);
                            num = (int)(((num2 + 1438922508) ^ 0xED976226u) - 0);
                            continue;
                        case 0u:
                            linkedList = new LinkedList<Instruction[]>();
                            num = (int)(((num2 + 1661375474) ^ 0x834E4179u) << 0);
                            continue;
                        case 25u:
                            break;
                        default:
                            {
                                return result;
                            }
                        IL_01f8:
                            num = 600520443;
                            continue;
                        IL_01b7:
                            num6 = 0;
                            goto IL_0182;
                        IL_0182:
                            flag3 = (byte)num6 != 0;
                            num = 1243643193;
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

