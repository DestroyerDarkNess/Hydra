﻿using dnlib.DotNet;
using dnlib.DotNet.Writer;
using HydraEngine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Packer.NetBuilderInjection
{
    public static class AssemblyToShellCode
    {
        private static string Donut = Path.Combine(Path.GetTempPath(), "donut.exe");

        public static byte[] ToShellCode(this ModuleDefMD Module, MethodDef EntryPoint, string appdomainName = "")
        {
            if (!File.Exists(Donut)) File.WriteAllBytes(Donut, HydraEngine.Properties.Resources.donut);

            string TempShell = Path.Combine(Path.GetTempPath(), "loader.b64");
            string TargetAsmName = Path.Combine(Path.GetTempPath(), "tempASMShell.exe");

            if (File.Exists(TargetAsmName)) File.Delete(TargetAsmName);

            Module.Write(TargetAsmName);

            System.Threading.Thread.Sleep(100);

            TypeDef declaringType = EntryPoint.DeclaringType;

            string FullDonutArgs = $"-f 2 -c {declaringType.Namespace + "." + declaringType.Name} -m {EntryPoint.Name} --input:{TargetAsmName}";

            if (appdomainName != "")
            {
                FullDonutArgs += " -d " + appdomainName;
            }

            string DonutResult = Core.Utils.RunRemoteHost(Donut, FullDonutArgs);
            Console.WriteLine("Shell Output: " + DonutResult);

            //Console.WriteLine("Shell Output: " + DonutResult.Replace(TargetAsmName, "******").Replace("Donut", "Hydra").Replace("(built Mar  3 2023 13:33:22)", "").Replace("[ Copyright (c) 2019-2021 TheWover, Odzhan", "[ Github: https://github.com/DestroyerDarkNess"));

            if (File.Exists(TempShell) == true)
            {
                string data = File.ReadAllText(TempShell);

                if (File.Exists(TempShell)) File.Delete(TempShell);

                return Convert.FromBase64String(data);
            }
            else
            {
                return null;
            }
        }

        public static byte[] ToShellCode(this string TargetAssembly, MethodDef EntryPoint, string appdomainName = "")
        {
            if (!File.Exists(Donut)) File.WriteAllBytes(Donut, HydraEngine.Properties.Resources.donut);

            string TempShell = Path.Combine(Path.GetTempPath(), "loader.b64");
            string TargetAsmName = Path.Combine(Path.GetTempPath(), "tempASMShell.dll");

            if (File.Exists(TargetAsmName)) File.Delete(TargetAsmName);

            //try
            //{
            //    ModuleDefMD ModuleDef = ModuleDefMD.Load(TargetAssembly);

            //    if (ModuleDef.Kind == ModuleKind.Dll)
            //    {
            //        //ModuleDef.Kind = ModuleKind.Console;

            //        //var writerOptions = new ModuleWriterOptions(ModuleDef);
            //        //writerOptions.Cor20HeaderOptions.Flags &= ~dnlib.DotNet.MD.ComImageFlags.ILLibrary;

            //        //writerOptions.PEHeadersOptions.Characteristics &= ~dnlib.PE.Characteristics.Dll;
            //        //writerOptions.PEHeadersOptions.Characteristics |= dnlib.PE.Characteristics.ExecutableImage;

            //        ModuleDef.Write(TargetAsmName);
            //    }
            //    else
            //    {
            //        throw new Exception("Module is not a DLL");
            //    }
            //}
            //catch { File.Copy(TargetAssembly, TargetAsmName); }

            File.Copy(TargetAssembly, TargetAsmName);

            System.Threading.Thread.Sleep(100);

            TypeDef declaringType = EntryPoint.DeclaringType;

            string FullDonutArgs = $"-f 2 -c {declaringType.Namespace + "." + declaringType.Name} -m {EntryPoint.Name} --input:{TargetAsmName}";

            if (appdomainName != "")
            {
                FullDonutArgs += " -d " + appdomainName;
            }

            string DonutResult = Core.Utils.RunRemoteHost(Donut, FullDonutArgs);
            //Console.WriteLine("Shell Output: " + DonutResult);

            Console.WriteLine("Shell Output: " + DonutResult.Replace(TargetAsmName, "******").Replace("Donut", "Hydra").Replace("(built Mar  3 2023 13:33:22)", "").Replace("[ Copyright (c) 2019-2021 TheWover, Odzhan", "[ Github: https://github.com/DestroyerDarkNess"));

            if (File.Exists(TempShell) == true)
            {
                string data = File.ReadAllText(TempShell);

                if (File.Exists(TempShell)) File.Delete(TempShell);

                return Convert.FromBase64String(data);
            }
            else
            {
                return null;
            }
        }
    }
}