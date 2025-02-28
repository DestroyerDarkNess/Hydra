using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.String
{

    public class ReplaceObfuscation : Models.Protection
    {
        public enum Mode
        {
            Simple,
            Homoglyph
        }

        private readonly Mode _mode = Mode.Homoglyph;

        private readonly Random _random;

        public ReplaceObfuscation()
            : base("Protection.String.ReplaceObfuscation", "String Encryption", "XOR + Clave en Recursos") { _random = new Random(Guid.NewGuid().GetHashCode()); }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                Importer importer = new Importer(module);
                foreach (TypeDef item in from t in module.GetTypes()
                                         where t.Methods.Count != 0
                                         select t)
                {
                    if (item.IsGlobalModuleType || item.Namespace == "Costura")
                    {
                        continue;
                    }
                    foreach (MethodDef method in item.Methods)
                    {
                        if (method.Body == null || !method.HasBody || !method.Body.HasInstructions)
                        {
                            continue;
                        }
                        method.Body.SimplifyMacros(method.Parameters);
                        method.Body.SimplifyBranches();
                        IList<Instruction> instructions = method.Body.Instructions;
                        for (int i = 0; i < instructions.Count; i++)
                        {
                            if (instructions[i].OpCode != OpCodes.Ldstr || (string)instructions[i].Operand == string.Empty)
                            {
                                continue;
                            }
                            instructions[i].Operand = ObfuscateString((string)instructions[i].Operand);
                            List<Instruction> list = new List<Instruction>();
                            IMethod operand = importer.Import(typeof(string).GetMethod("Replace", new Type[2]
                            {
                        typeof(string),
                        typeof(string)
                            }) ?? throw new InvalidDataException());
                            if (_mode == Mode.Homoglyph)
                            {
                                string[] source = new string[5] { "а", "е", "і", "о", "с" };
                                string[] array = source.OrderBy((string c) => _random.Next()).ToArray();
                                for (int j = 0; j < array.Length; j++)
                                {
                                    list.Add(new Instruction(OpCodes.Ldstr, array[j]));
                                    list.Add(new Instruction(OpCodes.Ldstr, ""));
                                    if (j == 0)
                                    {
                                        list.Add(new Instruction(OpCodes.Call, operand));
                                    }
                                    else
                                    {
                                        list.Add(new Instruction(OpCodes.Callvirt, operand));
                                    }
                                }
                            }
                            else
                            {
                                list.Add(new Instruction(OpCodes.Ldstr, "\u2029"));
                                list.Add(new Instruction(OpCodes.Ldstr, ""));
                                list.Add(new Instruction(OpCodes.Call, operand));
                            }
                            foreach (Instruction item2 in list)
                            {
                                instructions.Insert(i + 1, item2);
                                i++;
                            }
                        }
                        method.Body.OptimizeMacros();
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

        public void ExecuteFor(MethodDef m, ModuleDefMD _module)
        {
            Importer importer = new Importer(_module);
            foreach (TypeDef item in from t in _module.GetTypes()
                                     where t.Methods.Count != 0
                                     select t)
            {
                if (item.IsGlobalModuleType || item.Namespace == "Costura")
                {
                    continue;
                }
                foreach (MethodDef method in item.Methods)
                {
                    if (method != m || method.Body == null || !method.HasBody || !method.Body.HasInstructions)
                    {
                        continue;
                    }
                    method.Body.SimplifyMacros(method.Parameters);
                    method.Body.SimplifyBranches();
                    IList<Instruction> instructions = method.Body.Instructions;
                    for (int i = 0; i < instructions.Count; i++)
                    {
                        if (instructions[i].OpCode != OpCodes.Ldstr || (string)instructions[i].Operand == string.Empty)
                        {
                            continue;
                        }
                        instructions[i].Operand = ObfuscateString((string)instructions[i].Operand);
                        List<Instruction> list = new List<Instruction>();
                        IMethod operand = importer.Import(typeof(string).GetMethod("Replace", new Type[2]
                        {
                        typeof(string),
                        typeof(string)
                        }) ?? throw new InvalidDataException());
                        if (_mode == Mode.Homoglyph)
                        {
                            string[] source = new string[5] { "а", "е", "і", "о", "с" };
                            string[] array = source.OrderBy((string c) => _random.Next()).ToArray();
                            for (int j = 0; j < array.Length; j++)
                            {
                                list.Add(new Instruction(OpCodes.Ldstr, array[j]));
                                list.Add(new Instruction(OpCodes.Ldstr, ""));
                                if (j == 0)
                                {
                                    list.Add(new Instruction(OpCodes.Call, operand));
                                }
                                else
                                {
                                    list.Add(new Instruction(OpCodes.Callvirt, operand));
                                }
                            }
                        }
                        else
                        {
                            list.Add(new Instruction(OpCodes.Ldstr, "\u2029"));
                            list.Add(new Instruction(OpCodes.Ldstr, ""));
                            list.Add(new Instruction(OpCodes.Call, operand));
                        }
                        foreach (Instruction item2 in list)
                        {
                            instructions.Insert(i + 1, item2);
                            i++;
                        }
                    }
                    method.Body.OptimizeMacros();
                }
            }
        }

        private string ObfuscateString(string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in input)
            {
                if (_random.Next(0, 1) == 0)
                {
                    stringBuilder.Append((_mode == Mode.Homoglyph) ? new string(GetHomoglyph(c), 1) : new string('\u2029', 1));
                    stringBuilder.Append(c);
                }
                else
                {
                    stringBuilder.Append(c);
                    stringBuilder.Append((_mode == Mode.Homoglyph) ? new string(GetHomoglyph(c), 1) : new string('\u2029', 1));
                }
            }
            return stringBuilder.ToString();
        }

        private char GetHomoglyph(char input)
        {
            char[] array = new char[5] { 'а', 'е', 'і', 'о', 'с' };
            switch (input)
            {
                case 'e':
                    return array[1];
                case 'a':
                    return array[0];
                case 'o':
                    return array[3];
                case 'i':
                    return array[2];
                default:
                    return array[_random.Next(array.Length)];
            }
        }
    }
}
