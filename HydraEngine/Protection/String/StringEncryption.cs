using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using HydraEngine.Protection.String.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.String
{
     public class StringEncryption : Models.Protection
    {
        public StringEncryption() : base("Protection.String.StringEncryption", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                MethodDef decryptMethod = InjectMethod(module, "Decrypt_Base64");

                foreach (TypeDef type in module.Types)
                {
                    if (type.IsGlobalModuleType || type.Name == "Resources" || type.Name == "Settings")
                        continue;

                    foreach (MethodDef method in type.Methods)
                    {
                        if (!method.HasBody)
                            continue;
                        if (method == decryptMethod)
                            continue;

                        method.Body.KeepOldMaxStack = true;

                        for (int i = 0; i < method.Body.Instructions.Count; i++)
                        {
                            if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)    // String
                            {
                                string oldString = method.Body.Instructions[i].Operand.ToString();  //Original String

                                method.Body.Instructions[i].Operand = Utils.Encrypt_Base64(oldString);
                                method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Call, decryptMethod));
                            }
                        }

                        method.Body.SimplifyBranches();
                        method.Body.OptimizeBranches();
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

        private static MethodDef InjectMethod(ModuleDef module, string methodName)
        {
            ModuleDefMD typeModule = ModuleDefMD.Load(typeof(DecryptionHelper).Module);
            TypeDef typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(DecryptionHelper).MetadataToken));
            IEnumerable<IDnlibDef> members = InjectHelper.Inject(typeDef, module.GlobalType, module);
            MethodDef injectedMethodDef = (MethodDef)members.Single(method => method.Name == methodName);

            foreach (MethodDef md in module.GlobalType.Methods)
            {
                if (md.Name == ".ctor")
                {
                    module.GlobalType.Remove(md);
                    break;
                }
            }

            return injectedMethodDef;
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
