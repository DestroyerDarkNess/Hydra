using dnlib.DotNet;
using System;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Misc
{


    public class TestProtection : Models.Protection
    {
        public TestProtection() : base("Protection.Misc.TestProtection", "Renamer Phase", "Description for Renamer Phase") { }

        public string Ouput { get; set; } = string.Empty;


        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                if (string.IsNullOrEmpty(Ouput))
                    throw new Exception("Output Path is Empty");

                //var ILDyn = new IL2Dynamic();

                //foreach (var method in module.GlobalType.Methods.ToArray())
                //{

                //    if (!method.HasBody) continue;

                //    if (!method.Body.HasInstructions) continue;

                //    ILDyn.CtorCallProtection(method);

                //    if (method == module.GlobalType.FindOrCreateStaticConstructor())
                //        ILDyn.ConvertToDynamic(method, module);

                //}



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

    }
}
