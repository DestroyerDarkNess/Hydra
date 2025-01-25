using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Renamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.CtrlFlow
{
    public class ControlFlow : Models.Protection
    {
        public ControlFlow() : base("Protection.CtrlFlow.ControlFlow", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD md)
        {
            try
            {
                foreach (var type in md.Types)
                {
                    if (!Analyzer.CanRename(type)) continue;
                    if (type == md.GlobalType) continue;
                    foreach (MethodDef method in type.Methods)
                    {
                        if (!method.HasBody) continue;
                        if (!method.Body.HasInstructions) continue;
                        if (!Analyzer.CanRename(method)) continue;


                        for (int i = 0; i < method.Body.Instructions.Count; i++)
                        {
                            if (method.Body.Instructions[i].IsLdcI4())
                            {
                                int numorig = new Random(Guid.NewGuid().GetHashCode()).Next();
                                int div = new Random(Guid.NewGuid().GetHashCode()).Next();
                                int num = numorig ^ div;

                                Instruction nop = OpCodes.Nop.ToInstruction();

                                Local local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
                                method.Body.Variables.Add(local);

                                method.Body.Instructions.Insert(i + 1, OpCodes.Stloc.ToInstruction(local));
                                method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldc_I4, method.Body.Instructions[i].GetLdcI4Value() - sizeof(float)));
                                method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, num));
                                method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Ldc_I4, div));
                                method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Xor));
                                method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Ldc_I4, numorig));
                                method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Bne_Un, nop));
                                method.Body.Instructions.Insert(i + 8, Instruction.Create(OpCodes.Ldc_I4, 2));
                                method.Body.Instructions.Insert(i + 9, OpCodes.Stloc.ToInstruction(local));
                                method.Body.Instructions.Insert(i + 10, Instruction.Create(OpCodes.Sizeof, method.Module.Import(typeof(float))));
                                method.Body.Instructions.Insert(i + 11, Instruction.Create(OpCodes.Add));
                                method.Body.Instructions.Insert(i + 12, nop);
                                i += 12;
                            }
                        }

                    }
                }

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }



        //public override async Task<bool> Execute(ModuleDefMD md)
        //{
        //    try
        //    {
        //        foreach (var type in md.Types)
        //        {
        //            if (!Analyzer.CanRename(type)) continue;
        //            if (type == md.GlobalType) continue;
        //            foreach (var meth in type.Methods)
        //            {
        //                if (!Analyzer.CanRename(meth)) continue;
        //                if (meth.IsRuntimeSpecialName || meth.IsSpecialName || meth.Name == "Invoke") continue;
        //                if (meth.Name.StartsWith("get_") || meth.Name.StartsWith("set_")) continue;
        //                if (!meth.HasBody || meth.IsConstructor) continue;
        //                meth.Body.SimplifyBranches();
        //                meth.Body.KeepOldMaxStack = true;
        //                ControlFlowPhase(meth);
        //                ExecuteMethod(meth);
        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception Ex)
        //    {
        //        this.Errors = Ex;
        //        return false;
        //    }
        //}

        private void ControlFlowPhase(MethodDef method)
        {
            for (var i = 0; i < method.Body.Instructions.Count; ++i)
            {
                if (!method.Body.Instructions[i].IsLdcI4())
                    continue;

                var numorig = new Random(Guid.NewGuid().GetHashCode()).Next();
                var div = new Random(Guid.NewGuid().GetHashCode()).Next();
                var num = numorig ^ div;

                var nop = OpCodes.Nop.ToInstruction();

                var local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
                method.Body.Variables.Add(local);

                method.Body.Instructions.Insert(i + 1, OpCodes.Stloc.ToInstruction(local));
                method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldc_I4, method.Body.Instructions[i].GetLdcI4Value() - sizeof(float)));
                method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, num));
                method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Ldc_I4, div));
                method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Xor));
                method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Ldc_I4, numorig));
                method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Bne_Un, nop));
                method.Body.Instructions.Insert(i + 8, Instruction.Create(OpCodes.Ldc_I4, 2));
                method.Body.Instructions.Insert(i + 9, OpCodes.Stloc.ToInstruction(local));
                method.Body.Instructions.Insert(i + 10, Instruction.Create(OpCodes.Sizeof, method.Module.Import(typeof(float))));
                method.Body.Instructions.Insert(i + 11, Instruction.Create(OpCodes.Add));
                method.Body.Instructions.Insert(i + 12, nop);
                i += 12;
            }
        }

        private void ExecuteMethod(MethodDef meth)
        {
            meth.Body.SimplifyMacros(meth.Parameters);
            var blocks = BlockParser.ParseMethod(meth);
            blocks = Randomize(blocks);
            meth.Body.Instructions.Clear();
            var local = new Local(meth.Module.CorLibTypes.Int32);
            meth.Body.Variables.Add(local);
            var target = Instruction.Create(OpCodes.Nop);
            var instr = Instruction.Create(OpCodes.Br, target);
            foreach (var instruction in Calc(0))
                meth.Body.Instructions.Add(instruction);
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Br, instr));
            meth.Body.Instructions.Add(target);
            foreach (var block in blocks.Where(block => block != blocks.Single(x => x.Number == blocks.Count - 1)))
            {
                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                foreach (var instruction in Calc(block.Number))
                    meth.Body.Instructions.Add(instruction);
                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
                var instruction4 = Instruction.Create(OpCodes.Nop);
                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, instruction4));

                foreach (var instruction in block.Instructions)
                    meth.Body.Instructions.Add(instruction);

                foreach (var instruction in Calc(block.Number + 1))
                    meth.Body.Instructions.Add(instruction);

                meth.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
                meth.Body.Instructions.Add(instruction4);
            }
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
            foreach (var instruction in Calc(blocks.Count - 1))
                meth.Body.Instructions.Add(instruction);
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, instr));
            meth.Body.Instructions.Add(Instruction.Create(OpCodes.Br, blocks.Single(x => x.Number == blocks.Count - 1).Instructions[0]));
            meth.Body.Instructions.Add(instr);

            foreach (var lastBlock in blocks.Single(x => x.Number == blocks.Count - 1).Instructions)
                meth.Body.Instructions.Add(lastBlock);
        }

        private  readonly Random Rnd = new Random();

        private  List<Block> Randomize(List<Block> input)
        {
            var ret = new List<Block>();
            foreach (var group in input)
                ret.Insert(Rnd.Next(0, ret.Count), group);
            return ret;
        }

        private  List<Instruction> Calc(int value)
        {
            var instructions = new List<Instruction> { Instruction.Create(OpCodes.Ldc_I4, value) };
            return instructions;
        }

        public void AddJump(IList<Instruction> instrs, Instruction target)
        {
            instrs.Add(Instruction.Create(OpCodes.Br, target));
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
