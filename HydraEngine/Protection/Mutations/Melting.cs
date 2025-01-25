using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydraEngine.Core;

namespace HydraEngine.Protection.Mutations
{
    public class Melting : Models.Protection
    {
        public Melting() : base("", "", "") { }

        public bool UnsafeMutation { get; set; } = true;

        public override async Task<bool> Execute(ModuleDefMD md)
        {
            try
            {

                foreach (TypeDef type in md.Types.ToArray())
                {
                    if (!Analyzer. CanRename(type)) continue;
                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (!Analyzer.CanRename(method)) continue;
                        ReplaceStringLiterals(method);
                        ReplaceIntLiterals(method);
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

        private static void ReplaceStringLiterals(MethodDef methodDef)
        {
            if (CanObfuscate(methodDef))
            {
                foreach (Instruction instruction in methodDef.Body.Instructions)
                {
                    if (instruction.OpCode != OpCodes.Ldstr) continue;
                    MethodDef replacementMethod = new MethodDefUser(Randomizer.GenerateRandomString(), MethodSig.CreateStatic(methodDef.DeclaringType.Module.CorLibTypes.String), MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig) { Body = new CilBody() };
                    replacementMethod.Body.Instructions.Add(new Instruction(OpCodes.Ldstr, instruction.Operand.ToString()));
                    replacementMethod.Body.Instructions.Add(new Instruction(OpCodes.Ret));
                    methodDef.DeclaringType.Methods.Add(replacementMethod);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = replacementMethod;
                }
            }
        }

        private static void ReplaceIntLiterals(MethodDef methodDef)
        {
            if (CanObfuscate(methodDef))
            {
                foreach (Instruction instruction in methodDef.Body.Instructions)
                {
                    if (instruction.OpCode != OpCodes.Ldc_I4) continue;
                    MethodDef replacementMethod = new MethodDefUser(Randomizer.GenerateRandomString(), MethodSig.CreateStatic(methodDef.DeclaringType.Module.CorLibTypes.Int32), MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig) { Body = new CilBody() };
                    replacementMethod.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, instruction.GetLdcI4Value()));
                    replacementMethod.Body.Instructions.Add(new Instruction(OpCodes.Ret));
                    methodDef.DeclaringType.Methods.Add(replacementMethod);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = replacementMethod;
                }
            }
        }

        public static bool CanObfuscate(MethodDef methodDef)
        {
            if (!methodDef.HasBody)
                return false;
            if (!methodDef.Body.HasInstructions)
                return false;
            if (methodDef.DeclaringType.IsGlobalModuleType)
                return false;
            return true;
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
