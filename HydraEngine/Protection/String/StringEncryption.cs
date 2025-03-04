using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Core;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.String
{
    public class StringEncryption : Models.Protection
    {
        private static byte[] _encryptionKey;
        private static readonly RandomNumberGenerator csp = RandomNumberGenerator.Create();

        public StringEncryption()
            : base("Protection.String.StringEncryption", "String Encryption", "XOR + Clave en Recursos") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                _encryptionKey = GenerateRandomKey();
                StoreKeyInResources(module, _encryptionKey);

                var decryptMethod = InjectMethod(module, "Decrypt");



                foreach (TypeDef type in module.Types.Where(t => !t.IsGlobalModuleType))
                {
                    foreach (MethodDef method in type.Methods.Where(m => m.HasBody))
                    {
                        EncryptStringsInMethod(method, decryptMethod);
                    }
                }

                var decryptMethod2 = InjectMethod2(module, "Decrypt2");

                foreach (TypeDef type in module.Types.Where(t => !t.IsGlobalModuleType))
                {
                    foreach (MethodDef method in type.Methods.Where(m => m.HasBody))
                    {
                        EncryptStringsInMethod2(method, decryptMethod2);
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

        private static byte[] GenerateRandomKey()
        {
            // Generar clave de 32 bytes usando RNG criptográfico
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        private void StoreKeyInResources(ModuleDefMD module, byte[] key)
        {
            byte[] obfuscatedKey = new byte[key.Length];
            for (int i = 0; i < key.Length; i++)
                obfuscatedKey[i] = (byte)(key[i] ^ 0xAA); // XOR con 0xAA

            // Crear recurso embebido
            var resource = new EmbeddedResource("HailHydra", obfuscatedKey);
            module.Resources.Add(resource);
        }

        private void EncryptStringsInMethod(MethodDef method, MethodDef decryptMethod)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                {
                    string original = method.Body.Instructions[i].Operand.ToString();
                    string encrypted = EncryptXor(original, _encryptionKey);

                    method.Body.Instructions[i].Operand = encrypted;
                    method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Call, decryptMethod));
                    i++;
                }
            }
        }

        private void EncryptStringsInMethod2(MethodDef method, MethodDef decryptMethod)
        {
            int Amount = 0;

            method.Body.SimplifyBranches();

            for (int i = 0; i < method.Body.Instructions.Count; i++)
                if (method.Body.Instructions[i] != null && method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                {
                    int key = Next();
                    object op = method.Body.Instructions[i].Operand;

                    if (op == null)
                        continue;

                    method.Body.Instructions[i].Operand = Encrypt(op.ToString(), key);
                    method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(Next()));
                    method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(key));
                    method.Body.Instructions.Insert(i + 3, OpCodes.Ldc_I4.ToInstruction(Next()));
                    method.Body.Instructions.Insert(i + 4, OpCodes.Ldc_I4.ToInstruction(Next()));
                    method.Body.Instructions.Insert(i + 5, OpCodes.Ldc_I4.ToInstruction(Next()));
                    method.Body.Instructions.Insert(i + 6, OpCodes.Call.ToInstruction(decryptMethod));

                    ++Amount;
                }

            method.Body.OptimizeBranches();
        }

        private static string EncryptXor(string input, byte[] key)
        {
            byte[] data = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < data.Length; i++)
                data[i] ^= key[i % key.Length];

            return Convert.ToBase64String(data);
        }

        public static string Encrypt(string str, int key)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in str.ToCharArray())
                builder.Append((char)(c + key));

            return builder.ToString();
        }

        private MethodDef InjectMethod(ModuleDef module, string methodName)
        {


            var decryptionHelper = ModuleDefMD.Load(typeof(DecrypStr).Module);
            var typeDef = decryptionHelper.ResolveTypeDef(MDToken.ToRID(typeof(DecrypStr).MetadataToken));
            var injectedMembers = InjectHelper.Inject(typeDef, module.GlobalType, module);

            var decryptMethod = injectedMembers.OfType<MethodDef>().First(m => m.Name == methodName);

            if (decryptMethod != null) decryptMethod.Name = Randomizer.GenerateRandomString(22);

            var GetKeyMethod = injectedMembers.OfType<MethodDef>().First(m => m.Name == "GetKeyFromResources");

            if (GetKeyMethod != null) GetKeyMethod.Name = Randomizer.GenerateRandomString(20);

            //var cctor = module.GlobalType.FindStaticConstructor();
            //if (cctor != null) module.GlobalType.Remove(cctor);

            return decryptMethod;
        }

        private MethodDef InjectMethod2(ModuleDef module, string methodName)
        {
            var decryptionHelper = ModuleDefMD.Load(typeof(DecrypStr2).Module);
            var typeDef = decryptionHelper.ResolveTypeDef(MDToken.ToRID(typeof(DecrypStr2).MetadataToken));
            var injectedMembers = InjectHelper.Inject(typeDef, module.GlobalType, module);

            var decryptMethod = injectedMembers.OfType<MethodDef>().First(m => m.Name == methodName);

            if (decryptMethod != null) decryptMethod.Name = Randomizer.GenerateRandomString(22);

            //var cctor = module.GlobalType.FindStaticConstructor();
            //if (cctor != null) module.GlobalType.Remove(cctor);

            return decryptMethod;
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }

        public static int Next()
        {
            return BitConverter.ToInt32(RandomBytes(sizeof(int)), 0);
        }

        private static byte[] RandomBytes(int bytes)
        {
            byte[] buffer = new byte[bytes];
            csp.GetBytes(buffer);
            return buffer;
        }
    }

    internal static class DecrypStr
    {
        public static string Decrypt(string encryptedData)
        {
            try
            {
                // 1. Obtener clave de los recursos
                byte[] key = GetKeyFromResources();

                if (key == null) { return "Failed"; }

                // 2. Descifrar string
                byte[] data = Convert.FromBase64String(encryptedData);
                for (int i = 0; i < data.Length; i++)
                    data[i] ^= key[i % key.Length];

                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return string.Empty;
            }
        }


        private static byte[] GetKeyFromResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                byte[] encryptedKey = new byte[32];
                using (var stream = assembly.GetManifestResourceStream("HailHydra"))
                {
                    stream.Read(encryptedKey, 0, 32);
                }

                byte[] realKey = new byte[32];
                for (int i = 0; i < encryptedKey.Length; i++)
                    realKey[i] = (byte)(encryptedKey[i] ^ 0xAA);

                return realKey;
            }
            catch { return null; }

        }
    }

    internal static class DecrypStr2
    {
        public static string Decrypt2(string str, int min, int key, int hash, int length, int max)
        {
            if (max > 78787878) ;
            if (length > 485941) ;

            StringBuilder builder = new StringBuilder();
            foreach (char c in str.ToCharArray())
                builder.Append((char)(c - key));

            if (min < 14141) ;
            if (length < 1548174) ;

            return builder.ToString();
        }

    }
}
