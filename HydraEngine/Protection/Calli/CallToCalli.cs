﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Renamer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Calli
{



    public class CallToCalli : Models.Protection
    {
        public CallToCalli() : base("Protection.Calli.CallToCalli", "Renamer Phase", "Description for Renamer Phase") { }

        private string[] a = { "My.", ".My", "Costura" };
        private string[] b = { "Dispose", "ISupportInitialize", "Object" };


        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private Random Random = new Random();
        public MethodDef CollatzCtor;

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                foreach (var type in module.Types.ToArray())
                {
                    if (!AnalyzerPhase.CanRename(type)) continue;
                    foreach (var meth in type.Methods.ToArray())
                    {
                        if (!AnalyzerPhase.CanRename(meth, type)) continue;
                        if (!meth.HasBody) continue;
                        if (!meth.Body.HasInstructions) continue;
                        if (meth.FullName.Contains("My.")) continue;
                        if (meth.FullName.Contains(".My")) continue;
                        if (meth.FullName.Contains("Costura")) continue;
                        if (meth.IsConstructor) continue;
                        if (meth.DeclaringType.IsGlobalModuleType) continue;

                        if (meth.IsConstructor) continue;
                        if (!meth.HasBody || !meth.Body.HasInstructions || meth.DeclaringType.IsGlobalModuleType) continue;

                        if (meth.HasGenericParameters) continue;
                        if (meth.IsPinvokeImpl) continue;
                        if (meth.IsUnmanagedExport) continue;


                        if (meth.HasClosureReferences()) continue;

                        if (meth.Body.Instructions.Any(instr => IsAccessingNonPublicMember(instr, type))) continue;

                        if (meth.Parameters.Count >= 4) continue;

                        for (var i = 0; i < meth.Body.Instructions.Count - 1; i++)
                        {
                            try
                            {
                                if (meth.Body.Instructions[i].ToString().Contains("ISupportInitialize") || meth.Body.Instructions[i].OpCode != OpCodes.Call &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Callvirt &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Ldloc_S) continue;

                                if (meth.Body.Instructions[i].ToString().Contains("Object") || meth.Body.Instructions[i].OpCode != OpCodes.Call &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Callvirt &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Ldloc_S) continue;

                                try
                                {
                                    var membertocalli = (MemberRef)meth.Body.Instructions[i].Operand;
                                    meth.Body.Instructions[i].OpCode = OpCodes.Calli;
                                    meth.Body.Instructions[i].Operand = membertocalli.MethodSig;
                                    meth.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Ldftn, membertocalli));
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }
                    foreach (var md in module.GlobalType.Methods)
                    {
                        if (md.Name != ".ctor") continue;
                        module.GlobalType.Remove(md);
                        break;
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

        private bool IsAccessingNonPublicMember(Instruction instr, TypeDef declaringType)
        {
            if (instr.OpCode == OpCodes.Ldfld || instr.OpCode == OpCodes.Ldflda || instr.OpCode == OpCodes.Stfld)
            {
                var field = instr.Operand as IField;
                var fieldDef = field?.ResolveFieldDef();
                if (fieldDef?.DeclaringType == declaringType && !fieldDef.IsPublic)
                    return true;
            }

            if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
            {
                var method = instr.Operand as IMethod;
                var methodDef = method?.ResolveMethodDef();
                if (methodDef?.DeclaringType == declaringType && !methodDef.IsPublic)
                    return true;
            }

            return false;
        }
    }
}
