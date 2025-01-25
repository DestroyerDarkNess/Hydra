using AsmResolver.DotNet.Signatures.Types;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.PE;
using HydraEngine.Properties;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HydraEngine.Protection.Packer
{
   
    public class PEPacker : Models.Pack
    {
        public PEPacker() : base("Protection.Pack.PEPacker", "Renamer Phase", "Description for Renamer Phase") { }
      

        public override async Task<bool> Execute(ModuleDefMD module, string Ouput)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        private const int PEHeaderWithExtraByteHex = 0x00014550;

        public static string GenerateCodeFunction(string largeString)
        {
            // Dividir el string en partes más pequeñas
            int chunkSize = 30; // Tamaño de cada parte
            int length = largeString.Length;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("public static string Code()");
            sb.AppendLine("{");
            sb.AppendLine("    StringBuilder sb = new StringBuilder();");

            for (int i = 0; i < length; i += chunkSize)
            {
                int size = Math.Min(chunkSize, length - i);
                string chunk = largeString.Substring(i, size);
                sb.AppendLine($"    sb.Append(\"{chunk}\");");
            }

            sb.AppendLine("    return sb.ToString();");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public override async Task<bool> Execute(string filePath, string Ouput)
        {
            try
            {
                var Options = new Dictionary<string, string>();
                Options.Add("CompilerVersion", "v4.0");
                Options.Add("language", "c#");
                var codeProvider = new CSharpCodeProvider(Options);
                CompilerParameters parameters = new CompilerParameters();
                parameters.CompilerOptions = "/target:winexe /unsafe";
                parameters.GenerateExecutable = true;
                parameters.OutputAssembly = Ouput;
                parameters.IncludeDebugInformation = false;
                parameters.TreatWarningsAsErrors = false;
                string[] Librarys = { "System", "System.Windows.Forms", "System.Management", "System.Net", "System.Core", "System.Net.Http", "System.Runtime", "System.Runtime.InteropServices" };
                foreach (string Library in Librarys)
                {
                    parameters.ReferencedAssemblies.Add(Library + ".dll");
                }
                byte[] CodeToProtect = File.ReadAllBytes(filePath);
                string RandomIV = RandomName(16);
                string RandomKey = RandomPassword(17);
                string RandomXORKey = RandomPassword(4);
                string EncryptedKey = XOREncryptionKeys(RandomKey, RandomXORKey);
                string EncryptedIV = XOREncryptionKeys(RandomIV, RandomXORKey);
                string Final = AesTextEncryption(Convert.ToBase64String(CodeToProtect).Replace("A", ".").Replace("B", "*").Replace("S", @"_"), EncryptedKey, EncryptedIV);
                string PackStub = Properties.Resources.PackCode;
                string CodeFunc = GenerateCodeFunction(Final);
          
                string NewPackStub = PackStub.Replace("//CoFunc", CodeFunc).Replace("THISISIV", RandomIV).Replace("THISISKEY", RandomKey);
                string TotallyNewPackStub = NewPackStub.Replace("decryptkeyencryption", Convert.ToBase64String(Encoding.UTF8.GetBytes(RandomXORKey))).Replace("decryptkeyiv", Convert.ToBase64String(Encoding.UTF8.GetBytes(RandomXORKey))).Replace("PackStub", "namespace " + RandomName(12));
                CompilerResults cr = codeProvider.CompileAssemblyFromSource(parameters, TotallyNewPackStub);

                if (cr.Errors.Count > 0)
                {
                    string ErrorList = string.Empty;
                    foreach (CompilerError ce in cr.Errors)
                    {
                        ErrorList += "Errors building: " + ce.ErrorText + ", in line: " + ce.Line + Environment.NewLine;
                    }
                    throw new Exception(ErrorList);
                }



                if (filePath != Ouput)
                System.IO.File.Copy(filePath, Ouput, true);
                return true;
        }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
}

        private string AesTextEncryption(string DataToEncrypt, string KeyToEncryptWith, string IVKey)
        {
            byte[] data = UTF8Encoding.UTF8.GetBytes(DataToEncrypt);
            using (SHA256CryptoServiceProvider SHA256 = new SHA256CryptoServiceProvider())
            {
                string initVector = IVKey;
                byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
                byte[] keys = SHA256.ComputeHash(UTF8Encoding.UTF8.GetBytes(KeyToEncryptWith));
                using (AesCryptoServiceProvider AES = new AesCryptoServiceProvider() { Key = keys, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
                {
                    AES.IV = initVectorBytes;
                    ICryptoTransform transform = AES.CreateEncryptor();
                    byte[] results = transform.TransformFinalBlock(data, 0, data.Length);
                    string Result = Convert.ToBase64String(results, 0, results.Length);
                    return Result;
                }
            }
        }

        private string XOREncryptionKeys(string KeyToEncrypt, string Key)
        {
            StringBuilder DecryptEncryptionKey = new StringBuilder();
            for (int c = 0; c < KeyToEncrypt.Length; c++)
                DecryptEncryptionKey.Append((char)((uint)KeyToEncrypt[c] ^ (uint)Key[c % 4]));
            return DecryptEncryptionKey.ToString();
        }

        private string RandomPassword(int PasswordLength)
        {
            StringBuilder MakePassword = new StringBuilder();
            Random MakeRandom = new Random();
            while (0 < PasswordLength--)
            {
                string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ*!@=&?&/abcdefghijklmnopqrstuvwxyz1234567890";
                MakePassword.Append(characters[MakeRandom.Next(characters.Length)]);
            }
            return MakePassword.ToString();
        }

        private string RandomName(int NameLength)
        {
            StringBuilder MakePassword = new StringBuilder();
            Random MakeRandom = new Random();
            while (0 < NameLength--)
            {
                string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                MakePassword.Append(characters[MakeRandom.Next(characters.Length)]);
            }
            return MakePassword.ToString();
        }

    }
}
