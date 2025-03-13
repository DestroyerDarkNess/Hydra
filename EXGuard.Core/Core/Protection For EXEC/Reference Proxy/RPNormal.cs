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
    public static class RPNormal
    {
        public static List<MethodDef> ProxyMethods
        {
            get;
            private set;
        }

        static RPNormal()
        {
            ProxyMethods = new List<MethodDef>();
        }

        public static void Execute(ModuleDef module)
        {
            RPHelper rPHelper = new RPHelper();
            rPHelper.Random = new RandomGenerator();
            rPHelper.NameService = new NameService();

            foreach (TypeDef type in module.Types.ToArray())
            {
                foreach (MethodDef method in type.Methods.ToArray())
                {
                    if (ProxyMethods.Contains(method))
                        continue;

                    if (canObfuscate(method))
                    {
                        foreach (Instruction instruction in method.Body.Instructions.ToArray())
                        {
                            if (instruction.OpCode == OpCodes.Stfld)
                            {
                                FieldDef targetField = instruction.Operand as FieldDef;

                                if (targetField == null)
                                    continue;
                                CilBody body = new CilBody();

                                body.Instructions.Add(OpCodes.Nop.ToInstruction());
                                body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                                body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
                                body.Instructions.Add(OpCodes.Stfld.ToInstruction(targetField));
                                body.Instructions.Add(OpCodes.Ret.ToInstruction());

                                var sig = MethodSig.CreateInstance(module.CorLibTypes.Void, targetField.FieldSig.GetFieldType());
                                sig.HasThis = true;

                                MethodDefUser methodDefUser = new MethodDefUser(rPHelper.NameService.NewName(rPHelper.Random.NextHexString()), sig)
                                {
                                    Body = body,
                                    IsHideBySig = true
                                };

                                ProxyMethods.Add(methodDefUser);
                                method.DeclaringType.Methods.Add(methodDefUser);

                                instruction.Operand = methodDefUser;
                                instruction.OpCode = OpCodes.Call;
                            }
                            else
                            if (instruction.OpCode == OpCodes.Call)
                            {
                                if (instruction.Operand is MemberRef)
                                {
                                    MemberRef methodReference = (MemberRef)instruction.Operand;

                                    if (!methodReference.FullName.Contains("Collections.Generic") && !methodReference.Name.Contains("ToString") && !methodReference.FullName.Contains("Thread::Start"))
                                    {
                                        MethodDef methodDef = rPHelper.GenerateMethod(type, methodReference, methodReference.HasThis);

                                        if (methodDef != null)
                                        {
                                            ProxyMethods.Add(methodDef);
                                            type.Methods.Add(methodDef);

                                            instruction.Operand = methodDef;
                                            methodDef.Body.Instructions.Add(new Instruction(OpCodes.Ret));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool canObfuscate(MethodDef methodDef)
        {
            if (!methodDef.HasBody)
                return false;
            if (!methodDef.Body.HasInstructions)
                return false;

            if (methodDef.DeclaringType.IsGlobalModuleType)
                return false;

            return true;
        }
    }
}
