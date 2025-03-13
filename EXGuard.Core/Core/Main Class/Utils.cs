using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using dnlib.DotNet;
using dnlib.DotNet.Writer;

using EXGuard.Core.VM;
using EXGuard.Core.AST.IR;

namespace EXGuard.Core
{
	public static class Utils {
		public static readonly char[] hexCharset = "0123456789ABCDEF".ToCharArray();

		public static ModuleWriterOptions ExecuteModuleWriterOptions;

		public static void AssemblyReferencesAdder(this ModuleDef moduleDefMD)
		{
			var AsmResolver = new AssemblyResolver { EnableTypeDefCache = true };
			var ModCtx = new ModuleContext(AsmResolver);

			AsmResolver.DefaultModuleContext = ModCtx;
			moduleDefMD.Context = ModCtx;

			foreach (var AsmRef in moduleDefMD.GetAssemblyRefs())
			{
				try
				{
					if (AsmRef == null)
						continue;

					var ASM = AsmResolver.Resolve(AsmRef.FullName, moduleDefMD);
					if (ASM == null)
						continue;

					((AssemblyResolver)moduleDefMD.Context.AssemblyResolver).AddToCache(ASM);
				}
				catch { }
			}
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

		public static IList<T> RemoveWhere<T>(this IList<T> self, Predicate<T> match)
		{
			for (int i = self.Count - 1; i >= 0; i--)
			{
				if (match(self[i]))
					self.RemoveAt(i);
			}
			return self;
		}

		public static void AddRange<T>(this IList<T> list, IList<T> values)
		{
			for (int i = 0; i < values.Count; i++)
			{
				list.Add(values[i]);
			}
		}

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

		public static StrongNameKey LoadSNKey(string path, string pass)
        {
            if (path == null) return null;

            try
            {
                if (pass != null) //pfx
                {
                    // http://stackoverflow.com/a/12196742/462805
                    var cert = new X509Certificate2();
                    cert.Import(path, pass, X509KeyStorageFlags.Exportable);

                    var rsa = cert.PrivateKey as RSACryptoServiceProvider;

                    if (rsa == null)
                        throw new ArgumentException("RSA key does not present in the certificate.", "path");

                    return new StrongNameKey(rsa.ExportCspBlob(true));
                }
                return new StrongNameKey(path);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Cannot load the Strong Name Key located at: " + path);
                throw ex;
            }
        }

		public static string ToHexString(this string str)
		{
			var sb = new StringBuilder();
			var bytes = Encoding.Unicode.GetBytes(str);
			foreach (var t in bytes)
			{
				sb.Append(t.ToString("X2"));
			}
			return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
		}

		public static string ToHexString(this byte[] buff)
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

		public static void Increment<T>(this Dictionary<T, int> self, T key) {
			int count;
			if (!self.TryGetValue(key, out count))
				count = 0;
			self[key] = ++count;
		}

		public static void Replace<T>(this List<T> list, int index, IEnumerable<T> newItems) {
			list.RemoveAt(index);
			list.InsertRange(index, newItems);
		}

		public static void Replace(this List<IRInstruction> list, int index, IEnumerable<IRInstruction> newItems) {
			var instr = list[index];
			list.RemoveAt(index);
			foreach (var i in newItems)
				i.ILAST = instr.ILAST;
			list.InsertRange(index, newItems);
		}

		public static bool IsGPR(this VMRegisters reg) {
			if (reg >= VMRegisters.R0 && reg <= VMRegisters.R7)
				return true;
			return false;
		}

		public static TypeSig ResolveType(this GenericArguments genericArgs, TypeSig typeSig) {
			switch (typeSig.ElementType) {
				case ElementType.Ptr:
					return new PtrSig(genericArgs.ResolveType(typeSig.Next));

				case ElementType.ByRef:
					return new ByRefSig(genericArgs.ResolveType(typeSig.Next));

				case ElementType.SZArray:
					return new SZArraySig(genericArgs.ResolveType(typeSig.Next));

				case ElementType.Array:
					var arraySig = (ArraySig)typeSig;
					return new ArraySig(genericArgs.ResolveType(typeSig.Next), arraySig.Rank, arraySig.Sizes, arraySig.LowerBounds);

				case ElementType.Pinned:
					return new PinnedSig(genericArgs.ResolveType(typeSig.Next));

				case ElementType.Var:
				case ElementType.MVar:
					return genericArgs.Resolve(typeSig);

				case ElementType.GenericInst:
					var genInst = (GenericInstSig)typeSig;
					var typeArgs = new List<TypeSig>();
					foreach (var arg in genInst.GenericArguments)
						typeArgs.Add(genericArgs.ResolveType(arg));
					return new GenericInstSig(genInst.GenericType, typeArgs);

				case ElementType.CModReqd:
					return new CModReqdSig(((CModReqdSig)typeSig).Modifier, genericArgs.ResolveType(typeSig.Next));

				case ElementType.CModOpt:
					return new CModOptSig(((CModOptSig)typeSig).Modifier, genericArgs.ResolveType(typeSig.Next));

				case ElementType.ValueArray:
					return new ValueArraySig(genericArgs.ResolveType(typeSig.Next), ((ValueArraySig)typeSig).Size);

				case ElementType.Module:
					return new ModuleSig(((ModuleSig)typeSig).Index, genericArgs.ResolveType(typeSig.Next));
			}
			if (typeSig.IsTypeDefOrRef) {
				var s = (TypeDefOrRefSig)typeSig;
				if (s.TypeDefOrRef is TypeSpec)
					throw new NotSupportedException(); // TODO: ?
			}
			return typeSig;
		}

		public static unsafe int EncryptInt(this int input, double Key)
		{
			byte[] KEY = BitConverter.GetBytes(Key);
			
			int IV0 = 0;
			int IV1 = 0;
			int IV2 = 0;
			int IV3 = 0;
			int[] IVS = new int[8];

			for (int a = 0; a < 8; a++)
				IVS[a] = KEY[a] % (a + 1) ^ (int)input ^ (a + 1) ^ (int)input;

			for (int i = 0; i < 8; i++)
			{
				int X = (int)Math.Log10(IVS[i]);

				IV0 ^= IVS[i] ^ (((IVS[i] ^ (int)X)) * i >> (int)((float)i * (float)0.25F));
				IV1 += IVS[i] >> (((IVS[i] ^ (int)X)) * i << (int)((short)i + (float)0.58F));
				IV2 -= IVS[i] << (((IVS[i] ^ (int)X)) * i >> (int)((float)i * (float)0.41F));
				IV3 ^= IVS[i] + (((IVS[i] ^ (int)X)) * i << (int)((float)i - (float)0.99F));
			}

			return (IV0 ^ input) ^ (IV1 ^ input) ^ (IV2 ^ input) ^ IV3;
		}
	}
}