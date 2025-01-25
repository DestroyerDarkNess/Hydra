using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Runtimes.Exceptions.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Exceptions
{
    public class ExpMan : Models.Protection
    {
        public ExpMan() : base("Runtimes.Exceptions.ExpMan", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                System.Type ExManager = null;

                bool IsWindowsForms = module.GetAssemblyRefs().FirstOrDefault(assRef => assRef.Name == "System.Windows.Forms") != null;

                if (IsWindowsForms) {
                    ExManager = typeof(ExceptionManager);
                }   else  {
                    ExManager = typeof(ExceptionManagerCore);
                }


                var typeModule = ModuleDefMD.Load(ExManager.Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(ExManager.MetadataToken));
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
            catch (System.Exception Ex)
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
