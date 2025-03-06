using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
    public class AntiTamper : Models.Protection
    {
        public AntiTamper() : base("Runtimes.Anti.AntiTamper", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(EofAntiTamper).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(EofAntiTamper).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "Initializer");
                cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                init.Name = "<" + Core.Randomizer.GenerateRandomString2() + ">";
                foreach (var md in module.GlobalType.Methods)
                {
                    if (md.Name != ".ctor") continue;
                    module.GlobalType.Remove(md);
                    break;
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

        public static void Sha256(string filePath)
        {
            var sha256Bytes = SHA256.Create().ComputeHash(File.ReadAllBytes(filePath));
            var stream = new FileStream(filePath, FileMode.Append);
            stream.Write(sha256Bytes, 0, sha256Bytes.Length);
            stream.Dispose();
        }

    }
}
