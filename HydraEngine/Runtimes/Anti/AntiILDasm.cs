using dnlib.DotNet;
using System;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
    public class AntiILDasm : Models.Protection
    {
        public AntiILDasm() : base("Runtimes.Anti.AntiILDasm", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD mod)
        {
            try
            {
                foreach (ModuleDef module in mod.Assembly.Modules)
                {
                    TypeRef attrRef = mod.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "SuppressIldasmAttribute");
                    var ctorRef = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attrRef);
                    var attr = new CustomAttribute(ctorRef);
                    module.CustomAttributes.Add(attr);
                }

                //AntiILDasm_Inject.Execute(mod);

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
