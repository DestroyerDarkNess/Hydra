using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.JIT
{
    public static class Crypto
    {
        public static byte[] Encrypt(byte[] data, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128;
                aes.BlockSize = 128;
                aes.Padding = PaddingMode.Zeros;
                aes.Key = key;
                aes.IV = new byte[16] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    return PerformCryptography(data, encryptor);
                }
            }
        }
        private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using (var ms = new MemoryStream())
            using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
            {
                cryptoStream.Write(data, 0, data.Length);
                cryptoStream.FlushFinalBlock();

                return ms.ToArray();
            }
        }
    }
    public static class Utils
    {

        internal static byte[] GetOriginalRawILBytes(this ModuleDefMD module, MethodDef methodDef)
        {
            var reader = module.Metadata.PEImage.CreateReader(methodDef.RVA);
            byte b = reader.ReadByte();
            uint codeSize = 0;
            switch (b & 7)
            {
                case 2:
                case 6:
                    codeSize = (uint)(b >> 2);
                    break;
                case 3:
                    var flags = (ushort)((reader.ReadByte() << 8) | b);
                    var headerSize = (byte)(flags >> 12);

                    reader.ReadUInt16();
                    codeSize = reader.ReadUInt32();
                    reader.ReadUInt32();

                    reader.Position = reader.Position - 12 + headerSize * 4U;
                    break;
            }
            byte[] ilBytes = new byte[codeSize];
            reader.ReadBytes(ilBytes, 0, ilBytes.Length);
            return ilBytes;
        }
        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                || candidate == null
                || array.Length == 0
                || candidate.Length == 0
                || candidate.Length > array.Length;
        }
        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;
            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;
            return true;
        }
        internal static int Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return -1;
            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;
                return i;
            }
            return -1;
        }

    }
}
