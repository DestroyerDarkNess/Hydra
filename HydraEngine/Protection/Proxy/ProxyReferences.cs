using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using HydraEngine.Core;

namespace HydraEngine.Protection.Proxy
{
    public class ProxyReferences : Models.Protection
    {
        public ProxyReferences() : base("Protection.Proxy.ProxyReferences", "Renamer Phase", "Description for Renamer Phase") { }

        private List<MethodDef> usedMethods = new List<MethodDef>();

        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private  bool CanObfuscate(MethodDef methodDef)
        {
            if (!methodDef.HasBody || !methodDef.Body.HasInstructions || methodDef.DeclaringType.IsGlobalModuleType)
            {
                return false;
            }
            return true;
        }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                Helper helper = new Helper();
                FixProxy(module);

                foreach (var typeDef in module.Types.ToArray())
                {
                    if (typeDef.IsGlobalModuleType)
                    {
                        continue;
                    }

                    if (typeDef.Name == "GeneratedInternalTypeHelper" || typeDef.Name == "Resources" || typeDef.Name == "Settings")
                    {
                        continue;
                    }

                    if (!Analyzer.CanRename(typeDef)) continue;

                    foreach (var methodDef in typeDef.Methods.ToArray())
                    {
                        if (usedMethods.Contains(methodDef) || !CanObfuscate(methodDef))
                        {
                            continue;
                        }

                        if (!Analyzer.CanRename(methodDef)) continue;
                        if (!methodDef.HasBody)
                        {
                            continue;
                        }

                        if (methodDef.IsVirtual || methodDef.IsSpecialName)
                        {
                            continue;
                        }

                        if (methodDef.Name == ".ctor" || methodDef.Name == ".cctor")
                        {
                            continue;
                        }

                        var instructions = methodDef.Body.Instructions.ToArray();
                        for (int i = 0; i < instructions.Length; i++)
                        {
                            var instruction = instructions[i];

                            if (instruction.OpCode == OpCodes.Call)
                            {
                                HandleCall(helper, typeDef, instruction);
                            }

                            if (instruction.OpCode == OpCodes.Stfld)
                            {
                                HandleStfld(module, methodDef, instruction);
                            }

                            if (instruction.OpCode == OpCodes.Ldfld)
                            {
                                HandleLdfld(helper, methodDef, instruction);
                            }

                            //if (instruction.OpCode == OpCodes.Newobj)
                            //{
                            //    HandleNewobj(helper, methodDef, instruction);
                            //}
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

        private void HandleNewobj(Helper helper, MethodDef methodDef, Instruction instruction)
        {
            if (instruction.Operand is IMethodDefOrRef methodDefOrRef && !methodDefOrRef.IsMethodSpec)
            {
                var newMethod = helper.GenerateMethod(methodDefOrRef, methodDef);
                if (newMethod != null)
                {
                    methodDef.DeclaringType.Methods.Add(newMethod);
                    usedMethods.Add(newMethod);
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newMethod;
                    usedMethods.Add(newMethod);
                }
            }
        }

        private void HandleStfld(ModuleDef moduleDef, MethodDef methodDef, Instruction instruction)
        {
            if (instruction.Operand is FieldDef fieldDef)
            {
                var cilBody = new CilBody
                {
                    Instructions =
                    {
                        OpCodes.Nop.ToInstruction(),
                        OpCodes.Ldarg_0.ToInstruction(),
                        OpCodes.Ldarg_1.ToInstruction(),
                        OpCodes.Stfld.ToInstruction(fieldDef),
                        OpCodes.Ret.ToInstruction()
                    }
                };

                var methodSig = MethodSig.CreateInstance(moduleDef.CorLibTypes.Void, fieldDef.FieldSig.GetFieldType());
                methodSig.HasThis = true;

                var newMethod = new MethodDefUser(Helper.InvisibleName, methodSig)
                {
                    Body = cilBody,
                    IsHideBySig = true
                };

                methodDef.DeclaringType.Methods.Add(newMethod);
                usedMethods.Add(newMethod);
                instruction.OpCode = OpCodes.Call;
                instruction.Operand = newMethod;
            }
        }

        private void HandleLdfld(Helper helper, MethodDef methodDef, Instruction instruction)
        {
            if (instruction.Operand is FieldDef fieldDef)
            {
                var newMethod = helper.GenerateMethod(fieldDef, methodDef);
                if (newMethod != null)
                {
                    instruction.OpCode = OpCodes.Call;
                    instruction.Operand = newMethod;
                    usedMethods.Add(newMethod);
                }
            }
        }

        private void HandleCall(Helper helper, TypeDef typeDef, Instruction instruction)
        {
            if (instruction.Operand is MemberRef memberRef)
            {
                if (!memberRef.FullName.Contains("Collections.Generic") &&
                    !memberRef.Name.Contains("ToString") &&
                    !memberRef.FullName.Contains("Thread::Start"))
                {
                    var newMethod = helper.GenerateMethod(typeDef, memberRef, memberRef.HasThis, memberRef.FullName.StartsWith("System.Void"));
                    if (newMethod != null)
                    {
                        usedMethods.Add(newMethod);
                        typeDef.Methods.Add(newMethod);
                        instruction.Operand = newMethod;
                        newMethod.Body.Instructions.Add(new Instruction(OpCodes.Ret));
                    }
                }
            }
        }

        [CompilerGenerated]
        private void FixProxy(ModuleDef moduleDef)
        {
            var assemblyResolver = new AssemblyResolver();
            var moduleContext = new ModuleContext(assemblyResolver);

            assemblyResolver.DefaultModuleContext = moduleContext;
            assemblyResolver.EnableTypeDefCache = true;

            var assemblyRefs = moduleDef.GetAssemblyRefs().ToList();
            moduleDef.Context = moduleContext;

            foreach (var assemblyRef in assemblyRefs)
            {
                if (assemblyRef != null)
                {
                    var assemblyDef = assemblyResolver.Resolve(assemblyRef.FullName, moduleDef);
                    if (assemblyDef != null)
                    {
                        ((AssemblyResolver)moduleDef.Context.AssemblyResolver).AddToCache(assemblyDef);
                    }
                }
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
