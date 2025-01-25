using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Invalid
{
    public class InvalidOpcodes : Models.Protection
    {
        public InvalidOpcodes() : base("Protection.Invalid.InvalidOpcodes", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                foreach (TypeDef typeDef in module.Types.ToArray())
                {
                    if (!Analyzer.CanRename(typeDef)) continue;

                    foreach (MethodDef methodDef in typeDef.Methods.ToArray())
                    {
                        if (!Analyzer.CanRename(methodDef)) continue;

                        bool flag = !methodDef.HasBody || !methodDef.Body.HasInstructions;
                        if (!flag)
                        {
                            //MethodData methodData = PackMethod(methodDef);
                            //MutateMethod(methodDef, methodData);
                            methodDef.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Box, methodDef.Module.Import(typeof(Math))));
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

        private class StoredInstruction
        {
            public OpCode opcode;
            public object operand;
        }

        private class StoredFieldReference
        {
            public string name;
        }

        private class StoredMethodReference
        {
            public string name;
            public StoredTypeReference returnType;
        }

        private class StoredVariableReference
        {
            public StoredTypeReference type;
        }

        private class StoredTypeReference
        {
            public string ns;
            public string name;
        }

        private class MethodData
        {
            public StoredInstruction[] instructions;
        }

        private MethodData PackMethod(MethodDef method)
        {
            var sinst = new List<StoredInstruction>();
            foreach (var inst in method.Body.Instructions)
            {
                if (inst.Operand is Instruction)
                    continue;

                var _op = inst.Operand;
                if (_op is TypeSig typeSig)
                {
                    TypeDef typeDef = typeSig.ToTypeDefOrRef().ResolveTypeDef();
                    var op = typeDef;
                    _op = new StoredTypeReference()
                    {
                        name = op.Name,
                        ns = op.Namespace
                    };
                }
                else if (_op is FieldDef fieldDef)
                {
                    _op = new StoredFieldReference()
                    {
                        name = fieldDef.Name
                    };
                }
                else if (_op is MethodDef methodDef)
                {
                    _op = new StoredMethodReference()
                    {
                        name = methodDef.Name
                    };
                }
                else if (_op is Local local)
                {
                    _op = new StoredVariableReference()
                    {
                        type = new StoredTypeReference()
                        {
                            name = local.Type.FullName,
                            ns = ""
                        }
                    };
                }

                sinst.Add(new StoredInstruction()
                {
                    opcode = inst.OpCode,
                    operand = _op
                });

                if (inst.Operand != null)
                    Console.WriteLine(inst.Operand.GetType());
            }

            var methodData = new MethodData()
            {
                instructions = sinst.ToArray()
            };

            return methodData;
        }

        private void MutateMethod(MethodDef method, MethodData methodData)
        {
         
            method.Body.Instructions.Clear();
            var p = method.Body.Instructions;
            foreach (var storedInst in methodData.instructions)
            {
                Instruction inst;
                if (storedInst.operand is StoredTypeReference typeRef)
                {
                    var type = method.Module.Find(typeRef.ns + "." + typeRef.name, true);
                    inst = new Instruction(storedInst.opcode, method.Module.Import(type));
                }
                else if (storedInst.operand is StoredFieldReference fieldRef)
                {
                    var field = method.Module.Types
                        .SelectMany(t => t.Fields)
                        .FirstOrDefault(f => f.Name == fieldRef.name);
                    inst = new Instruction(storedInst.opcode, method.Module.Import(field));
                }
                else if (storedInst.operand is StoredMethodReference methodRef)
                {
                    var mRef = method.Module.Types
                        .SelectMany(t => t.Methods)
                        .FirstOrDefault(m => m.Name == methodRef.name);
                    inst = new Instruction(storedInst.opcode, method.Module.Import(mRef));
                }
                else if (storedInst.operand is StoredVariableReference varRef)
                {
                    var localVar = new Local(method.Module.ImportAsTypeSig(method.Module.CorLibTypes.GetTypeRef(varRef.type.ns, varRef.type.name).GetType()));
                    method.Body.Variables.Add(localVar);
                    inst = new Instruction(storedInst.opcode, localVar);
                }
                else
                {
                    inst = new Instruction(storedInst.opcode, storedInst.operand);
                }

                p.Add(inst);
            }
        }

        private bool CanRename(MethodDef method)
        {
            return !method.IsConstructor &&
                   !method.DeclaringType.IsForwarder &&
                   !method.IsFamily &&
                   !method.IsStaticConstructor &&
                   !method.IsRuntimeSpecialName &&
                   !method.DeclaringType.IsGlobalModuleType &&
                   !method.Name.Contains("Hydra");
        }

        private static bool CanRename(TypeDef type)
        {
            if (type.Namespace.Contains("My")) return false;
            return !type.IsGlobalModuleType &&
                   type.Interfaces.Count == 0 &&
                   !type.IsSpecialName &&
                   !type.IsRuntimeSpecialName &&
                   !type.Name.Contains("<HailHydra>");
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
