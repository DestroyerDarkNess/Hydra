using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.PE;

namespace HydraEngine.References
{
    public class ILRepack
    {
        public Exception Errors { get; set; } = new Exception("Undefined");

        public bool MergeAssemblies(string original, List<string> dllModules, string outputFile)
        {
            string tempDir = null;
            try
            {
                tempDir = Path.Combine(Path.GetTempPath(), "HydraEngine_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                string tempOriginal = Path.Combine(tempDir, Path.GetFileName(original));
                File.Copy(original, tempOriginal, true);

                string ILRepack = Path.Combine(tempDir, "ILRepack.exe");
                File.WriteAllBytes(ILRepack, HydraEngine.Properties.Resources.ILRepack);

                foreach (string dll in dllModules)
                {
                    string destination = Path.Combine(tempDir, Path.GetFileName(dll));
                    File.Copy(dll, destination, true);
                }

                string tempOutput = Path.Combine(tempDir, Path.GetFileName(outputFile));

                List<string> args = new List<string>
                {
                    "/out:" + tempOutput,
                    tempOriginal
                };

                args.AddRange(Directory.GetFiles(tempDir, "*.dll")
                    .Where(dll => !dll.Equals(tempOriginal, StringComparison.OrdinalIgnoreCase)));

                args.AddRange(new[]
                {
                    $"/target:{GetILRepackTargetType(tempOriginal)}",
                    "/wildcards",
                    "/ndebug",
                    "/copyattrs",
                    "/union",
                    "/parallel"
                });

                string arguments = string.Join(" ", args);
                string MergeResult = Core.Utils.RunRemoteHost(ILRepack, arguments, false);
                Console.WriteLine(MergeResult);

                if (File.Exists(tempOutput))
                {
                    string outputDir = Path.GetDirectoryName(outputFile);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    File.Copy(tempOutput, outputFile, true);
                    return true;
                }
                else
                {
                    Errors = new Exception("El archivo de salida no se generó correctamente.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Errors = ex;
                return false;
            }
            finally
            {
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch { }
                }
            }
        }

        private string GetILRepackTargetType(string assemblyPath)
        {
            try
            {
                using (ModuleDefMD module = ModuleDefMD.Load(assemblyPath))
                {
                    switch (module.Kind)
                    {
                        case ModuleKind.Dll:
                            return "library";

                        case ModuleKind.Windows:
                            return "winexe";

                        default:
                            break;
                    }
                    return "exe";
                }
            }
            catch
            {
                return "exe";
            }
        }
    }
}