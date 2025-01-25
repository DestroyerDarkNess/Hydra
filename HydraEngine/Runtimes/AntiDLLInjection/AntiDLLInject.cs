using dnlib.DotNet.Emit;
using dnlib.DotNet;
using HydraEngine.Core;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydraEngine.Runtimes.AntiDLLInjection.Runtime;

namespace HydraEngine.Runtimes.AntiDLLInjection
{
     public class AntiDLLInject : Models.Protection
    {
        public AntiDLLInject() : base("Runtimes.Anti.AntiDLLInject", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(LoadLibraryDetour).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(LoadLibraryDetour).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "Initialize");
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
    }
}
