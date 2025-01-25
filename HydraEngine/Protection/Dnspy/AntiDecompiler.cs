using AsmResolver.DotNet;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Decompiler
{
    public class AntiDecompiler : Models.Protection
    {
        public AntiDecompiler() : base("Protection.Dnspy.AntiDecompiler", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        { 
            try
            {
                var moduleType = module.GlobalType;
                var nestedType = new TypeDefUser(
                    string.Empty,
                    "HydraCrash",
                    module.CorLibTypes.Object.TypeDefOrRef
                );
                nestedType.Attributes = TypeAttributes.Sealed | TypeAttributes.ExplicitLayout;
                moduleType.NestedTypes.Add(nestedType);

                foreach (var type in module.Types)
                {
                    if (!Analyzer.CanRename(type)) continue;
                    if (type.DeclaringType == moduleType && type.IsNested)
                    {
                        type.Attributes = TypeAttributes.Sealed | TypeAttributes.ExplicitLayout;
                    }
                    foreach (var meth in type.Methods)
                    {
                        TypeDef NewType = new TypeDefUser(Randomizer.GenerateRandomString(40));
                        NewType.Methods.Add(new MethodDefUser());
                        NewType.NestedTypes.Add(new TypeDefUser(Randomizer.GenerateRandomString(40)));
                        NewType.Methods.Add(new MethodDefUser());
                        type.NestedTypes.Add(NewType);
                        type.Events.Add(new EventDefUser());

                        if (!Analyzer.CanRename(meth)) continue;
                        if (!meth.HasBody)
                        {
                            break;
                        }

                        var body = meth.Body;
                        var target = body.Instructions[0];
                        var item = Instruction.Create(OpCodes.Br_S, target);
                        var instruction3 = Instruction.Create(OpCodes.Pop);
                        var random = new Random();
                        var instruction4 = GetRandomInstruction(random);

                        body.Instructions.Insert(0, instruction4);
                        body.Instructions.Insert(1, instruction3);
                        body.Instructions.Insert(2, item);

                        if (body.ExceptionHandlers != null)
                        {
                            foreach (var handler in body.ExceptionHandlers)
                            {
                                if (handler.TryStart == target)
                                {
                                    handler.TryStart = item;
                                }
                                else if (handler.HandlerStart == target)
                                {
                                    handler.HandlerStart = item;
                                }
                                else if (handler.FilterStart == target)
                                {
                                    handler.FilterStart = item;
                                }
                            }
                        }
                        if (meth == null || meth.HasBody == false) return false;

                        PatchMethod(module, meth);
                    }

                    MethodDef Constructor = type.FindDefaultConstructor();
                    if (Constructor != null)
                        PatchMethod(module, Constructor);

                    MethodDef StaticConstructor = type.FindStaticConstructor();
                    if (StaticConstructor != null)
                        PatchMethod(module, StaticConstructor);
                }

                if (module.EntryPoint != null)
                    PatchMethod(module, module.EntryPoint);

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

        public void PatchMethod(ModuleDef module, MethodDef method)
        {
            Local Sugar = new Local(module.Import(typeof(int)).ToTypeSig());
            Local Sugar_2 = new Local(module.Import(typeof(bool)).ToTypeSig());

            method.Body.Variables.Add(Sugar);
            method.Body.Variables.Add(Sugar_2);

            Instruction operand = null;
            Instruction instruction = new Instruction(OpCodes.Ret);
            Instruction instruction2 = new Instruction(OpCodes.Ldc_I4_1);

            method.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4_0));
            method.Body.Instructions.Insert(1, new Instruction(OpCodes.Stloc, Sugar));
            method.Body.Instructions.Insert(2, new Instruction(OpCodes.Br, instruction2));

            Instruction instruction3 = new Instruction(OpCodes.Ldloc, Sugar);

            method.Body.Instructions.Insert(3, instruction3);
            method.Body.Instructions.Insert(4, new Instruction(OpCodes.Ldc_I4_0));
            method.Body.Instructions.Insert(5, new Instruction(OpCodes.Ceq));
            method.Body.Instructions.Insert(6, new Instruction(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Insert(7, new Instruction(OpCodes.Ceq));
            method.Body.Instructions.Insert(8, new Instruction(OpCodes.Stloc, Sugar_2));
            method.Body.Instructions.Insert(9, new Instruction(OpCodes.Ldloc, Sugar_2));
            method.Body.Instructions.Insert(10, new Instruction(OpCodes.Brtrue, method.Body.Instructions[sizeof(Decimal) - 6]));
            method.Body.Instructions.Insert(11, new Instruction(OpCodes.Ret));
            method.Body.Instructions.Insert(12, new Instruction(OpCodes.Calli));
            method.Body.Instructions.Insert(13, new Instruction(OpCodes.Sizeof, operand));
            method.Body.Instructions.Insert(method.Body.Instructions.Count, instruction2);
            method.Body.Instructions.Insert(method.Body.Instructions.Count, new Instruction(OpCodes.Stloc, Sugar_2));
            method.Body.Instructions.Insert(method.Body.Instructions.Count, new Instruction(OpCodes.Br, instruction3));
            method.Body.Instructions.Insert(method.Body.Instructions.Count, instruction);

            ExceptionHandler item2 = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                HandlerStart = method.Body.Instructions[10],
                HandlerEnd = method.Body.Instructions[11],
                TryEnd = method.Body.Instructions[14],
                TryStart = method.Body.Instructions[12]
            };

            bool flag3 = !method.Body.HasExceptionHandlers;

            if (flag3)
            {
                method.Body.ExceptionHandlers.Add(item2);
            }

            operand = new Instruction(OpCodes.Br, instruction);
            method.Body.OptimizeBranches();
            method.Body.OptimizeMacros();
        }

        private  Instruction GetRandomInstruction(Random random)
        {
            var opcode = random.Next(0, 5);

            switch (opcode)
            {
                case 0:
                    return Instruction.Create(OpCodes.Ldnull);
                case 1:
                    return Instruction.Create(OpCodes.Ldc_I4_0);
                case 2:
                    return Instruction.Create(OpCodes.Ldstr, "Isolator");
                case 3:
                    return Instruction.Create(OpCodes.Ldc_I8, (uint)random.Next());
                default:
                    return Instruction.Create(OpCodes.Ldc_I8, (long)random.Next());
            }
        }


    }
}
