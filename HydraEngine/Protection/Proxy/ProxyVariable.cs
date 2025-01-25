using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Proxy
{
     public class ProxyVariable : Models.Protection
    {
        public ProxyVariable() : base("Protection.Proxy.ProxyVariable", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {

                var renamedvartype = new TypeDefUser("n", "Type",
                            Module.CorLibTypes.Object.TypeDefOrRef);
                renamedvartype.Attributes = TypeAttributes.Public | TypeAttributes.AutoLayout |
                                    TypeAttributes.Class | TypeAttributes.AnsiClass;
                Module.Types.Add(renamedvartype);
                foreach (TypeDef type in Module.GetTypes())
                {
                    if (type.IsGlobalModuleType)
                    {
                        continue;
                    }

                    if (!Analyzer.CanRename(type)) continue;

                    if (type.Name == "GeneratedInternalTypeHelper")
                    {
                        continue;
                    }

                    foreach (MethodDef method in type.Methods)
                    {
                       
                        if (!method.HasBody || method.Body == null) continue;

                        if (!Analyzer.CanRename(method)) continue;
                        if (!method.HasBody)
                        {
                            continue;
                        }

                        if (method.IsVirtual || method.IsSpecialName)
                        {
                            continue;
                        }

                        if (method.Name == ".ctor" || method.Name == ".cctor")
                        {
                            continue;
                        }


                        Dictionary<string, Local> strings = new Dictionary<string, Local>();
                        Dictionary<int, Local> ints = new Dictionary<int, Local>();
                        int addedstrings = 0;
                        int addedints = 0;
                        // just trolling
                        var fucked_typesig = Module.ImportAsTypeSig(typeof(int************************************************************************************************************************************************************************************));

                        fucked_typesig = renamedvartype.ToTypeSig();

                        for (int i = 0; i < method.Body.Instructions.Count(); i++)
                        {
                            var instr = method.Body.Instructions;
                            // strings
                            if (instr[i].OpCode == OpCodes.Ldstr)
                            {
                                if (!strings.ContainsKey(instr[i].Operand.ToString()))
                                {
                                    var local1 = new Local(Module.CorLibTypes.String);
                                    method.Body.Variables.Add(local1);
                                    instr.Insert(0, Instruction.Create(OpCodes.Ldstr, instr[i].Operand.ToString()));
                                    addedstrings++;
                                    instr.Insert(1, Instruction.Create(OpCodes.Stloc_S, local1));
                                    i += 2;
                                    strings.Add(instr[i].Operand.ToString(), local1);
                                }
                            }
                            // ints
                            if (instr[i].IsLdcI4())
                            {
                                if (!ints.ContainsKey(instr[i].GetLdcI4Value()))
                                {
                                    var local1 = new Local(Module.CorLibTypes.Int32);
                                    method.Body.Variables.Add(local1);
                                    instr.Insert(0, Instruction.Create(OpCodes.Ldc_I4, instr[i].GetLdcI4Value()));
                                    addedints++;
                                    instr.Insert(1, Instruction.Create(OpCodes.Stloc_S, local1));
                                    i += 2;

                                    ints.Add(instr[i].GetLdcI4Value(), local1);
                                }
                            }


                        }
                        method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Nop));
                        int s = 0;
                        int iss = 0;
                        for (int i = 0; i < method.Body.Instructions.Count(); i++)
                        {
                            var instr = method.Body.Instructions;
                            //strings
                            if (instr[i].OpCode == OpCodes.Ldstr)
                            {
                                if (s < addedstrings)
                                {
                                    s += 1;
                                }
                                else
                                {
                                    instr[i].OpCode = OpCodes.Ldloc_S;
                                    instr[i].Operand = strings[instr[i].Operand.ToString()];
                                }
                            }
                            //ints
                            if (instr[i].IsLdcI4())
                            {
                                if (iss < addedints)
                                {
                                    iss += 1;
                                }
                                else
                                {
                                    int localldc = instr[i].GetLdcI4Value();
                                    instr[i].OpCode = OpCodes.Ldloc_S;
                                    instr[i].Operand = ints[localldc];
                                }
                            }
                        }

                        foreach (var local in method.Body.Variables)
                        {
                            local.Type = fucked_typesig;
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
