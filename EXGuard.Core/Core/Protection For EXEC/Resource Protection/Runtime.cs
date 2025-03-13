using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.IO.Compression;
using System.Security.Cryptography;

namespace EXGuard.Core.EXECProtections
{
    internal static class ResourceProt_Runtime
	{
		static Assembly c;

		static void Initialize()
		{
			var sb = new StringBuilder();
			var bytes = Encoding.Unicode.GetBytes(Encoding.BigEndianUnicode.GetString(SHA1.Create().ComputeHash(BitConverter.GetBytes(Mutation.IntKey0))));
			foreach (var t in bytes)
			{
				sb.Append(t.ToString("X2"));
			}

			var str = typeof(ResourceProt_Runtime).Assembly.GetManifestResourceStream(sb.ToString().Substring(0, 8));
			byte[] dat = new byte[str.Length];
			str.Read(dat, 0, dat.Length);

			var aes = Rijndael.Create();

			aes.Key = SHA256.Create().ComputeHash(BitConverter.GetBytes(Mutation.IntKey1));
			aes.IV = new byte[16];
			aes.Mode = CipherMode.CBC;

			var memoryStream = new MemoryStream();
			var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);

			cryptoStream.Write(dat, 0, dat.Length);
			cryptoStream.FlushFinalBlock();

			dat = memoryStream.ToArray();

			memoryStream.Close();
			cryptoStream.Close();

			var decompressedMs = new MemoryStream();
			using (var gzs = new GZipStream(new MemoryStream(dat), CompressionMode.Decompress))
			{
				int bufSize = 1024, count;
				var bytex = new byte[bufSize];
				count = gzs.Read(bytex, 0, bufSize);
				while (count > 0)
				{
					decompressedMs.Write(bytex, 0, count);
					count = gzs.Read(bytex, 0, bufSize);
				}
			}

			c = Assembly.Load(decompressedMs.ToArray());
			AppDomain.CurrentDomain.AssemblyResolve += Handler;
		}

		static Assembly Handler(object sender, ResolveEventArgs args)
		{
			if (c.FullName == args.Name)
				return c;
			return null;
		}
	}
}
