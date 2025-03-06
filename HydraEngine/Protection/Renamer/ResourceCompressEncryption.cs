using dnlib.DotNet;
using EXGuard.Core.EXECProtections;
using System;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Renamer
{
    public class ResourceCompressEncryption : Models.Protection
    {

        public ResourceCompressEncryption() : base("Protection.Resource.CompressEncryption", "Renamer Phase", "Description for Renamer Phase") { }

        public bool UnsafeMutation { get; set; } = true;

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                ResourceProt_Inject.Execute(Module);

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
