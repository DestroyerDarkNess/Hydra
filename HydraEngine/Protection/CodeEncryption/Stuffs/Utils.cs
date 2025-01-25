using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HydraEngine.Protection.CodeEncryption.Stuffs
{
    public static class Utils
    {
        static readonly char[] hexCharset = "0123456789abcdef".ToCharArray();
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defValue = default(TValue))
        {
            TValue ret;
            if (dictionary.TryGetValue(key, out ret))
                return ret;
            return defValue;
        }
        public static TValue GetValueOrDefaultLazy<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> defValueFactory)
        {
            TValue ret;
            if (dictionary.TryGetValue(key, out ret))
                return ret;
            return defValueFactory(key);
        }
        public static void AddListEntry<TKey, TValue>(this IDictionary<TKey, List<TValue>> self, TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            List<TValue> list;
            if (!self.TryGetValue(key, out list))
                list = self[key] = new List<TValue>();
            list.Add(value);
        }
        public static string GetRelativePath(string fileSpec, string baseDirectory)
        {
            if (fileSpec is null) throw new ArgumentNullException(nameof(fileSpec));
            if (baseDirectory is null) throw new ArgumentNullException(nameof(fileSpec));

            return GetRelativePath(new FileInfo(fileSpec), new DirectoryInfo(baseDirectory));
        }

        public static string GetRelativePath(FileInfo fileSpec, DirectoryInfo baseDirectory)
        {
            if (fileSpec is null) throw new ArgumentNullException(nameof(fileSpec));
            if (baseDirectory is null) throw new ArgumentNullException(nameof(fileSpec));

            if (baseDirectory.FullName.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                baseDirectory = new DirectoryInfo(baseDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar));
            }

            var relativePath = fileSpec.Name;
            var currentDirectory = fileSpec.Directory;
            while (!(currentDirectory is null) && !string.Equals(currentDirectory.FullName, baseDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = currentDirectory.Name + Path.DirectorySeparatorChar + relativePath;
                currentDirectory = currentDirectory.Parent;
            }

            if (currentDirectory is null) return null; //file is not inside the base directory
            return relativePath;
        }
        public static string NullIfEmpty(this string val)
        {
            if (string.IsNullOrEmpty(val))
                return null;
            return val;
        }
        public static byte[] SHA1(byte[] buffer)
        {
            var sha = new SHA1Managed();
            return sha.ComputeHash(buffer);
        }
        public static byte[] Xor(byte[] buffer1, byte[] buffer2)
        {
            if (buffer1.Length != buffer2.Length)
                throw new ArgumentException("Length mismatched.");
            var ret = new byte[buffer1.Length];
            for (int i = 0; i < ret.Length; i++)
                ret[i] = (byte)(buffer1[i] ^ buffer2[i]);
            return ret;
        }
        public static byte[] SHA256(byte[] buffer)
        {
            var sha = new SHA256Managed();
            return sha.ComputeHash(buffer);
        }
        public static string EncodeString(byte[] buff, char[] charset)
        {
            int current = buff[0];
            var ret = new StringBuilder();
            for (int i = 1; i < buff.Length; i++)
            {
                current = (current << 8) + buff[i];
                while (current >= charset.Length)
                {
                    current = Math.DivRem(current, charset.Length, out int remainder);
                    ret.Append(charset[remainder]);
                }
            }
            if (current != 0)
                ret.Append(charset[current % charset.Length]);
            return ret.ToString();
        }
        public static string ToHexString(byte[] buff)
        {
            var ret = new char[buff.Length * 2];
            int i = 0;
            foreach (byte val in buff)
            {
                ret[i++] = hexCharset[val >> 4];
                ret[i++] = hexCharset[val & 0xf];
            }
            return new string(ret);
        }
        public static void RemoveWhere<T>(this IList<T> self, Predicate<T> match)
        {
            if (self is List<T> list)
            {
                list.RemoveAll(match);
                return;
            }

            // Switch to slow algorithm
            for (int i = self.Count - 1; i >= 0; i--)
            {
                if (match(self[i]))
                    self.RemoveAt(i);
            }
        }
    }
}
