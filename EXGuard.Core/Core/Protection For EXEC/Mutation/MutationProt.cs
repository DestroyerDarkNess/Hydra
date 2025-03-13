using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.EXECProtections._Mutation.Blocks;
using EXGuard.Core.EXECProtections._Mutation.Emulator;

namespace EXGuard.Core.EXECProtections._Mutation
{
    public static class MutationProt
    {
        static Random rnd = new Random();

        public static void Execute(ModuleDef module, MethodDef method)
        {
            if (!method.HasBody)
                return;

            if (!method.Body.HasInstructions)
                return;

            method.Body.SimplifyMacros(method.Parameters);

            var blocks = method.GetBlocks();

            var emulator = new InstructionEmulator();
            var firstBlock = new Block();

            var locals = new List<Local>();
            var localToBlocks = new Dictionary<Local, List<Block>>();

            var maxLocals = 2;

            for (int i = 0; i < maxLocals; i++)
            {
                var newLocal = new Local(module.CorLibTypes.Int32);

                locals.Add(newLocal);
                localToBlocks.Add(newLocal, new List<Block>());

                firstBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(rnd.Next()));
                firstBlock.Instructions.Add(OpCodes.Stloc.ToInstruction(newLocal));

            }

            emulator.Emulate(firstBlock);

            var allBlocks = new List<Block>() { firstBlock };

            foreach (var block in blocks)
            {
                if (block.IsSafe && !block.IsBranched && !block.IsException)
                {

                    foreach (var local in locals)
                    {
                        var updateValue = new Block();

                        switch (rnd.Next(0, 7))
                        {
                            case 0:
                                SimpleUpdateGen(updateValue, local, rnd.Next(0, 2));
                                break;
                            case 1:
                                var nop = new Instruction(OpCodes.Nop);
                                var currentValue = (int)emulator.GetLocalValue(local);
                                var branchBlock = new Block();

                                updateValue.Instructions.Add(nop);

                                var isFake = rnd.Next(0, 2) == 0;
                                var isReverse = rnd.Next(0, 2) == 0;

                                if (isReverse)
                                {
                                    branchBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(rnd.Next(100, 500)));
                                    branchBlock.Instructions.Add(OpCodes.Ldloc.ToInstruction(local));
                                }
                                else
                                {
                                    branchBlock.Instructions.Add(OpCodes.Ldloc.ToInstruction(local));
                                    branchBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(rnd.Next(100, 500)));
                                }

                                branchBlock.Instructions.Add(OpCodes.Ceq.ToInstruction());
                                branchBlock.Instructions.Add(isFake ? OpCodes.Brfalse.ToInstruction(nop) :
                                    OpCodes.Brtrue.ToInstruction(nop));

                                var insideBlock = new Block();
                                SimpleUpdateGen(insideBlock, local, rnd.Next(0, 2), rnd.Next(2, 5));

                                if (!isFake)
                                {
                                    emulator.Emulate(insideBlock);
                                }

                                branchBlock.Copy(insideBlock.Instructions);

                                if (rnd.Next(0, 2) == 0)
                                {
                                    SimpleUpdateGen(updateValue, local, rnd.Next(0, 2));
                                }

                                allBlocks.Add(branchBlock);
                                break;
                            case 2:
                                var currentLocValue = (int)emulator.GetLocalValue(local);
                                var max = rnd.Next(currentLocValue + 1000, currentLocValue + 2000);

                                var backUpNop = new Instruction(OpCodes.Nop);
                                var backNop = new Instruction(OpCodes.Nop);

                                var loopBlock = new Block();

                                var outsideLoopBlock = new Block();


                                updateValue.Instructions.Add(backUpNop);

                                loopBlock.Instructions.Add(backNop);

                                loopBlock.Instructions.Add(OpCodes.Ldloc.ToInstruction(local));
                                loopBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(max));

                                loopBlock.Instructions.Add(OpCodes.Cgt.ToInstruction());
                                loopBlock.Instructions.Add(OpCodes.Brtrue.ToInstruction(backUpNop));

                                var insideLoopBlock = new Block();
                                SimpleUpdateGen(insideLoopBlock, local, rnd.Next(0, 2), rnd.Next(2, 3));

                                while (currentLocValue < max)
                                {
                                    emulator.Emulate(insideLoopBlock);

                                    currentLocValue = (int)emulator.GetLocalValue(local);
                                }


                                loopBlock.Copy(insideLoopBlock.Instructions);
                                loopBlock.Instructions.Add(OpCodes.Br.ToInstruction(backNop));

                                allBlocks.Add(loopBlock);
                                break;
                            case 3:
                                SimpleUpdateGen(updateValue, local, rnd.Next(0, 2));
                                break;
                            case 4:
                                SimpleUpdateGen(updateValue, local, rnd.Next(0, 2));
                                break;
                            case 5:
                                var maxCases = rnd.Next(3, 7);
                                var array = new int[maxCases];
                                var targets = new Instruction[maxCases];

                                for (int i = 0; i < maxCases; i++)
                                {
                                    var val = rnd.Next(100, 500);

                                    array[i] = val;
                                    targets[i] = OpCodes.Ldc_I4.ToInstruction(val);

                                }

                                var switchBlock = new Block();
                                var targetIndex = rnd.Next(0, array.Length);
                                var targetValue = array[targetIndex];
                                var stlocInstr = OpCodes.Stloc.ToInstruction(local);
                                var switchInstr = OpCodes.Switch.ToInstruction(targets);
                                var newTargets = new Instruction[maxCases];

                                //var firstPushCalc = Calculate(localValue, targetIndex, out var reversePush);

                                switchBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(targetIndex));
                                //switchBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(targetIndex));
                                switchBlock.Instructions.Add(switchInstr);

                                switchBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(rnd.Next(100, 500)));
                                switchBlock.Instructions.Add(OpCodes.Br.ToInstruction(stlocInstr));

                                var k = 0;
                                foreach (var instr in targets)
                                {
                                    var switchValue = instr.GetLdcI4Value();
                                    var ldlocInstr = OpCodes.Ldc_I4.ToInstruction(switchValue);

                                    newTargets[k] = ldlocInstr;

                                    switchBlock.Instructions.Add(ldlocInstr);
                                    switchBlock.Instructions.Add(OpCodes.Br.ToInstruction(stlocInstr));
                                    k++;
                                }

                                switchInstr.Operand = newTargets;

                                switchBlock.Instructions.Add(stlocInstr);

                                emulator.SetLocalValue(local, targetValue);

                                allBlocks.Add(switchBlock);
                                break;
                            case 6:
                                var arrayType = module.ImportAsTypeSig(typeof(int[]));
                                var arrayLoc = new Local(arrayType);
                                var arrSize = rnd.Next(1, 5);
                                var arrValues = new int[arrSize];
                                var arrayBlock = new Block();
                                var arrIndex = rnd.Next(0, arrValues.Length);

                                arrayBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(arrSize));
                                arrayBlock.Instructions.Add(OpCodes.Newarr.ToInstruction(module.CorLibTypes.Int32));
                                arrayBlock.Instructions.Add(OpCodes.Stloc.ToInstruction(arrayLoc));

                                for (int z = 0; z < arrSize; z++)
                                {
                                    var rndLocalValue = rnd.Next(500, 1000);

                                    arrayBlock.Instructions.Add(OpCodes.Ldloc.ToInstruction(arrayLoc));
                                    arrayBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(z)); //INDEX
                                    arrayBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(rndLocalValue)); //VALUE
                                    arrayBlock.Instructions.Add(OpCodes.Stelem_I4.ToInstruction());

                                    arrValues[z] = rndLocalValue;
                                }

                                arrayBlock.Instructions.Add(OpCodes.Ldloc.ToInstruction(arrayLoc));
                                arrayBlock.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(arrIndex)); //INDEX
                                arrayBlock.Instructions.Add(OpCodes.Ldelem_I4.ToInstruction());
                                arrayBlock.Instructions.Add(OpCodes.Stloc.ToInstruction(local));

                                emulator.SetLocalValue(local, arrValues[arrIndex]);

                                allBlocks.Add(arrayBlock);
                                method.Body.Variables.Add(arrayLoc);
                                break;
                        }

                        emulator.Emulate(updateValue);

                        allBlocks.Add(updateValue);
                        localToBlocks[local].Add(updateValue);
                    }
                }

                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    var instr = block.Instructions[i];

                    if (instr.IsLdcI4())
                    {
                        var value = instr.GetLdcI4Value();
                        var rndLocal = locals[rnd.Next(0, locals.Count)];
                        var result = Calculate(value, (int)emulator.GetLocalValue(rndLocal), out var reverse);

                        instr.OpCode = OpCodes.Ldc_I4;
                        instr.Operand = result;

                        block.Instructions.Insert(i + 1, new Instruction(OpCodes.Ldloc, rndLocal));
                        block.Instructions.Insert(i + 2, new Instruction(reverse));

                    }
                }
                allBlocks.Add(block);
            }

            method.Body.Instructions.Clear();

            foreach (var block in allBlocks)
                foreach (var instr in block.Instructions)
                    method.Body.Instructions.Add(instr);

            foreach (var local in locals)
                method.Body.Variables.Add(local);
        }

        static void SimpleUpdateGen(Block block, Local local, int caseValue, int quantity = 1) {
            for (int i = 0; i < quantity; i++) {
                switch (caseValue)
                {
                    case 0:
                        block.Instructions.Add(OpCodes.Ldloc.ToInstruction(local));
                        block.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(rnd.Next(100, 350)));
                        block.Instructions.Add(OpCodes.Add.ToInstruction());
                        block.Instructions.Add(OpCodes.Stloc.ToInstruction(local));
                        break;
                    case 1:
                        block.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(rnd.Next(100, 350)));
                        block.Instructions.Add(OpCodes.Ldloc.ToInstruction(local));
                        block.Instructions.Add(OpCodes.Add.ToInstruction());
                        block.Instructions.Add(OpCodes.Stloc.ToInstruction(local));
                        break;
                }
            }
        }

        static int Calculate(int a, int b, out OpCode reverse) {
            reverse = OpCodes.Nop;

            switch (rnd.Next(0, 3)) {
                case 0:
                    reverse = OpCodes.Add;
                    return a - b;
                case 1:
                    reverse = OpCodes.Sub;
                    return a + b;
                case 2:
                    reverse = OpCodes.Xor;
                    return a ^ b;
            }
            return -1;
        }
    }
}
