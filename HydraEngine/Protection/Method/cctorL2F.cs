using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Method
{
    public class cctorL2F : Models.Protection
    {
        public cctorL2F() : base("Protection.Method.cctorL2F", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                MethodDef methodDef = module.GlobalType.FindOrCreateStaticConstructor();
                IList<Instruction> instructions = methodDef.Body.Instructions;
                Dictionary<int, FieldDef> dictionary = new Dictionary<int, FieldDef>();

                foreach (MethodDef method in module.GlobalType.Methods.ToArray())
                {
                    if (method.IsStaticConstructor) continue;
                    method.Name = Randomizer.GenerateRandomString();

                    if (!method.HasBody || !method.Body.HasInstructions || method.MethodHasL2FAttribute())
                    {
                        continue;
                    }

                    method.Body.Instructions.Insert(0, new Instruction(OpCodes.Nop));
                    method.Body.Instructions.Insert(1, new Instruction(OpCodes.Br_S, method.Body.Instructions[1]));
                    method.Body.Instructions.Insert(2, new Instruction(OpCodes.Unaligned, (byte)0));

                    IList<Instruction> instructions2 = method.Body.Instructions;
                    List<Instruction> list = instructions2.Where((Instruction x) => x.IsLdcI4()).ToList();
                    foreach (Instruction item in list)
                    {
                        int ldcI4Value = item.GetLdcI4Value();
                        if (!dictionary.TryGetValue(ldcI4Value, out var value))
                        {
                            value = CreateField(new FieldSig(module.CorLibTypes.Int32));
                            module.GlobalType.Fields.Add(value);
                            dictionary[ldcI4Value] = value;
                            if (instructions.Count == 0)
                            {
                                instructions.Insert(0, OpCodes.Stsfld.ToInstruction(value));
                                instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
                            }
                            else
                            {
                                Instruction instruction = instructions.FirstOrDefault((Instruction i) => i.OpCode != OpCodes.Nop);
                                if (instruction != null)
                                {
                                    int index = instructions.IndexOf(instruction);
                                    instructions.Insert(index, OpCodes.Stsfld.ToInstruction(value));
                                    instructions.Insert(index, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
                                }
                                else
                                {
                                    instructions.Insert(0, OpCodes.Stsfld.ToInstruction(value));
                                    instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
                                }
                            }
                        }
                        item.OpCode = OpCodes.Ldsfld;
                        item.Operand = value;
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


        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
        public FieldDefUser CreateField(FieldSig sig)
        {
            return new FieldDefUser(Randomizer.GenerateRandomString(), sig, dnlib.DotNet.FieldAttributes.Public | dnlib.DotNet.FieldAttributes.Static);
        }
    }
}
