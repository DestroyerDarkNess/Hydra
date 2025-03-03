
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

        public string ouput { get; set; } = string.Empty;

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

                var methods = new HashSet<MethodDef>();
                //methods.Add(module.EntryPoint);

                //module.AssemblyReferencesAdder();
                foreach (var type in module.Types.ToArray())
                {
                    if (type == module.GlobalType) continue;

                    //if (!AnalyzerPhase.CanRename(type)) continue;
                    //if (!Analyzer.CanRename(type)) continue;
                    //if (type.HasGenericParameters) continue;
                    //if (type.CustomAttributes.Count(i => i.TypeFullName.Contains("CompilerGenerated")) != 0) continue;
                    //if (type.IsValueType) continue;

                    if (type.Namespace == string.Empty) continue;

                    if (type.Name.Contains("ImplementationDetails>")) continue;

                    if (type.Name.Contains("<>f__AnonymousType")) continue;

                    foreach (var method in type.Methods.ToArray())
                    {
                        Console.WriteLine("Method1: " + method.FullName);
                        //if (method != module.GlobalType.FindOrCreateStaticConstructor()) continue;

                        if (!Renamer.AnalyzerPhase.CanRename(method, type)) continue;
                        if (!Analyzer.CanRename(method)) continue;
                        //if (method.Parameters.Count != 0) continue;
                        if (method.IsConstructor) continue;
                        if (!method.HasBody) continue;
                        if (method.Body.Instructions.Count < 2) continue;
                        if (type.IsGlobalModuleType && method.IsConstructor) continue;
                        //if (method.HasGenericParameters) continue;
                        if (method.CustomAttributes.Count(i => i.TypeFullName.Contains("CompilerGenerated")) != 0) continue;
                        //if (method.ReturnType == null) continue;
                        //if (method.ReturnType.IsGenericParameter) continue;
                        //if (method.Parameters.Count(i => i.Type.FullName.EndsWith("&") && i.ParamDef.IsOut == false) != 0) continue;
                        //if (method.CustomAttributes.Count(i => i.NamedArguments.Count == 2 &&
                        //                                        i.NamedArguments[0].Value.ToString().Contains("Encrypt") &&
                        //                                        i.NamedArguments[1].Name.Contains("Exclude") && i.NamedArguments[1].Value
                        //                                        .ToString().ToLower().Contains("true")) != 0) continue;
                        //Console.WriteLine("Method2: " + method.FullName);

                        if (method.IsPinvokeImpl) continue;
                        if (method.IsUnmanagedExport) continue;
                        if (methods.Contains(method)) continue;

                        methods.Add(method);

                        if (Protect)
                        {
                            new HideCallString(module).Execute(type, method);
                            //new HideCallNumber(module).Execute(type, method);
                        }
                    }
                }

                methods.Distinct();

                Console.WriteLine("Methods: " + methods.Count);

                new EXGuardTask().Exceute(module, methods, Tempoutput, RuntimeVM_Name, "", "");

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
