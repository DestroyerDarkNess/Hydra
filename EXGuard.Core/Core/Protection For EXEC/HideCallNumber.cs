using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.Services;
using EXGuard.Core.RT.Mutation;

namespace EXGuard.Core.EXECProtections
{
    public class HideCallNumber
    {
        public ModuleDef Module { get; set; }
        public Dictionary<object, FieldDef> Numbers { get; set; }

        public HideCallNumber(ModuleDef module)
        {
            this.Module = module;
            this.Numbers = new Dictionary<object, FieldDef>();
        }

        public void Execute(TypeDef type, MethodDef method)
        {
            if (method.HasBody && method.Body.HasInstructions)
            {
                HideAllNumbers(type, method, this.Module);
            }
        }

        private void HideAllNumbers(TypeDef type, MethodDef method, ModuleDef module)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (this.Module.GlobalType.Fields.Count < 65000)
                {
                    if (method.Body.Instructions[i].IsLdcI4() ||
                        method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_0 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_1 ||
                        method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_2 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_2 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_3 ||
                        method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_4 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_5 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_6 ||
                        method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_7 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_8 ||method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_S ||
                        method.Body.Instructions[i].OpCode == OpCodes.Ldc_I4_M1 ||

                        method.Body.Instructions[i].OpCode == OpCodes.Ldc_I8 || method.Body.Instructions[i].OpCode == OpCodes.Ldc_R4 ||
                        method.Body.Instructions[i].OpCode == OpCodes.Ldc_R8)
                    {
                        var operand = method.Body.Instructions[i].Operand;
                        var opcode = method.Body.Instructions[i].OpCode;

                        try
                        {
                            if (operand == null)
                            {
                                if (opcode == OpCodes.Ldc_I4_0)
                                    operand = 0;
                                else if (opcode == OpCodes.Ldc_I4_1)
                                    operand = 1;
                                else if (opcode == OpCodes.Ldc_I4_2)
                                    operand = 2;
                                else if (opcode == OpCodes.Ldc_I4_3)
                                    operand = 3;
                                else if (opcode == OpCodes.Ldc_I4_4)
                                    operand = 4;
                                else if (opcode == OpCodes.Ldc_I4_5)
                                    operand = 5;
                                else if (opcode == OpCodes.Ldc_I4_6)
                                    operand = 6;
                                else if (opcode == OpCodes.Ldc_I4_7)
                                    operand = 7;
                                else if (opcode == OpCodes.Ldc_I4_8)
                                    operand = 8;
                            }

                            FieldDef field;
                            if (!Numbers.TryGetValue(operand, out field))
                            {
                                field = Add(module, type, operand, opcode, module.CorLibTypes.GetTypeRef(operand.GetType().Namespace, operand.GetType().Name).ToTypeSig());
                                Numbers.Add(operand, field);
                            }

                            method.Body.Instructions[i].OpCode = OpCodes.Ldsfld;
                            method.Body.Instructions[i].Operand = field;
                        }
                        catch { }
                    }
                }
            }
        }

        private FieldDef Add(ModuleDef module, TypeDef type, object value, OpCode opcode, TypeSig sig)
        {
            var rand = new RandomGenerator();
            var field = new FieldDefUser(new NameService().NewName(rand.NextString()), new FieldSig(sig),
                FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static)
            {
                DeclaringType = null
            };

            module.GlobalType.Fields.Add(field);

            var cctor = Module.GlobalType.FindOrCreateStaticConstructor();

            if (value == null)
                cctor.Body.Instructions.Insert(0, new Instruction(opcode));
            else
                cctor.Body.Instructions.Insert(0, new Instruction(opcode, value));

            cctor.Body.Instructions.Insert(1, new Instruction(OpCodes.Stsfld, field));

            return field;
        }
    }
}
