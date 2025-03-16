using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Renamer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Method
{
    public class MethodToDelegate : Models.Protection
    {
        public MethodToDelegate() : base("Protection.Method.MethodToDelegate", "Renamer Phase", "Description for Renamer Phase") { }

        public bool DynamicEntryPoint { get; set; } = false;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                foreach (var type in Module.Types.ToArray())
                {
                    if (!AnalyzerPhase.CanRename(type)) continue;
                    foreach (var method in type.Methods.ToArray())
                    {
                        if (method.IsConstructor) continue;
                        if (!method.HasBody || !method.Body.HasInstructions || method.DeclaringType.IsGlobalModuleType) continue;

                        if (method.HasGenericParameters) continue;
                        if (method.IsPinvokeImpl) continue;
                        if (method.IsUnmanagedExport) continue;

                        //if (method.HasByRefParameters()) continue;

                        //var unsafeOpcodes = new[] { OpCodes.Ldind_I1, OpCodes.Stind_I1, OpCodes.Conv_I };
                        //if (method.Body.Instructions.Any(instr => unsafeOpcodes.Contains(instr.OpCode)))
                        //{
                        //    continue;
                        //}

                        if (!AnalyzerPhase.CanRename(method, type)) continue;

                        if (method.HasClosureReferences()) continue;

                        if (method.Body.Instructions.Any(instr => IsAccessingNonPublicMember(instr, type))) continue;

                        FixPrivateAccess(method);
                        FixFieldAndMethodAccess(method);

                        TypeDef ResultMove = MethodMover.MoveMethodILToStaticDelegate(method, Module);
                        if (ResultMove != null)
                        {
                            ResultMove.Name = Guid.NewGuid().ToString("N") + Randomizer.GenerateRandomString(10, 30);

                            try
                            {
                                TypeRef attrRef = Module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CompilerGeneratedAttribute");
                                var ctorRef = new MemberRefUser(Module, ".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void), attrRef);
                                var attr = new CustomAttribute(ctorRef);

                                TypeRef attrRef2 = Module.CorLibTypes.GetTypeRef("System", "EntryPointNotFoundException");
                                var ctorRef2 = new MemberRefUser(Module, ".ctor", MethodSig.CreateInstance(Module.CorLibTypes.Void, Module.CorLibTypes.String), attrRef2);

                                MethodDef NewMethod = ResultMove.Methods.FirstOrDefault(x => x.Name == method.Name);

                                if (NewMethod != null)
                                {
                                    HydraEngine.Core.InjectHelper.AddAttributeToMethod(NewMethod, attr);
                                    NewMethod.Name = $"<{Randomizer.GenerateRandomString(10, 30)}>";
                                    NewMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Protected by https://github.com/DestroyerDarkNess/Hydra"));
                                    NewMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctorRef2));
                                    NewMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            //Console.WriteLine("Failed: " + method.FullName);
                        }

                    }
                }

                if (DynamicEntryPoint)
                {
                    bool Dynamic = new IL2Dynamic().ConvertToDynamic(Module.EntryPoint, Module);
                }


                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
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

        private void FixFieldAndMethodAccess(MethodDef method)
        {
            if (!method.HasBody) return;
            var instrs = method.Body.Instructions;

            foreach (var instr in instrs)
            {
                if (instr.Operand is IField fieldRef)
                {
                    var fieldDef = fieldRef.ResolveFieldDef();
                    if (fieldDef != null && fieldDef.IsPrivate)
                    {
                        fieldDef.Attributes &= ~FieldAttributes.FieldAccessMask;
                        fieldDef.Attributes |= FieldAttributes.Public;
                    }
                }

                if (instr.Operand is IMethod methodRef)
                {
                    var targetMethodDef = methodRef.ResolveMethodDef();
                    if (targetMethodDef != null && targetMethodDef.IsPrivate)
                    {
                        targetMethodDef.Attributes &= ~MethodAttributes.MemberAccessMask;
                        targetMethodDef.Attributes |= MethodAttributes.Public;
                    }
                }
            }
        }

        private void FixPrivateAccess(MethodDef method)
        {
            if (method == null || !method.HasBody)
                return;

            foreach (var instr in method.Body.Instructions)
            {
                if (instr.Operand is IField fieldRef)
                {
                    var fieldDef = fieldRef.ResolveFieldDef();
                    if (fieldDef != null && fieldDef.IsPrivate)
                    {
                        fieldDef.Attributes &= ~FieldAttributes.FieldAccessMask;
                        fieldDef.Attributes |= FieldAttributes.Public;
                    }
                }

                else if (instr.Operand is IMethod methodRef)
                {
                    var targetMethodDef = methodRef.ResolveMethodDef();
                    if (targetMethodDef != null && targetMethodDef.IsPrivate)
                    {
                        targetMethodDef.Attributes &= ~MethodAttributes.MemberAccessMask;
                        targetMethodDef.Attributes |= MethodAttributes.Public;
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