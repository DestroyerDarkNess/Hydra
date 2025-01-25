using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Mutations
{
    public class Mutator : Models.Protection
    {
        public Mutator() : base("Protection.Mutations.Mutator", "Renamer Phase", "Description for Renamer Phase") { }

        public bool UnsafeMutation { get; set; } = true;

        public override async Task<bool> Execute(ModuleDefMD md)
        {
            try
            {
                foreach (TypeDef typeDef in md.Types)
                {
                    if (!Analyzer.CanRename(typeDef)) continue;
                    foreach (MethodDef method in typeDef.Methods)
                    {
                        if (!Analyzer.CanRename(method)) continue;
                        CilBody body = method.Body;
                        if (body == null) continue;
                        body.SimplifyBranches();
                        Random random = new Random();
                        int i = 0;
                        while (i < body.Instructions.Count)
                        {
                            bool flag = body.Instructions[i].IsLdcI4();
                            if (flag)
                            {
                                int ldcI4Value = body.Instructions[i].GetLdcI4Value();
                                int num = random.Next(5, 40);
                                body.Instructions[i].OpCode = OpCodes.Ldc_I4;
                                body.Instructions[i].Operand = num * ldcI4Value;
                                body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Ldc_I4, num));
                                body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Div));
                                i += 3;
                            }
                            else
                            {
                                i++;
                            }
                        }
                        Random random2 = new Random();
                        int num2 = 0;
                        ITypeDefOrRef type = null;
                        for (int j = 0; j < method.Body.Instructions.Count; j++)
                        {
                            Instruction instruction = method.Body.Instructions[j];
                            bool flag2 = instruction.IsLdcI4();
                            if (flag2)
                            {
                                switch (random2.Next(1, 16))
                                {
                                    case 1:
                                        type = method.Module.Import(typeof(int));
                                        num2 = 4;
                                        break;
                                    case 2:
                                        type = method.Module.Import(typeof(sbyte));
                                        num2 = 1;
                                        break;
                                    case 3:
                                        type = method.Module.Import(typeof(byte));
                                        num2 = 1;
                                        break;
                                    case 4:
                                        type = method.Module.Import(typeof(bool));
                                        num2 = 1;
                                        break;
                                    case 5:
                                        type = method.Module.Import(typeof(decimal));
                                        num2 = 16;
                                        break;
                                    case 6:
                                        type = method.Module.Import(typeof(short));
                                        num2 = 2;
                                        break;
                                    case 7:
                                        type = method.Module.Import(typeof(long));
                                        num2 = 8;
                                        break;
                                    case 8:
                                        type = method.Module.Import(typeof(uint));
                                        num2 = 4;
                                        break;
                                    case 9:
                                        type = method.Module.Import(typeof(float));
                                        num2 = 4;
                                        break;
                                    case 10:
                                        type = method.Module.Import(typeof(char));
                                        num2 = 2;
                                        break;
                                    case 11:
                                        type = method.Module.Import(typeof(ushort));
                                        num2 = 2;
                                        break;
                                    case 12:
                                        type = method.Module.Import(typeof(double));
                                        num2 = 8;
                                        break;
                                    case 13:
                                        type = method.Module.Import(typeof(DateTime));
                                        num2 = 8;
                                        break;
                                    case 14:
                                        type = method.Module.Import(typeof(ConsoleKeyInfo));
                                        num2 = 12;
                                        break;
                                    case 15:
                                        type = method.Module.Import(typeof(Guid));
                                        num2 = 16;
                                        break;
                                }
                                int num3 = random2.Next(1, 1000);
                                bool flag3 = Convert.ToBoolean(random2.Next(0, 2));
                                switch ((num2 != 0) ? ((Convert.ToInt32(instruction.Operand) % num2 == 0) ? random2.Next(1, 5) : random2.Next(1, 4)) : random2.Next(1, 4))
                                {
                                    case 1:
                                        method.Body.Instructions.Insert(j + 1, Instruction.Create(OpCodes.Sizeof, type));
                                        method.Body.Instructions.Insert(j + 2, Instruction.Create(OpCodes.Add));
                                        instruction.Operand = Convert.ToInt32(instruction.Operand) - num2 + (flag3 ? (-num3) : num3);
                                        method.Body.Instructions.Insert(j + 3, Instruction.CreateLdcI4(num3));
                                        method.Body.Instructions.Insert(j + 4, Instruction.Create(flag3 ? OpCodes.Add : OpCodes.Sub));
                                        j += 4;
                                        break;
                                    case 2:
                                        method.Body.Instructions.Insert(j + 1, Instruction.Create(OpCodes.Sizeof, type));
                                        method.Body.Instructions.Insert(j + 2, Instruction.Create(OpCodes.Sub));
                                        instruction.Operand = Convert.ToInt32(instruction.Operand) + num2 + (flag3 ? (-num3) : num3);
                                        method.Body.Instructions.Insert(j + 3, Instruction.CreateLdcI4(num3));
                                        method.Body.Instructions.Insert(j + 4, Instruction.Create(flag3 ? OpCodes.Add : OpCodes.Sub));
                                        j += 4;
                                        break;
                                    case 3:
                                        method.Body.Instructions.Insert(j + 1, Instruction.Create(OpCodes.Sizeof, type));
                                        method.Body.Instructions.Insert(j + 2, Instruction.Create(OpCodes.Add));
                                        instruction.Operand = Convert.ToInt32(instruction.Operand) - num2 + (flag3 ? (-num3) : num3);
                                        method.Body.Instructions.Insert(j + 3, Instruction.CreateLdcI4(num3));
                                        method.Body.Instructions.Insert(j + 4, Instruction.Create(flag3 ? OpCodes.Add : OpCodes.Sub));
                                        j += 4;
                                        break;
                                    case 4:
                                        method.Body.Instructions.Insert(j + 1, Instruction.Create(OpCodes.Sizeof, type));
                                        method.Body.Instructions.Insert(j + 2, Instruction.Create(OpCodes.Mul));
                                        instruction.Operand = Convert.ToInt32(instruction.Operand) / num2;
                                        j += 2;
                                        break;
                                    default:
                                        method.Body.Instructions.Insert(j + 3, Instruction.CreateLdcI4(num3));
                                        method.Body.Instructions.Insert(j + 4, Instruction.Create(flag3 ? OpCodes.Add : OpCodes.Sub));
                                        j += 4;
                                        break;
                                }
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

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
