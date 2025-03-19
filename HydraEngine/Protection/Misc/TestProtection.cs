using dnlib.DotNet;
using System;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Misc
{


    public class TestProtection : Models.Protection
    {
        public TestProtection() : base("Protection.Misc.TestProtection", "Renamer Phase", "Description for Renamer Phase") { }

        public string Ouput { get; set; } = string.Empty;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            if (string.IsNullOrEmpty(Ouput))
                throw new Exception("Output Path is Empty");

            //ModuleDefMD CurrentProtected = ModuleDefMD.Load(TempModule);

            //foreach (var type in Module.Types)
            //{
            //    if (type.IsGlobalModuleType) continue;
            //    foreach (var method in type.Methods)
            //    {
            //        if (!method.HasBody || !method.Body.HasInstructions) continue;
            //        if (method == Module.EntryPoint) continue;
            //        if (method.Body.Instructions.Any(instr => IsDynMethod(instr, type)))
            //        {
            //            Console.WriteLine($"[DynamicCode] Found Dynamic Method: {method.FullName}");
            //            bool Dynamic = new IL2Dynamic().ConvertToDynamic(method, Module);
            //        }
            //    }
            //}

            //TempModule = new MemoryStream();
            //CurrentProtected.Write(TempModule);



            return true;
        }


        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        //private bool IsDynMethod(Instruction instr, TypeDef declaringType)
        //{
        //    if (instr.OpCode == OpCodes.Call && instr.Operand is IMethod method)
        //    {
        //        if (method.DeclaringType.FullName == "ConversionBack.Dyn" && method.Name == "Run")
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

    }
}
