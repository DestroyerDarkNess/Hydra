using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Renamer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Method
{

    public class cctorHider : Models.Protection
    {
        public cctorHider() : base("Protection.Method.cctorHider", "Renamer Phase", "Description for Renamer Phase") { }


        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                Random random = new Random();
                for (int i = 0; i < random.Next(100, 200); i++)
                {
                    TypeDef globalType = module.GlobalType;
                    TypeDefUser typeDefUser = new TypeDefUser(globalType.Name);
                    globalType.Name = GGeneration.GenerateGuidStartingWithLetter();
                    globalType.BaseType = module.CorLibTypes.GetTypeRef("System", "Object");
                    module.Types.Insert(0, typeDefUser);
                    MethodDef methodDef = globalType.FindOrCreateStaticConstructor();
                    MethodDef methodDef2 = typeDefUser.FindOrCreateStaticConstructor();
                    methodDef.Name = GGeneration.GenerateGuidStartingWithLetter();
                    methodDef.IsRuntimeSpecialName = false;
                    methodDef.IsSpecialName = false;
                    methodDef.Access = MethodAttributes.PrivateScope;
                    methodDef2.Body = new CilBody(initLocals: true, new List<Instruction>
                {
                    Instruction.Create(OpCodes.Call, methodDef),
                    Instruction.Create(OpCodes.Ret)
                }, new List<ExceptionHandler>(), new List<Local>());

                    for (int j = 0; j < globalType.Methods.Count; j++)
                    {
                        MethodDef methodDef3 = globalType.Methods[j];

                        if (!methodDef3.FullName.ToLower().StartsWith("system.void")) continue;
                        Console.WriteLine(methodDef3.FullName);
                        if (methodDef3.IsNative)
                        {
                            MethodDefUser methodDefUser = new MethodDefUser(methodDef3.Name, methodDef3.MethodSig.Clone())
                            {
                                Attributes = (MethodAttributes.MemberAccessMask | MethodAttributes.Static),
                                ImplAttributes = MethodImplAttributes.IL,
                                Body = new CilBody()
                            };
                            methodDefUser.Body.Instructions.Add(new Instruction(OpCodes.Jmp, methodDef3));
                            methodDefUser.Body.Instructions.Add(new Instruction(OpCodes.Ret));
                            globalType.Methods[j] = methodDefUser;
                            typeDefUser.Methods.Add(methodDef3);
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

        public MethodDefUser CreateMethod(ModuleDef mod)
        {
            return new MethodDefUser(
                Randomizer.GenerateRandomString(),
                MethodSig.CreateStatic(mod.CorLibTypes.Void),
                MethodImplAttributes.IL,
                MethodAttributes.Public | MethodAttributes.Static)
            {
                Body = new CilBody { Instructions = { OpCodes.Ret.ToInstruction() } }
            };
        }

    }
}
