using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace HydraEngine.Protection.Import
{
     public class ImportProtection : Models.Protection
    {
        public ImportProtection() : base("Protection.Import.ImportProtection", "Renamer Phase", "Description for Renamer Phase") { }

        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";


        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                MethodDef realCctor = module.GlobalType.FindOrCreateStaticConstructor();

                MethodDef Cctor = new MethodDefUser(Core.Randomizer.GenerateRandomString(BaseChars, 20), MethodSig.CreateStatic(module.CorLibTypes.Void),
               MethodImplAttributes.IL | MethodImplAttributes.Managed,
               MethodAttributes.Public | MethodAttributes.Static);
                Cctor.Body = new CilBody();
                Cctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                module.GlobalType.Methods.Add(Cctor);

                realCctor.Body.Instructions.Insert(realCctor.Body.Instructions.Count - 1, OpCodes.Call.ToInstruction(Cctor));


                //var module = context.Module;
                var brigdes = new Dictionary<IMethod, MethodDef>();
                var methods = new Dictionary<IMethod, TypeDef>();
                var field = new FieldDefUser(Core.Randomizer.GenerateRandomString(BaseChars, 20), new FieldSig(module.ImportAsTypeSig(typeof(object[]))), FieldAttributes.Public | FieldAttributes.Static); 
                //  var cctor = context.Cctor;
                foreach (TypeDef type in module.GetTypes().ToArray())
                {
                    if (!Analyzer.CanRename(type)) continue;

                    if (type.IsRuntimeSpecialName || type.IsSpecialName || type.IsWindowsRuntime || type.IsInterface)
                    {
                        continue;
                    }

                    if (type.IsDelegate)
                        continue;
                    if (type.IsGlobalModuleType)
                        continue;
                    if (type.Namespace == "Costura")
                        continue;

                    foreach (MethodDef method in type.Methods.ToArray())
                    {

                        if (!Analyzer.CanRename(method)) continue;

                        if (method.IsRuntimeSpecialName || method.IsRuntime || method.IsStaticConstructor || method.IsVirtual) continue;

                        if (!method.HasBody)
                            continue;
                        if (!method.Body.HasInstructions)
                            continue;
                        if (method.IsConstructor)
                            continue;

                        var instrs = method.Body.Instructions;


                        for (int i = 0; i < instrs.Count; i++)
                        {
                            IMethod idef = null;

                            if (instrs[i].OpCode != OpCodes.Call && instrs[i].OpCode == OpCodes.Callvirt)
                                continue;
                            if (instrs[i].Operand is IMethod)
                            {
                                idef = (IMethod)instrs[i].Operand;
                                if (!idef.IsMethodDef)
                                    continue;

                                var def = idef.ResolveMethodDef();

                                if (def == null)
                                    continue;
                                if (def.HasThis)
                                    continue;

                                if (brigdes.ContainsKey(idef))
                                {
                                    instrs[i].OpCode = OpCodes.Call;
                                    instrs[i].Operand = brigdes[idef];
                                    continue;
                                }

                                var sig = CreateProxySignature(module, def);
                                var delegateType = CreateDelegateType(module, sig);
                                module.Types.Add(delegateType);

                                var methImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
                                var methFlags = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
                                var brigde = new MethodDefUser(Core.Randomizer.GenerateRandomString(BaseChars, 20), sig, methImplFlags, methFlags);
                                brigde.Body = new CilBody();

                                brigde.Body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(field));
                                brigde.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(methods.Count));
                                brigde.Body.Instructions.Add(OpCodes.Ldelem_Ref.ToInstruction());
                                foreach (var parameter in brigde.Parameters)
                                {
                                    parameter.Name = Core.Randomizer.GenerateRandomString(BaseChars, 20);
                                    brigde.Body.Instructions.Add(OpCodes.Ldarg.ToInstruction(parameter));
                                }
                                brigde.Body.Instructions.Add(OpCodes.Call.ToInstruction(delegateType.Methods[1]));
                                brigde.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

                                delegateType.Methods.Add(brigde);

                                instrs[i].OpCode = OpCodes.Call;
                                instrs[i].Operand = brigde;

                                if (idef.IsMethodDef)
                                    methods.Add(def, delegateType);
                                else if (idef.IsMemberRef)
                                    methods.Add(idef as MemberRef, delegateType);

                                brigdes.Add(idef, brigde);
                            }
                        }
                    }
                }

                module.GlobalType.Fields.Add(field);

                var instructions = new List<Instruction>();
                var current = Cctor.Body.Instructions.ToList();
                Cctor.Body.Instructions.Clear();

                instructions.Add(OpCodes.Ldc_I4.ToInstruction(methods.Count));
                instructions.Add(OpCodes.Newarr.ToInstruction(module.CorLibTypes.Object));
                instructions.Add(OpCodes.Dup.ToInstruction());

                var index = 0;

                foreach (var entry in methods)
                {
                    
                    instructions.Add(OpCodes.Ldc_I4.ToInstruction(index));
                    instructions.Add(OpCodes.Ldnull.ToInstruction());
                    instructions.Add(OpCodes.Ldftn.ToInstruction(entry.Key));
                    instructions.Add(OpCodes.Newobj.ToInstruction(entry.Value.Methods[0]));
                    instructions.Add(OpCodes.Stelem_Ref.ToInstruction());
                    instructions.Add(OpCodes.Dup.ToInstruction());
                    index++;
                }

                instructions.Add(OpCodes.Pop.ToInstruction());
                instructions.Add(OpCodes.Stsfld.ToInstruction(field));

                foreach (var instr in instructions)
                    Cctor.Body.Instructions.Add(instr);
                foreach (var instr in current)
                    Cctor.Body.Instructions.Add(instr);

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

        private TypeDef CreateDelegateType(ModuleDef module, MethodSig sig)
        {
            var ret = new TypeDefUser(Core.Randomizer.GenerateRandomString(BaseChars, 20), module.CorLibTypes.GetTypeRef("System", "MulticastDelegate"));
            ret.Attributes = TypeAttributes.Public | TypeAttributes.Sealed;

            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.Object, module.CorLibTypes.IntPtr));
            ctor.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
            ctor.ImplAttributes = MethodImplAttributes.Runtime;
            ret.Methods.Add(ctor);

            var invoke = new MethodDefUser("Invoke", sig.Clone());
            invoke.MethodSig.HasThis = true;
            invoke.Attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            invoke.ImplAttributes = MethodImplAttributes.Runtime;
            ret.Methods.Add(invoke);

            return ret;
        }

        private  MethodSig CreateProxySignature(ModuleDef module, IMethod method)
        {
            IEnumerable<TypeSig> paramTypes = method.MethodSig.Params.Select(type => {
                if (type.IsClassSig && method.MethodSig.HasThis)
                    return module.CorLibTypes.Object;
                return type;
            });
            if (method.MethodSig.HasThis && !method.MethodSig.ExplicitThis)
            {
                TypeDef declType = method.DeclaringType.ResolveTypeDefThrow();
                if (!declType.IsValueType)
                    paramTypes = new[] { module.CorLibTypes.Object }.Concat(paramTypes);
                else
                    paramTypes = new[] { declType.ToTypeSig() }.Concat(paramTypes);
            }
            TypeSig retType = method.MethodSig.RetType;
            if (retType.IsClassSig)
                retType = module.CorLibTypes.Object;
            return MethodSig.CreateStatic(retType, paramTypes.ToArray());
        }




    }
}
