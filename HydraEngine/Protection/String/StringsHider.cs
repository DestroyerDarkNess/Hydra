using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.String
{
    public class StringsHider : Models.Protection
    {
        public StringsHider() : base("Protection.String.StringsHider", "Renamer Phase", "Description for Renamer Phase") { }

        public string tag { get; set; } = "HailHydra";

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                //var methods = new HashSet<MethodDef>();
                //methods.Add(Module.EntryPoint);

                //foreach (var hcst in Module.Types.ToArray())
                //{
                //    if (hcst == Module.GlobalType) continue;

                //    foreach (var hcsm in hcst.Methods.ToArray())
                //    {
                //        new HideCallString(Module).Execute(hcst, hcsm);
                //    }
                //    //if (!methods.Contains(hcsm))

                //}

                //foreach (var mtt in Module.Types.ToArray())
                //{
                //    if (mtt == Module.GlobalType) continue;
                //    foreach (var mtm in mtt.Methods.ToArray())
                //    {
                //        if (!RPNormal.ProxyMethods.Contains(mtm))
                //            CEXControlFlow.Execute(mtm, 1);
                //    }
                //}

                //ResourceProt_Inject.Execute(Module);

                //RPNormal.Execute(Module);

                //AntiDebug_Inject.Execute(Module);
                //AntiDump_Inject.Execute(Module);

                //AntiILDasm_Inject.Execute(Module);

                //AntiDe4dot_Inject.Execute(Module);

                //AntiDnspy_Inject.Execute(Module);

                //AntiWebDebuggers_Inject.Execute(Module);


                foreach (TypeDef type in Module.Types.Where(t => t.HasMethods))
                {
                    if (Module.GlobalType != type)
                    {
                        if (!CanRename(type)) continue;
                    }

                    MethodDef cctor = type.FindOrCreateStaticConstructor();
                    if (cctor.HasBody && cctor.Body.HasInstructions && cctor.Body.Instructions.Last().OpCode == OpCodes.Ret) cctor.Body.Instructions.Remove(cctor.Body.Instructions.Last());

                    foreach (MethodDef method in type.Methods.Where(m => m.HasBody && m.Body.HasInstructions))
                    {
                        if (!CanRename(method)) continue;
                        int ii = 0;
                        for (int iii = 0; iii < method.Body.Instructions.Count; iii++)
                        {
                            Instruction Instruction = method.Body.Instructions[iii];
                            if (Instruction.OpCode == OpCodes.Ldstr)
                            {
                                string str = (string)Instruction.Operand;
                                if (!string.IsNullOrEmpty(str))
                                {
                                    byte[] bytes = Encoding.UTF8.GetBytes(str);

                                    string newName = $"[{method.MDToken.ToString()}-{tag}]_{ii.ToString()}";

                                    FieldDefUser field = new FieldDefUser(newName, new FieldSig(Module.CorLibTypes.String), dnlib.DotNet.FieldAttributes.Assembly | dnlib.DotNet.FieldAttributes.Static);

                                    type.Fields.Add(field);

                                    cctor.Body.Instructions.Insert(0, OpCodes.Stsfld.ToInstruction(field));
                                    cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Callvirt, Module.Import(typeof(Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }))));

                                    for (int i = 0; i < bytes.Length; i++)
                                    {
                                        cctor.Body.Instructions.Insert(0, OpCodes.Stelem_I1.ToInstruction());
                                        cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, (int)bytes[i]));
                                        cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, (int)i));
                                        cctor.Body.Instructions.Insert(0, OpCodes.Dup.ToInstruction());
                                    }

                                    cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Newarr, Module.CorLibTypes.Byte.ToTypeDefOrRef()));
                                    cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, (int)bytes.Length));
                                    cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Call, Module.Import(typeof(Encoding).GetMethod("get_UTF8", new Type[] { }))));

                                    Instruction.OpCode = OpCodes.Ldsfld;
                                    Instruction.Operand = field;

                                    ii++;
                                }
                            }
                        }

                        for (int iii = 0; iii < method.Body.Instructions.Count; iii++) //Fix branches pointing the field directly.
                        {
                            Instruction Instruction = method.Body.Instructions[iii];
                            if (Instruction.OpCode == OpCodes.Brfalse || Instruction.OpCode == OpCodes.Brtrue || Instruction.OpCode == OpCodes.Brfalse_S || Instruction.OpCode == OpCodes.Brtrue_S)
                                if (Instruction.Operand.ToString().ToLower().Contains("ldsfld"))
                                {
                                    object operand = Instruction.Operand;
                                    Instruction targetedInstruction = (operand as Instruction);

                                    //Instruction.Operand = method.Body.Instructions[method.Body.Instructions.IndexOf(targetedInstruction) - 1];
                                }
                        }

                        if (cctor.Body.Instructions.Count > 0) //Fix last instructions
                        {
                            while (cctor.Body.Instructions.Last().OpCode == OpCodes.Stloc)
                            {
                                Instruction instr = cctor.Body.Instructions.Last();
                                cctor.Body.Instructions.Remove(instr);
                            }

                            while (cctor.Body.Instructions.Last().OpCode == OpCodes.Ret)
                            {
                                Instruction instr = cctor.Body.Instructions.Last();
                                cctor.Body.Instructions.Remove(instr);
                            }
                        }

                        method.Body.Instructions.SimplifyBranches();
                        method.Body.Instructions.OptimizeBranches();
                    }

                    if (cctor.Body.Instructions.Count == 0 || cctor.Body.Instructions[cctor.Body.Instructions.Count - 1].OpCode != OpCodes.Ret)
                    {
                        cctor.Body.Instructions.Add(new Instruction(OpCodes.Ret));
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

        private bool CanRename(TypeDef type)
        {
            // Excluir tipos especiales de VB.NET y C#
            if (string.IsNullOrEmpty(type.Namespace)) return false;
            if (type.IsRuntimeSpecialName || type.IsGlobalModuleType || type.IsSpecialName || type.IsWindowsRuntime || type.IsInterface) return false;
            if (type.Namespace.StartsWith("Microsoft.VisualBasic")) return false;
            if (type.Namespace.StartsWith("My")) return false;
            //   if (type.Name == "My" || type.Name == "MySettings" || type.Name == "MyApplication") return false;
            if (type.Name == "GeneratedInternalTypeHelper" || type.Name == "Resources" || type.Name == "Settings") return false;

            return true;
        }

        private bool CanRename(MethodDef method)
        {
            // Excluir métodos especiales

            if (!method.HasBody) return false;

            if (method.Name == ".ctor" || method.Name == ".cctor") return false;

            if (method.IsConstructor || method.IsRuntimeSpecialName || method.IsRuntime || method.IsStaticConstructor || method.IsVirtual) return false;

            if (method.DeclaringType.Namespace.StartsWith("Microsoft.VisualBasic")) return false;

            return true;
        }

    }
}
