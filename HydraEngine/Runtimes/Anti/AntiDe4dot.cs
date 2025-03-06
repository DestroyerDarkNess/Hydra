using dnlib.DotNet;
using EXGuard.Core.EXECProtections;
using System;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
    public class AntiDe4dot : Models.Protection
    {
        public AntiDe4dot() : base("Runtimes.Anti.AntiDe4dot", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD mod)
        {
            try
            {

                foreach (var module in mod.Assembly.Modules)
                {
                    var interfaceM = new InterfaceImplUser(module.GlobalType);
                    for (var i = 0; i < 1; i++)
                    {
                        var typeDef1 = new TypeDefUser(string.Empty, $"Form{i}", module.CorLibTypes.GetTypeRef("System", "Attribute"));
                        var interface1 = new InterfaceImplUser(typeDef1);
                        module.Types.Add(typeDef1);
                        typeDef1.Interfaces.Add(interface1);
                        typeDef1.Interfaces.Add(interfaceM);
                    }
                }

                AntiDe4dot_Inject.Execute(mod);
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
