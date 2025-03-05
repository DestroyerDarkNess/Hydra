using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EXGuard.Core.EXECProtections;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti
{
    public class Antimanything : Models.Protection
    {
        public Antimanything() : base("Runtimes.Anti.Antimanything", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(SelfDeleteClass).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(SelfDeleteClass).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "Init");
                foreach (Instruction Instruction in init.Body.Instructions.Where((Instruction I) => I.OpCode == OpCodes.Ldstr))
                {
                    if (Instruction.Operand.ToString() == "message")
                        Instruction.Operand = this.ExitMethod;
                }
                cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                init.Name = "<" + Core.Randomizer.GenerateRandomString2() + ">";
                foreach (var md in module.GlobalType.Methods)
                {
                    if (md.Name != ".ctor") continue;
                    module.GlobalType.Remove(md);
                    break;
                }
                AntiDnspy_Inject.Execute(module);
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
