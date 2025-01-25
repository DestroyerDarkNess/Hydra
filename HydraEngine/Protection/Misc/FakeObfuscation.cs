using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Misc
{
    public class FakeObfuscation : Models.Protection
    {
        public FakeObfuscation() : base("Protection.Misc.FakeObfuscation", "Fake Obfuscation", "Description for Renamer Phase") { }

        static string[] attrib = { "ObfuscatedByGoliath", "NineRays.Obfuscator.Evaluation", "NetGuard", "dotNetProtector", "YanoAttribute", "Xenocode.Client.Attributes.AssemblyAttributes.ProcessedByXenocode", "PoweredByAttribute", "DotNetPatcherPackerAttribute", "DotNetPatcherObfuscatorAttribute", "DotfuscatorAttribute", "CryptoObfuscator.ProtectedWithCryptoObfuscatorAttribute", "BabelObfuscatorAttribute", "BabelAttribute", "AssemblyInfoAttribute", "ZYXDNGuarder", "ConfusedByAttribute", "HydraProtectorAttribute" };

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                for (int i = 0; i < attrib.Length; i++)
                {
                    var fakeattrib = new TypeDefUser(attrib[i], attrib[i], module.CorLibTypes.Object.TypeDefOrRef);
                    fakeattrib.Attributes = TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.WindowsRuntime;
                    module.Types.Add(fakeattrib);
                }

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
