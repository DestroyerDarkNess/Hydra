using dnlib.DotNet;
using dnlib.DotNet.Emit;
using VM.Core.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HydraEngine.Protection.VM;

namespace VM.Core.Protections.Impl.Virtualization.RuntimeProtections
{
    internal static class ProxyInteger
    {
        private static Hashtable HT = null;
        private static List<string> BlacklistTypes = new List<string>() { "<>c", "Ceq", "HxCgt", "Factory" }; //Issues when applied
        private static List<string> BlacklistMethods = new List<string>() { "RotateKey" }; //Issues when applied
        internal static void Execute(ModuleDef Module)
        {
            HT = new Hashtable();
            MethodDef cctor = Module.GlobalType.FindOrCreateStaticConstructor();
            foreach (var Type in Module.GetTypes().Where(T => T.HasMethods && !T.IsGlobalModuleType && !BlacklistTypes.Contains(T.Name) && !T.Name.Contains("AssemblyLoader")).ToArray())
                foreach (var Method in Type.Methods.Where(M => M.HasBody && M.Body.HasInstructions && !BlacklistMethods.Contains(M.Name)).ToArray())
                {
                    if (Module == Virtualizer.Instance.Module && !Virtualizer.Instance.VirtualizedMethods.Contains(Method.FullName))
                        continue;
                    try
                    {
                        for (var i = 0; i < Method.Body.Instructions.Count; i++)
                        {
                            Instruction Instruction = Method.Body.Instructions[i];
                            if (Instruction.IsLdcI4())
                            {
                                FieldDef F = null;
                                MethodDef M = null;
                                if (!HT.ContainsKey(Instruction.Operand))
                                {
                                    FieldDefUser Field = new FieldDefUser(Generator.RandomName(), new FieldSig(Module.CorLibTypes.Int32), FieldAttributes.Assembly | FieldAttributes.Static);
                                    Field.IsStatic = true;
                                    Module.GlobalType.Fields.Add(Field);
                                    OpCode OpCode = null;
                                    switch (Instruction.OpCode.Code)
                                    {
                                        case Code.Ldloc:
                                            OpCode = OpCodes.Ldsfld;
                                            break;

                                        case Code.Ldloca:
                                            OpCode = OpCodes.Ldsflda;
                                            break;

                                        case Code.Stloc:
                                            OpCode = OpCodes.Stsfld;
                                            break;
                                    }
                                    HT[Instruction.Operand] = Field;
                                    F = (FieldDef)HT[Instruction.Operand];
                                    cctor.Body.Variables.Add(new Local(Module.CorLibTypes.Int32));
                                    cctor.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4, Instruction.GetLdcI4Value()));
                                    cctor.Body.Instructions.Insert(1, new Instruction(OpCodes.Stsfld, F));
                                }
                                else
                                {
                                    F = (FieldDef)HT[Instruction.Operand];
                                }

                                if (!HT.ContainsKey(F))
                                {
                                    var MMethod = new MethodDefUser(Generator.RandomName(), MethodSig.CreateStatic(Module.CorLibTypes.Int32), MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot);
                                    Method.DeclaringType.Methods.Add(MMethod);
                                    MMethod.Body = new CilBody();
                                    MMethod.Body.Variables.Add(new Local(Module.CorLibTypes.Int32));
                                    MMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, F));
                                    MMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                                    MMethod.Body.SimplifyBranches();
                                    MMethod.Body.OptimizeBranches();
                                    HT[F] = MMethod;
                                    M = (MethodDef)HT[F];
                                }
                                else
                                {
                                    M = (MethodDef)HT[F];
                                }

                                Method.Body.Instructions[i] = new Instruction(OpCodes.Ldftn, M);
                                Method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Nop));
                                Method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Calli, M.MethodSig));
                            }
                            else if (Method.Body.Instructions[i].OpCode == OpCodes.Ldc_R4)
                            {
                                var MMethod = new MethodDefUser(Generator.RandomName(), MethodSig.CreateStatic(Module.CorLibTypes.Double), MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot);
                                Module.GlobalType.Methods.Add(MMethod);
                                MMethod.Body = new CilBody();
                                MMethod.Body.Variables.Add(new Local(Module.CorLibTypes.Double));
                                MMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, (float)Instruction.Operand));
                                MMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                                MMethod.Body.SimplifyBranches();
                                MMethod.Body.OptimizeBranches();

                                Method.Body.Instructions[i] = new Instruction(OpCodes.Ldftn, MMethod);
                                Method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Nop));
                                Method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Calli, MMethod.MethodSig));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //I'm not sure which one throw but whatever all of the visible ones in the assembly are handled.
                        //Console.WriteLine(ex.ToString());
                    }
                }
        }
    }
}