using dnlib.DotNet;
using dnlib.PE;
using Ressy;
using Ressy.HighLevel.Icons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Core
{
    public class DLLInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
    }

    public class AssemblyMap
    {

        public  MethodDef EntryPoint { get; set; }  = null;
        public ModuleKind Kind { get; set; } = ModuleKind.Console;
        public Characteristics Characteristics { get; set; } =  Characteristics.ExecutableImage;
        public Machine Machine { get; set; } = Machine.I386;
        public bool Is32BitPreferred { get; set; } = true;

        public bool Update(ModuleDef module)
        {
            try {
                this.EntryPoint = module.EntryPoint;
                this.Kind = module.Kind;
                this.Characteristics = module.Characteristics;
                this.Machine = module.Machine;
                this.Is32BitPreferred = module.Is32BitPreferred;
                return true;
            } catch { return false; }
        }
    }
    public class Utils
    {
        public static void ConvertToIco(Image img, string file, int size)
        {
            Icon icon;
            using (var msImg = new MemoryStream())
            using (var msIco = new MemoryStream())
            {
                img.Save(msImg, ImageFormat.Png);
                using (var bw = new BinaryWriter(msIco))
                {
                    bw.Write((short)0);           //0-1 reserved
                    bw.Write((short)1);           //2-3 image type, 1 = icon, 2 = cursor
                    bw.Write((short)1);           //4-5 number of images
                    bw.Write((byte)size);         //6 image width
                    bw.Write((byte)size);         //7 image height
                    bw.Write((byte)0);            //8 number of colors
                    bw.Write((byte)0);            //9 reserved
                    bw.Write((short)0);           //10-11 color planes
                    bw.Write((short)32);          //12-13 bits per pixel
                    bw.Write((int)msImg.Length);  //14-17 size of image data
                    bw.Write(22);                 //18-21 offset of image data
                    bw.Write(msImg.ToArray());    // write image data
                    bw.Flush();
                    bw.Seek(0, SeekOrigin.Begin);
                    icon = new Icon(msIco);
                }
            }
            using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                icon.Save(fs);
            }
        }

        public static bool ChangeIcon(string AppPath , string IconPath) {
            try {
                if (File.Exists(AppPath) || File.Exists(IconPath))
                {
                    var portableExecutable = new PortableExecutable(AppPath);
                    portableExecutable.RemoveIcon();
                    portableExecutable.SetIcon(IconPath);
                    return true;
                }
                return false;
            } catch { return false; }
        }

        public static string RunRemoteHost(string Target, string FullArguments = "", bool redirectouput = true)
        {
            try
            {

                Process cmdProcess = new Process();
                {
                    var withBlock = cmdProcess;
                    withBlock.StartInfo = new ProcessStartInfo(Target, FullArguments);
                    {
                        var withBlock1 = withBlock.StartInfo;
                        if (redirectouput)
                        {
                            withBlock1.CreateNoWindow = true;
                            withBlock1.UseShellExecute = false;
                            withBlock1.RedirectStandardOutput = true;
                            withBlock1.RedirectStandardError = true;
                        }
                        withBlock1.WindowStyle = ProcessWindowStyle.Hidden;
                        withBlock1.WorkingDirectory = Path.GetDirectoryName(Target);
                    }
                    withBlock.Start();
                    withBlock.WaitForExit();
                }

                if (redirectouput)
                {
                    string HostOutput = cmdProcess.StandardOutput.ReadToEnd().ToString() + Environment.NewLine + cmdProcess.StandardError.ReadToEnd().ToString();
                    return HostOutput.ToString();
                } else { return ""; }
               
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static System.Random rnd = new System.Random();
        public static int RandomTinyInt32() => rnd.Next(2, 25);
        public static int RandomSmallInt32() => rnd.Next(15, 40);
        public static int RandomInt32() => rnd.Next(100, 300);
        public static int RandomInt322() => rnd.Next(10000, 100000);
        public static int RandomBigInt32() => rnd.Next();
        public static bool RandomBoolean() => Convert.ToBoolean(rnd.Next(0, 2));

        public static string ComputeAssemblyHash(string assemblyPath)
        {
            using (var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public static bool IsAssemblyAltered(string assemblyPath, string originalHash)
        {
            var computedHash = ComputeAssemblyHash(assemblyPath);
            return !string.Equals(computedHash, originalHash, StringComparison.OrdinalIgnoreCase);
        }

        public static bool VerifyAssemblySignature(string assemblyPath)
        {
            try
            {
                var certificate = new X509Certificate2(assemblyPath);
                var publicKey = certificate.PublicKey.Key;

                using (var rsa = publicKey as RSA)
                {
                    var data = System.IO.File.ReadAllBytes(assemblyPath);
                    using (var sha256 = SHA256.Create())
                    {
                        var hash = sha256.ComputeHash(data);

                        var signature = certificate.GetPublicKey();
                        return rsa.VerifyData(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying signature: {ex.Message}");
                return false;
            }
        }

        public static List<DLLInfo> GetUniqueLibsToMerged(ModuleDef ASM, string WorkingDir)
        {
            var allLibs = GetLibsToMergedRecursive(ASM, WorkingDir);
            return allLibs
                .GroupBy(dll => dll.Path)
                .Select(g => g.First())
                .ToList();
        }

        public static List<DLLInfo> GetLibsToMergedRecursive(ModuleDef ASM, string WorkingDir)
        {
            List<DLLInfo> result = new List<DLLInfo>();

            var assemblyRefs = ASM.GetAssemblyRefs()
                                  .Select(modEx => Path.Combine(WorkingDir, modEx.Name + ".dll"))
                                  .Where(File.Exists);

            foreach (var relativePath in assemblyRefs)
            {
                try
                {
                    using (var module = ModuleDefMD.Load(relativePath))
                    {
                        if (module.IsILOnly)
                        {
                            result.Add(new DLLInfo { Path = relativePath, Info = module.Assembly.FullName });
                            result.AddRange(GetLibsToMergedRecursive(module, WorkingDir));
                        }
                    }
                }
                catch {}
            }

            return result;
        }

        public static ModuleDefMD LoadModule(byte[] data, out AssemblyResolver assemblyResolver)
        {
            try
            {
                assemblyResolver = new AssemblyResolver();
                var context = new ModuleContext(assemblyResolver);
                assemblyResolver.EnableTypeDefCache = false;
                assemblyResolver.DefaultModuleContext = context;
                var options = new ModuleCreationOptions()
                {
                    Context = context,
                    TryToLoadPdbFromDisk = false
                };

                return ModuleDefMD.Load(data, options);
            } catch { assemblyResolver = null; return null; }
          
        }


        //public static ModuleDefMD LoadModule(byte[] data, out AssemblyResolver assemblyResolver, out AssemblyMap assemblyMap)
        //{
        //    assemblyResolver = new AssemblyResolver();
        //    var context = new ModuleContext(assemblyResolver);
        //    assemblyResolver.EnableTypeDefCache = false;
        //    assemblyResolver.DefaultModuleContext = context;
        //    var options = new ModuleCreationOptions()
        //    {
        //        Context = context,
        //        TryToLoadPdbFromDisk = false
        //    };
        //    ModuleDefMD module = ModuleDefMD.Load(data, options);
        //    assemblyMap = new AssemblyMap();
        //    assemblyMap.Update(module);

        //    return module;
        //}

        //public static ModuleDefMD LoadModule(string FilePath, out AssemblyResolver assemblyResolver)
        //{
        //    assemblyResolver = new AssemblyResolver();
        //    var context = new ModuleContext(assemblyResolver);
        //    assemblyResolver.EnableTypeDefCache = false;
        //    assemblyResolver.DefaultModuleContext = context;
        //    var options = new ModuleCreationOptions()
        //    {
        //        Context = context,
        //        TryToLoadPdbFromDisk = false
        //    };
        //    return ModuleDefMD.Load(FilePath, options);
        //}

    }
}
