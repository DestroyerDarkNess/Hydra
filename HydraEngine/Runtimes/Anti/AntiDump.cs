using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
     public class AntiDump : Models.Protection
    {
        public AntiDump() : base("Runtimes.Anti.AntiDump", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD mod)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(AntiDumpRun).Module);
                var cctor = mod.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(AntiDumpRun).MetadataToken));
                var members = InjectHelper.Inject(typeDef, mod.GlobalType, mod);
                var init = (MethodDef)members.Single(method => method.Name == "Initialize");
                cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                init.Name = "<" + Core.Randomizer.GenerateRandomString2() + ">";
                foreach (var md in mod.GlobalType.Methods)
                {
                    if (md.Name != ".ctor") continue;
                    mod.GlobalType.Remove(md);
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
