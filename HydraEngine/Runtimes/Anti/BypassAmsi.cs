using dnlib.DotNet.Emit;
using dnlib.DotNet;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
    public class BypassAmsi : Models.Protection
    {
        public BypassAmsi() : base("Runtimes.Anti.BypassAmsi", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(AmsiBypass).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(AmsiBypass).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "BypassAmsi");
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
