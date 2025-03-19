//using AsmResolver.DotNet;
//using ILMerging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace HydraEngine.References
//{
//    public class LibzWrapper
//    {

//        public Exception Errors { get; set; } = new Exception("Undefined");

//        public bool MergeAssemblies(string Original, List<string> dllModules)
//        {
//            try
//            {

//                List<string> ListArg = new List<string>();

//                ListArg.Add("inject-dll");
//                ListArg.Add("-a");
//                ListArg.Add(Original);
//                ListArg.Add("-i");


//                foreach (string str in dllModules)
//                {
//                    System.IO.File.Copy(str, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Original), System.IO.Path.GetFileName(str)), true);
//                }

//                ListArg.Add("*.dll");
//                ListArg.Add("-e");
//                ListArg.Add(Original);
//                int Result =  libz.Load.Main(ListArg.ToArray());
//                return true;

//            }
//            catch (Exception ex)
//            {
//                Errors = ex;
//                return false;
//            }
//        }


//    }
//}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace HydraEngine.References
{
    public class LibzWrapper
    {
        public Exception Errors { get; set; } = new Exception("Undefined");

        public bool MergeAssemblies(string original, List<string> dllModules)
        {
            try
            {
                //// Crear un nuevo AppDomain
                //AppDomain domain = AppDomain.CreateDomain("LibZDomain");

                //try
                //{
                //    // Crear una instancia del proxy en el AppDomain secundario
                //    var proxyType = typeof(LibZExecutor);
                //    ObjectHandle executor = domain.CreateInstance(proxyType.Assembly.FullName, proxyType.FullName);
                //    executor.InitializeLifetimeService();
                //    var exe = (LibZExecutor)executor.Unwrap();
                //    // Ejecutar LibZ en el nuevo AppDomain
                //    bool result = exe.ExecuteLibZ(original, dllModules);
                //    return result;
                //}
                //finally
                //{
                //    // Descargar el AppDomain para liberar el ensamblado
                //    AppDomain.Unload(domain);
                //    Console.WriteLine("El AppDomain se ha descargado y el ensamblado se ha liberado.");
                //}

                string BaseDir = Path.GetDirectoryName(original);
                string Libz = Path.Combine(BaseDir, Path.GetFileName("libz.exe"));
                List<string> listArg = new List<string>
                {
                    "inject-dll",
                    "-a",
                    original,
                    "-i"
                };

                foreach (string str in dllModules)
                {
                    string destination = Path.Combine(BaseDir, Path.GetFileName(str));
                    File.Copy(str, destination, true);
                }

                listArg.Add("*.dll");
                listArg.Add("-e");
                listArg.Add(original);

                if (!File.Exists(Libz)) File.WriteAllBytes(Libz, HydraEngine.Properties.Resources.libz);

                string LibzResult = Core.Utils.RunRemoteHost(Libz, String.Join(" ", listArg.ToArray()), false);
                Console.WriteLine(LibzResult.Replace("LibZ 1.2.0.0, Copyright (c) 2013-2014, Milosz Krajewski", "Hydra LibZ Modded version, https://github.com/DestroyerDarkNess").Replace("https://libz.codeplex.com/", "HAIL HYDRA").Replace(original, "****").Replace("LibZ.", "Hydra."));

                return true;
            }
            catch (Exception ex)
            {
                Errors = ex;
                return false;
            }
        }
    }

    public class LibZExecutor : MarshalByRefObject
    {
        public bool ExecuteLibZ(string original, List<string> dllModules)
        {
            try
            {
                // Añadir el directorio del ensamblado original al path de búsqueda del AppDomain
                string assemblyDir = Path.GetDirectoryName(original);
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    string assemblyPath = Path.Combine(assemblyDir, new AssemblyName(args.Name).Name + ".dll");
                    return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
                };

                List<string> listArg = new List<string>
                {
                    "inject-dll",
                    "-a",
                    original,
                    "-i"
                };

                foreach (string str in dllModules)
                {
                    string destination = Path.Combine(Path.GetDirectoryName(original), Path.GetFileName(str));
                    File.Copy(str, destination, true);
                }

                listArg.Add("*.dll");
                listArg.Add("-e");
                listArg.Add(original);

                // Redirigir salida y errores a archivos para depuración
                string outputFile = Path.Combine(Path.GetTempPath(), "libz_output.log");
                string errorFile = Path.Combine(Path.GetTempPath(), "libz_error.log");

                using (StreamWriter outputWriter = new StreamWriter(outputFile))
                using (StreamWriter errorWriter = new StreamWriter(errorFile))
                {
                    // Redirigir la salida estándar y los errores
                    Console.SetOut(outputWriter);
                    Console.SetError(errorWriter);

                    // Llamar a la función Main de LibZ
                    int result = 0; // libz.Load.Main(listArg.ToArray());

                    // Asegúrate de que los mensajes se escriban en los archivos
                    outputWriter.Flush();
                    errorWriter.Flush();

                    Console.WriteLine($"LibZ terminó con el código de resultado: {result}");
                    Console.WriteLine($"Verifica el archivo {outputFile} para la salida y {errorFile} para errores.");

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    return result == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en ExecuteLibZ: " + ex.Message);
                return false;
            }
        }
    }
}
