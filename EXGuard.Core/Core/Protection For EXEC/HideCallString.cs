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
    public class HideCallString
    {
        public ModuleDef Module { get; set; }
        public Dictionary<string, FieldDef> Strings { get; set; }

        public HideCallString(ModuleDef module)
        {
            this.Module = module;
            this.Strings = new Dictionary<string, FieldDef>();
        }

        public void Execute(TypeDef type, MethodDef method)
        {
            if (method.HasBody && method.Body.HasInstructions)
            {
                HideAllStr(type, method, this.Module);
            }
        }

        private FieldDef Add(ModuleDef module, TypeDef type, string value)
        {
            var rand = new RandomGenerator();
            var field = new FieldDefUser(new NameService().NewName(rand.NextString()), new FieldSig(module.CorLibTypes.String),
                FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static)
            {
                DeclaringType = null
            };

            module.GlobalType.Fields.Add(field);

            var cctor = Module.GlobalType.FindOrCreateStaticConstructor();

            cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldstr, value));
            cctor.Body.Instructions.Insert(1, new Instruction(OpCodes.Stsfld, field));

            return field;
        }

        private void HideAllStr(TypeDef type, MethodDef method, ModuleDef module)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                var inst = method.Body.Instructions[i];
                if (inst.OpCode == OpCodes.Ldstr && this.Module.GlobalType.Fields.Count < 65000)
                {
                    try
                    {
                        string value = inst.Operand.ToString();

                        FieldDef field;
                        if (!Strings.TryGetValue(value, out field))
                        {
                            field = Add(module, type, value);
                            Strings.Add(value, field);
                        }

                        inst.OpCode = OpCodes.Ldsfld;
                        inst.Operand = field;
                    }
                    catch { }
                }
            }
        }
    }
}
