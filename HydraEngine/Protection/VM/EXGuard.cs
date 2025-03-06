
using dnlib.DotNet;
using EXGuard.Core.EXECProtections;
using EXGuard.Internal;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HydraEngine.Protection.VM
{
    public class EXGuard : Models.Protection
    {
        public EXGuard() : base("Protection.VM.EXGuard", "Renamer Phase", "Description for Renamer Phase") { ManualReload = true; }

        public bool Protect { get; set; } = false;

        public bool VMStrings { get; set; } = false;

        public string ouput { get; set; } = string.Empty;

        public List<MethodDef> SelectedMethods = null;

        public override async Task<bool> Execute(string moduledef)
        {
            throw new NotImplementedException();
        }
        public override async Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            try
            {
                string RuntimeVM_Name = $"{Randomizer.GenerateRandomString(Randomizer.BaseChars, 5)}.dll";

                string Tempoutput = Path.Combine(Path.GetTempPath(), module.Name);

                if (SelectedMethods == null || SelectedMethods.Count == 0)
                {
                    throw new Exception("No Selected Methods");
                }
                else
                {
                    HashSet<MethodDef> methodSet = new HashSet<MethodDef>(SelectedMethods);

                    if (VMStrings)
                    {
                        //foreach (var method in SelectedMethods)
                        //{
                        //    new HideCallString(module).Execute(method.DeclaringType, method);
                        //    //new HideCallNumber(module).Execute(module.GlobalType, method);
                        //} 
                        foreach (TypeDef type in module.Types.Where(t => t.HasMethods))
                        {
                            foreach (var method in type.Methods)
                            {
                                if (!method.HasBody) continue;
                                if (!method.Body.HasInstructions) continue;
                                new HideCallString(module).Execute(method.DeclaringType, method);
                                //new HideCallNumber(module).Execute(module.GlobalType, method);
                            }

                        }
                    }

                    new EXGuardTask().Exceute(module, methodSet, Tempoutput, RuntimeVM_Name, "", "");
                }

                int count = 0;
                while (!File.Exists(Tempoutput))
                {
                    await Task.Delay(1000);
                    if (count > 4) break;
                    count += 1;
                }

                if (!File.Exists(Tempoutput)) throw new Exception("AV Compromised, please Disable and try again");

                string Runtime = Path.Combine(Path.GetTempPath(), RuntimeVM_Name);
                if (File.Exists(Runtime) && Directory.Exists(ouput))
                {
                    File.Copy(Runtime, Path.Combine(ouput, RuntimeVM_Name));
                }

                TempModule = new MemoryStream(File.ReadAllBytes(Tempoutput));

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }
    }
}
