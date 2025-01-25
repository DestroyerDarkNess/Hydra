using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Models
{
       public class NETBuilderInjection : Models.Pack
    {
        public NETBuilderInjection() : base("Protection.Pack.NETBuilderInjection", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module, string Ouput)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public override async Task<bool> Execute(string FilePath, string Ouput)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

    }
}
