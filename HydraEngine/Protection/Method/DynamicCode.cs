using dnlib.DotNet;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Method
{
    public class DynamicCode : Models.Protection
    {
        public DynamicCode() : base("Protection.Method.DynamicCode", "Renamer Phase", "Description for Renamer Phase") { ManualReload = true; }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                if (!File.Exists("Runtime.dll")) File.WriteAllBytes("Runtime.dll", HydraEngine.Properties.Resources.Runtime);

                TempModule = DynCore.Execute.Protect(module);
                if (TempModule == null) throw new Exception("VM Methods Failed!");
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
