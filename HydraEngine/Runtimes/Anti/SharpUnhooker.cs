using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.IO;
using dnlib.DotNet.Emit;
using dnlib.DotNet;
using HydraEngine.Core;
using HydraEngine.Runtimes.Anti.Runtime;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
      public class SharpUnhooker : Models.Protection
    {
        public SharpUnhooker() : base("Runtimes.Anti.SharpUnhooker", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(Unhooker).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(Unhooker).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "IniUnhook");
                cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                init.Name = "<" + Core.Randomizer.GenerateRandomString2() + ">";
                foreach (var md in module.GlobalType.Methods)
                {
                    if (md.Name != ".ctor") continue;
                    module.GlobalType.Remove(md);
                    break;
                }
                HydraEngine.Core.InjectHelper.removeReference(module);
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
