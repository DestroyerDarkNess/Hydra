using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Misc
{


    public class TestProtection : Models.Protection
    {
        public TestProtection() : base("Protection.Misc.TestProtection", "Renamer Phase", "Description for Renamer Phase") { }

        public string Ouput { get; set; } = string.Empty;

        public List<MethodDef> SelectedMethods = null;

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            //try
            //{

            if (string.IsNullOrEmpty(Ouput))
                throw new Exception("Output Path is Empty");

            return true;

            //bool ResultMove = MethodMover.MoveMethodILToStaticClass(module.EntryPoint, module);
            //bool Dynamic = new IL2Dynamic().ConvertToDynamic(module.EntryPoint, module);
            //bool ResultMove2 = MethodMover.MoveMethodILToStaticClass(module.EntryPoint, module);

            //foreach (var type in module.Types.ToArray())
            //{
            //    if (!AnalyzerPhase.CanRename(type)) continue;

            //    foreach (var method in type.Methods.ToArray())
            //    {
            //        if (method.IsConstructor) continue;

            //        if (!method.HasBody) continue;
            //        if (!method.Body.HasInstructions) continue;
            //        if (!AnalyzerPhase.CanRename(method, type)) continue;

            //        if (!method.HasBody) continue;

            //        if (!method.Body.HasInstructions) continue;

            //        if (method.HasGenericParameters) continue;

            //        if (method.IsPinvokeImpl) continue;

            //        if (method.IsUnmanagedExport) continue;

            //        if (method.Body.Instructions.Any(instr => instr.OpCode == OpCodes.Or || instr.OpCode == OpCodes.And))
            //            continue;

            //        var unsafeOpcodes = new[] { OpCodes.Ldind_I1, OpCodes.Stind_I1, OpCodes.Conv_I };
            //        if (method.Body.Instructions.Any(instr => unsafeOpcodes.Contains(instr.OpCode)))
            //        {
            //            continue;
            //        }

            //        //bool TestEmulated = MethodMover.EmulateTest(method, module);
            //        //if (!TestEmulated) continue;
            //        if (method.IsStatic)
            //        {
            //            bool ResultMove = MethodMover.MoveMethodILToStaticClass(method, module);
            //            if (!ResultMove) continue;

            //            bool Dynamic = new IL2Dynamic().ConvertToDynamic(method, module);
            //        }


            //    }
            //}


            foreach (var method in SelectedMethods)
            {
                //if (method.IsConstructor) continue;

                //if (!method.HasBody) continue;
                //if (!method.Body.HasInstructions) continue;

                //if (!method.HasBody) continue;

                //if (!method.Body.HasInstructions) continue;

                //if (method.HasGenericParameters) continue;

                //if (method.IsPinvokeImpl) continue;

                //if (method.IsUnmanagedExport) continue;

                //if (method.Body.Instructions.Any(instr => instr.OpCode == OpCodes.Or || instr.OpCode == OpCodes.And)) continue;

                //var unsafeOpcodes = new[] { OpCodes.Ldind_I1, OpCodes.Stind_I1, OpCodes.Conv_I };
                //if (method.Body.Instructions.Any(instr => unsafeOpcodes.Contains(instr.OpCode))) continue;

                //bool TestEmulated = MethodMover.EmulateTest(method, module);
                //if (!TestEmulated) continue;
                //if (method.IsStatic)
                //{
                //    bool ResultMove = MethodMover.MoveMethodILToStaticClass(method, module);
                //    if (!ResultMove) continue;

                //    bool Dynamic = new IL2Dynamic().ConvertToDynamic(method, module);
                //}


            }


            return true;
            //}
            //catch (Exception Ex)
            //{
            //    this.Errors = Ex;
            //    return false;
            //}
        }


        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

    }
}
