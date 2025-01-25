using dnlib.DotNet.Emit;
using dnlib.DotNet;
using HydraEngine.Core;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

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
