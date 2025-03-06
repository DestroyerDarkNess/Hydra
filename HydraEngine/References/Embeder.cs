using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine._7zip;
using HydraEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace HydraEngine.References
{
    public class Embeder
    {
        public bool MergeAssemblies(ModuleDefMD Module, List<string> dlls)
        {
            try
            {
                if (dlls.Count == 0)
                {
                    return false;
                }
                newInjector injector = new newInjector(Module, typeof(embedRuntime));
                MethodDef method = injector.FindMember("AppStart") as MethodDef;
                MethodDef methodDef = Module.GlobalType.FindOrCreateStaticConstructor();
                methodDef.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, method));
                foreach (string dll in dlls)
                {
                    byte[] data = File.ReadAllBytes(dll);
                    Module.Resources.Add(new EmbeddedResource(Path.GetFileNameWithoutExtension(dll), QuickLZ.CompressBytes2(data)));
                }
                injector.Rename();

                return true;
            }
            catch { return false; }

        }
    }

    public static class embedRuntime
    {
        private static void AppStart()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;
        }

        private static Assembly ResolveAssemblies(object sender, ResolveEventArgs args)
        {
            try
            {
                string text = (args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", ""));
                if (text.EndsWith("_resources"))
                {
                    return null;
                }

                byte[] array = null;

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(text))
                {
                    array = new byte[stream.Length];
                    stream.Read(array, 0, array.Length);
                }

                if (array != null)
                {
                    return Assembly.Load(Decompress(array));
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] Decompress(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            MemoryStream memoryStream = new MemoryStream();
            using (DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(memoryStream);
            }
            return memoryStream.ToArray();
        }
    }
}
