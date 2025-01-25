using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Calli
{
    public class CallToCalli : Models.Protection
    {
        public CallToCalli() : base("Protection.Calli.CallToCalli", "Renamer Phase", "Description for Renamer Phase") { }

        private string[] a = { "My.", ".My", "Costura" };
        private string[] b = { "Dispose", "ISupportInitialize", "Object" };
       

        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                foreach (var type in module.Types.ToArray())
                {
                    if (!Analyzer.CanRename(type)) continue;
                    foreach (var meth in type.Methods.ToArray())
                    {
                        if (!Analyzer.CanRename(meth)) continue;
                        if (!meth.HasBody) continue;
                        if (!meth.Body.HasInstructions) continue;
                        if (meth.FullName.Contains("My.")) continue;
                        if (meth.FullName.Contains(".My")) continue;
                        if (meth.FullName.Contains("Costura")) continue;
                        if (meth.IsConstructor) continue;
                        if (meth.DeclaringType.IsGlobalModuleType) continue;
                        for (var i = 0; i < meth.Body.Instructions.Count - 1; i++)
                        {
                            try
                            {
                                if (meth.Body.Instructions[i].ToString().Contains("ISupportInitialize") || meth.Body.Instructions[i].OpCode != OpCodes.Call &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Callvirt &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Ldloc_S) continue;

                                if (meth.Body.Instructions[i].ToString().Contains("Object") || meth.Body.Instructions[i].OpCode != OpCodes.Call &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Callvirt &&
                                    meth.Body.Instructions[i].OpCode != OpCodes.Ldloc_S) continue;

                                try
                                {
                                    var membertocalli = (MemberRef)meth.Body.Instructions[i].Operand;
                                    meth.Body.Instructions[i].OpCode = OpCodes.Calli;
                                    meth.Body.Instructions[i].Operand = membertocalli.MethodSig;
                                    meth.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Ldftn, membertocalli));
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }
                    foreach (var md in module.GlobalType.Methods)
                    {
                        if (md.Name != ".ctor") continue;
                        module.GlobalType.Remove(md);
                        break;
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
