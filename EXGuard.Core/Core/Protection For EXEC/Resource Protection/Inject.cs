using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.Helpers;
using EXGuard.Core.Services;
using EXGuard.Core.RT.Mutation;

namespace EXGuard.Core.EXECProtections
{
    public static class ResourceProt_Inject
    {
        public static IList<MethodDef> Execute(ModuleDef module)
        {
            var typeDef = ModuleDefMD.Load(typeof(ResourceProt_Runtime).Module).ResolveTypeDef(MDToken.ToRID(typeof(ResourceProt_Runtime).MetadataToken));
            var members = Helpers.Injection.InjectHelper.Inject(typeDef, module.GlobalType, module);
            var init = members.OfType<MethodDef>().Single(method => method.Name == "Initialize");

            var Module_ctor = module.GlobalType.FindOrCreateStaticConstructor();

            Module_ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));

            #region MDPhase
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            string asmName = new RandomGenerator().NextHexString(true);
            var assembly = new AssemblyDefUser(asmName, new Version(0, 0));
            assembly.Modules.Add(new ModuleDefUser(asmName + ".dll"));
            ModuleDef dlmodule = assembly.ManifestModule;
            assembly.ManifestModule.Kind = ModuleKind.Dll;
            var asmRef = new AssemblyRefUser(dlmodule.Assembly);

            for (int i = 0; i < module.Resources.Count; i++)
                if (module.Resources[i] is EmbeddedResource)
                {
                    module.Resources[i].Attributes = ManifestResourceAttributes.Public;
                    dlmodule.Resources.Add((module.Resources[i] as EmbeddedResource));

                    module.Resources.Add(new AssemblyLinkedResource(module.Resources[i].Name, asmRef, module.Resources[i].Attributes));
                    module.Resources.RemoveAt(i);

                    i--;
                }

            int key0 = new RandomGenerator().NextInt32();
            byte[] moduleBuff;
            using (var ms = new MemoryStream())
            {
                var options = new ModuleWriterOptions(dlmodule);
                options.Cor20HeaderOptions.Flags = dnlib.DotNet.MD.ComImageFlags.ILOnly;
                options.ModuleKind = ModuleKind.Dll;

                dlmodule.Write(ms, options);

                var compressed = new CompressionService().GZIP_Compress(ms.ToArray()); // compress
                moduleBuff = Encrypt(compressed, key0); // encrypt
            }

            int resID = new RandomGenerator().NextInt32();
            string heapName = Encoding.BigEndianUnicode.GetString(SHA1.Create().ComputeHash(BitConverter.GetBytes(resID))).ToHexString().Substring(0, 8);
            module.Resources.Add(new EmbeddedResource(heapName, moduleBuff, ManifestResourceAttributes.Private));

            // Inject Resource Name Key
            MutationHelperCore.InjectKey_Int(init, 0, resID);

            // Inject Resource Encryption Key
            MutationHelperCore.InjectKey_Int(init, 1, key0);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            var methods = new HashSet<MethodDef>();
            methods.Add(init);

            #region Rename Merged Methods
            ///////////////////////////////////////////////////////////////////////
            foreach (IDnlibDef def in members)
            {
                IMemberDef memberDef = def as IMemberDef;

                if ((memberDef as MethodDef) != null)
                    memberDef.Name = new NameService().NewName(memberDef.Name);
                else if ((memberDef as FieldDef) != null)
                    memberDef.Name = new NameService().NewName(memberDef.Name);
            }
            ///////////////////////////////////////////////////////////////////////
            #endregion

            return methods.ToList();
        }

        private static byte[] Encrypt(byte[] plainBytes, int key0)
        {
            var aes = Rijndael.Create();

            aes.Key = SHA256.Create().ComputeHash(BitConverter.GetBytes(key0));
            aes.IV = new byte[16];

            aes.Mode = CipherMode.CBC;

            var memoryStream = new MemoryStream();
            var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();

            byte[] cipherBytes = memoryStream.ToArray();

            memoryStream.Close();
            cryptoStream.Close();

            return cipherBytes;
        }
    }
}
