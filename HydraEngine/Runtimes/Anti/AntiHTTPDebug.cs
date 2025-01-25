using dnlib.DotNet.Emit;
using dnlib.DotNet;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydraEngine.Runtimes.Anti.Runtime;

namespace HydraEngine.Runtimes.Anti
{
     public class AntiHTTPDebug : Models.Protection
    {
        public AntiHTTPDebug() : base("Runtimes.Anti.AntiHTTPDebug", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                var typeModule = ModuleDefMD.Load(typeof(AntiHTTPDebuggers).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(AntiHTTPDebuggers).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "Initialize");
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
