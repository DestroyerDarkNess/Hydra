using dnlib.DotNet;
using dnlib.DotNet.Emit;
using VM.Core.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using HydraEngine.Protection.VM;

namespace VM.Core.Protections.Impl.Virtualization.RuntimeProtections
{
    internal static class Strings
    {
        internal static void Execute(ModuleDefMD Module)
        {
            foreach (TypeDef Type in  Module.GetTypes().Where(T => T.HasMethods).ToArray())
            {
                ITypeDefOrRef valueTypeRef = Virtualizer.Instance.Module.Import(typeof(ValueType));
                foreach (MethodDef Method in Type.Methods.Where(m => m.HasBody && m.Body.HasInstructions).ToArray())
                {
                    if (Module == Virtualizer.Instance.Module && Method.IsConstructor)
                        continue;
                    //IsConstructor avoid virtualized constructors strings to be processed, on a sample it fail so I need to fix it later..

                    if (Module == Virtualizer.Instance.Module && !Virtualizer.Instance.VirtualizedMethods.Contains(Method.FullName))
                        continue;
                    try
                    {
                        for (int i = 0; i < Method.Body.Instructions.ToArray().Count(); i++)
                        {
                            if (Method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                            {
                                string str = Method.Body.Instructions[i].Operand.ToString();
                                if(str == string.Empty || str.Length == 0 || str == " ")
                                    continue;

                                TypeDef classWithLayout = null;
                                TypeDef ourType;
                                try
                                {
                                    ourType = (TypeDef)Module.Types.Single(t => t.Name == $"'__StaticArrayInitTypeSize={str.Length}'");
                                    classWithLayout = ourType;
                                }
                                catch
                                {
                                    ourType = new TypeDefUser($"'__StaticArrayInitTypeSize={str.Length}'", valueTypeRef);
                                    classWithLayout = ourType;
                                    classWithLayout.Attributes |= TypeAttributes.Sealed | TypeAttributes.ExplicitLayout;
                                    classWithLayout.ClassLayout = new ClassLayoutUser(1, (uint)str.Length);
                                    Module.Types.Add(classWithLayout);
                                }
                                FieldDef fieldWithRVA = new FieldDefUser($"'$$Method{Method.MDToken.ToString()}-{Generator.RandomName()}'", new FieldSig(classWithLayout.ToTypeSig()), FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.HasFieldRVA);
                                fieldWithRVA.InitialValue = Encoding.UTF8.GetBytes(str);
                                Method.DeclaringType.Fields.Add(fieldWithRVA);
                                FieldDef fieldInjectedArray = new FieldDefUser(Generator.RandomName(), new FieldSig(new SZArraySig(Module.CorLibTypes.Byte)), FieldAttributes.Static | FieldAttributes.Assembly);
                                //Module.GlobalType.Fields.Add(fieldInjectedArray);
                                Method.DeclaringType.Fields.Add(fieldInjectedArray);
                                ITypeDefOrRef runtimeHelpers = Virtualizer.Instance.Module.Import(typeof(RuntimeHelpers));
                                IMethod initArray = Virtualizer.Instance.Module.Import(typeof(RuntimeHelpers).GetMethod("InitializeArray", new Type[] { typeof(Array), typeof(RuntimeFieldHandle) }));

                                //MethodDef cctor = Module.GlobalType.FindOrCreateStaticConstructor();
                                MethodDef cctor = Method.DeclaringType.FindOrCreateStaticConstructor();
                                IList<Instruction> instrs = cctor.Body.Instructions;
                                instrs.Insert(0, new Instruction(OpCodes.Ldc_I4, str.Length));
                                instrs.Insert(1, new Instruction(OpCodes.Newarr, Module.CorLibTypes.Byte.ToTypeDefOrRef()));
                                instrs.Insert(2, new Instruction(OpCodes.Dup));
                                instrs.Insert(3, new Instruction(OpCodes.Ldtoken, fieldWithRVA));
                                instrs.Insert(4, new Instruction(OpCodes.Call, initArray));
                                instrs.Insert(5, new Instruction(OpCodes.Stsfld, fieldInjectedArray));

                                Method.Body.Instructions[i].OpCode = OpCodes.Ldfld;
                                Method.Body.Instructions[i].Operand = fieldInjectedArray;

                                Method.Body.Instructions.Insert(i, new Instruction(OpCodes.Call, Virtualizer.Instance.Module.Import(typeof(Encoding).GetMethod("get_UTF8", new Type[] { }))));
                                Method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Ldnull));
                                Method.Body.Instructions.Insert(i + 3, new Instruction(OpCodes.Callvirt, Virtualizer.Instance.Module.Import(typeof(Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }))));
                            }
                        }
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}