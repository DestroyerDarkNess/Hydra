using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Dnspy
{
    public class NopAttack : Models.Protection
    {
        public NopAttack() : base("Protection.Dnspy.NopAttack", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        { 
            try
            {
                TypeDef ModGlobalModule = module.GlobalType;

                if (ModGlobalModule == null) return false;

                var method = new MethodDefUser(
                    "AntiDnSpy",
                    MethodSig.CreateStatic(module.CorLibTypes.Void),
                    MethodImplAttributes.IL | MethodImplAttributes.Managed,
                    MethodAttributes.Public | MethodAttributes.Static);

                ModGlobalModule.Methods.Add(method);

                var body = new CilBody();
                method.Body = body;

                body.Instructions.Add(OpCodes.Ret.ToInstruction());

                for (var i = 0; i < 100000; i++)
                {
                    body.Instructions.Insert(0, OpCodes.Nop.ToInstruction());
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
