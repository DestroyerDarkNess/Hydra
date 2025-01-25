using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.Renamer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using static HydraEngine.Protection.Renamer.RenamerPhase;

namespace HydraEngine.Protection.LocalF
{
    public class L2F : Models.Protection
    {
        public L2F() : base("Protection.LocalF.L2F", "Renamer Phase", "Description for Renamer Phase") { }

        public string tag { get; set; } = "HailHydra";

        private  Dictionary<Local, FieldDef> _convertedLocals = new Dictionary<Local, FieldDef>();

        private  void Process(ModuleDef module, MethodDef method)
        {

            var cctor = module.GlobalType.FindOrCreateStaticConstructor();
            var body = cctor.Body.Instructions;
            var instrs = method.Body.Instructions;
            var first = instrs.First(x => x.IsLdcI4());
            var value = first.GetLdcI4Value();
            var field = new FieldDefUser(string.Format(tag + "_{0}", Randomizer.GenerateRandomString(Randomizer.BaseChars2, 10)), new FieldSig(module.CorLibTypes.Int32), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);  
            MethodDef mdefuser = new MethodDefUser(string.Format("<{0}>_{1}", Randomizer.GenerateRandomString(Randomizer.BaseChars2, 10), tag), MethodSig.CreateStatic(module.CorLibTypes.Int32),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.HideBySig)
            {
                Body = new CilBody()
            };
            module.GlobalType.Fields.Add(field);
            mdefuser.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
            mdefuser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            module.GlobalType.Methods.Add(mdefuser);
            body.Insert(0, OpCodes.Ldc_I4.ToInstruction(value));
            body.Insert(1, OpCodes.Stsfld.ToInstruction(field));
            first.OpCode = OpCodes.Call;
            first.Operand = mdefuser;
        }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                foreach (var type in module.Types.Where(x => x != module.GlobalType))
                {
                    if (type.IsGlobalModuleType)
                        continue;
                    foreach (var method in type.Methods.Where(x => x.HasBody && x.Body.HasInstructions && !x.IsConstructor))
                    {
                        if (!method.HasBody || !method.Body.HasInstructions)
                            continue;
                        var instrs = method.Body.Instructions;
                        if (!instrs.Any(x => x.IsLdcI4()))
                            continue;

                        Process(module, method);
                    }
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
