using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.CodeOptimizer
{
   public class OptimizeCode : Models.Protection
    {
        public OptimizeCode() : base("Protection.Proxy.OptimizeCode", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {

                Remove_const_Value(Module);
                ArmDot_Optimize(Module);
                Reduce_MetaData_Confusion(Module);

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

       

        public void Remove_const_Value(ModuleDef module)
        {
            try
            {
                foreach (TypeDef type in module.Types)
                {
                    for (int x = 0; x < type.Fields.Count; x++)
                    {
                        FieldDef field = type.Fields[x];
                        if (field.HasConstant && field.ElementType.Equals(ElementType.Object))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.Array))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.String))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.Boolean))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.Char))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.Ptr))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.SZArray))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.Class))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.I))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.I1))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.I2))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.I4))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.I8))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.R))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.R4))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.R8))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.U))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.U1))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.U4))
                        {
                            type.Fields.RemoveAt(x);
                        }
                        if (field.HasConstant && field.ElementType.Equals(ElementType.U8))
                        {
                            type.Fields.RemoveAt(x);
                        }
                    }
                }
            }
            catch { }
        }
        public void ArmDot_Optimize(ModuleDef module)
        {
            foreach (TypeDef type in module.Types)
            {
                foreach (MethodDef _method in type.Methods)
                {
                    if (_method.HasBody)
                    {
                        using (new AutoMethodBodySimplifyOptimize(_method, true))
                        {
                            _method.Body.MaxStack = ushort.MaxValue;
                            IMethod methodReference = _method.Module.Import(typeof(IntPtr).GetConstructor(new Type[] { typeof(int) }));
                            IMethod methodReference2 = _method.Module.Import(typeof(IntPtr).GetConstructor(new Type[] { typeof(long) }));
                            IMethod methodReference3 = _method.Module.Import(typeof(IntPtr).GetMethod("ToInt32", Type.EmptyTypes));
                            IMethod methodReference4 = _method.Module.Import(typeof(IntPtr).GetMethod("ToInt64", Type.EmptyTypes));
                            IMethod methodReference5 = _method.Module.Import(typeof(UIntPtr).GetConstructor(new Type[] { typeof(uint) }));
                            IMethod methodReference6 = _method.Module.Import(typeof(UIntPtr).GetConstructor(new Type[] { typeof(ulong) }));
                            IMethod methodReference7 = _method.Module.Import(typeof(UIntPtr).GetMethod("ToUInt32", Type.EmptyTypes));
                            IMethod methodReference8 = _method.Module.Import(typeof(UIntPtr).GetMethod("ToUInt64", Type.EmptyTypes));
                            List<Instruction> list = new List<Instruction>();
                            Dictionary<Instruction, Instruction> dictionary = new Dictionary<Instruction, Instruction>();
                            Instruction instruction = null;
                            foreach (Instruction inst in _method.Body.Instructions)
                            {
                                Instruction instA = null;
                                Instruction instB = null;
                                if (inst.OpCode == OpCodes.Newobj)
                                {
                                    IMethod methodReference9 = (IMethod)inst.Operand;
                                    if (MethodReferenceEqualityComparer.Singleton.Equals(methodReference9, methodReference) || MethodReferenceEqualityComparer.Singleton.Equals(methodReference9, methodReference2))
                                    {
                                        instA = new Instruction(OpCodes.Conv_I);
                                    }
                                    else if (MethodReferenceEqualityComparer.Singleton.Equals(methodReference9, methodReference5) || MethodReferenceEqualityComparer.Singleton.Equals(methodReference9, methodReference6))
                                    {
                                        instA = new Instruction(OpCodes.Conv_U);
                                    }
                                }
                                else if (inst.OpCode == OpCodes.Call)
                                {
                                    IMethod mRef = (IMethod)inst.Operand;
                                    if (MethodReferenceEqualityComparer.Singleton.Equals(mRef, methodReference4))
                                    {
                                        instB = instruction.CreateLoadInstructionInsteadOfLoadAddress(inst);
                                        if (instB != null)
                                        {
                                            instA = new Instruction(OpCodes.Conv_I8);
                                        }
                                    }
                                    else if (MethodReferenceEqualityComparer.Singleton.Equals(mRef, methodReference3))
                                    {
                                        instB = instruction.CreateLoadInstructionInsteadOfLoadAddress(inst);
                                        if (instB != null)
                                        {
                                            instA = new Instruction(OpCodes.Conv_I4);
                                        }
                                    }
                                    else if (MethodReferenceEqualityComparer.Singleton.Equals(mRef, methodReference8))
                                    {
                                        instB = instruction.CreateLoadInstructionInsteadOfLoadAddress(inst);
                                        if (instB != null)
                                        {
                                            instA = new Instruction(OpCodes.Conv_U8);
                                        }
                                    }
                                    else if (MethodReferenceEqualityComparer.Singleton.Equals(mRef, methodReference7))
                                    {
                                        instB = instruction.CreateLoadInstructionInsteadOfLoadAddress(inst);
                                        if (instB != null)
                                        {
                                            instA = new Instruction(OpCodes.Conv_U4);
                                        }
                                    }
                                }
                                if (instA == null)
                                {
                                    instA = inst;
                                }
                                list.Add(instA);
                                dictionary.Add(inst, instA);
                                if (instB != null)
                                {
                                    list.Insert(list.IndexOf(instruction), instB);
                                    list.Remove(instruction);
                                    dictionary.Remove(instruction);
                                    dictionary.Add(instruction, instB);
                                }
                                instruction = inst;
                            }
                            _method.Body.SetNewInstructions(list, dictionary);
                        }
                    }
                }
            }
        }
        public void Method_Optimize_A(ModuleDef module)
        {
            try
            {
                foreach (TypeDef typeDefinition in module.GetTypes())
                {
                    foreach (MethodDef methodDefinition in typeDefinition.Methods)
                    {
                        if (methodDefinition.HasBody)
                        {
                            methodDefinition.Body.OptimizeMacros();
                            methodDefinition.Body.OptimizeBranches();
                        }
                    }
                }
            }
            catch { }
        }
        public void Reduce_MetaData_Confusion(ModuleDef module)
        {
            try
            {
                Func<ModuleDef, IList<IDnlibDef>> Targets = (ModuleDef md) => md.FindDefinitions().ToList<IDnlibDef>();
                IMemberDef memberDef = Targets(module) as IMemberDef;
                foreach (TypeDef type in module.GetTypes())
                {
                    TypeDef typeDef = type;
                    if ((typeDef = (memberDef as TypeDef)) != null && !typeDef.IsTypePublic())
                    {
                        if (typeDef.IsEnum)
                        {
                            int num = 0;
                            while (typeDef.Fields.Count != 1)
                            {
                                if (typeDef.Fields[num].Name != "value__")
                                {
                                    typeDef.Fields.RemoveAt(num);
                                }
                                else
                                {
                                    num++;
                                }
                            }
                        }
                    }
                    else if (memberDef is EventDef)
                    {
                        if (memberDef.DeclaringType != null)
                        {
                            memberDef.DeclaringType.Events.Remove(memberDef as EventDef);
                        }
                    }
                    else if (memberDef is PropertyDef && memberDef.DeclaringType != null)
                    {
                        memberDef.DeclaringType.Properties.Remove(memberDef as PropertyDef);
                    }
                }
            }
            catch { }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
