using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.RTProtection;

namespace EXGuard.Runtime.Data
{
	internal unsafe class VMData
	{
		static Dictionary<Module, VMData> ModuleVMData = new Dictionary<Module, VMData>()
		{
			{ VMInstance.__ExecuteModule, new VMData() }
		};

		Dictionary<uint, string> Strings;
		Dictionary<uint, RefInfo> References;
		Dictionary<MethodBase, VMExportInfo> Exports;

		public Constants Constants
		{
			get;
			private set;
		}

		public VMData()
		{
			Strings = new Dictionary<uint, string>();
			References = new Dictionary<uint, RefInfo>();
			Exports = new Dictionary<MethodBase, VMExportInfo>();
			
			uint[] encryptedData = new uint[Mutation.IntKey0];

			if (encryptedData.Length != 0 && AntiDump.AntiDumpIsRunning)
			{
				RuntimeHelpers.InitializeArray(encryptedData, Mutation.LocationIndex<RuntimeFieldHandle>());

				#region Decrypt and Decompress Data
				////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
				ulong state = (uint)Mutation.IntKey1;

				var dst = new uint[0x10];
				var src = new uint[0x10];

				for (int i = 0; i < 0x10; i++)
				{
					state = (state * state) % 0x143fc089;

					src[i] = (uint)state;
					dst[i] = (uint)((state * state) % 0x444d56fb);
				}

				Mutation.Crypt(dst, src);

				for (int i = 0; i < 0x10; i++)
				{
					state ^= state >> 13;
					state ^= state << 25;
					state ^= state >> 27;

					src[i] = 0;

					switch (i % 3)
					{
						case 0:
							dst[i] = dst[i] ^ (uint)state;
							break;
						case 1:
							dst[i] = dst[i] * (uint)state;
							break;
						case 2:
							dst[i] = dst[i] + (uint)state;
							break;
					}
				}

				var beginDecryptedBuffer = new byte[encryptedData.Length << 2];

				uint keyIndex = 0;
				for (int i = 0; i < encryptedData.Length; i++)
				{
					uint decrypted = encryptedData[i] ^ dst[i & 0xf];

					dst[i & 0xf] = (dst[i & 0xf] ^ decrypted) + 0x3ddb2819;

					beginDecryptedBuffer[keyIndex + 0] = (byte)(decrypted >> 0);
					beginDecryptedBuffer[keyIndex + 1] = (byte)(decrypted >> 8);
					beginDecryptedBuffer[keyIndex + 2] = (byte)(decrypted >> 16);
					beginDecryptedBuffer[keyIndex + 3] = (byte)(decrypted >> 24);

					keyIndex += 4;
				}

				for (int i = 0; i < 0x10; i++)
					dst[i] = 0;

				#region LZMA Decompress
				///////////////////////////////////////////////////////////////
				byte[] dataBuffer = Lzma.Decompress(beginDecryptedBuffer);
				///////////////////////////////////////////////////////////////
				#endregion

				for (int i = 0; i < beginDecryptedBuffer.Length; i++)
					beginDecryptedBuffer[i] = 0;

				for (int i = 0; i < dataBuffer.Length; i++)
				{
					dataBuffer[i] ^= (byte)state;

					if ((i & 0xff) == 0)
						state = (state * state) % 0x8a5cb7;
				}
				////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
				#endregion

				var ptrBuffer = Marshal.AllocHGlobal(dataBuffer.Length);
				Marshal.Copy(dataBuffer, 0, ptrBuffer, dataBuffer.Length);

				var stream = new UnmanagedMemoryStream((byte*)ptrBuffer, dataBuffer.Length);
				var reader = new BinaryReader(stream);

				try
				{
					Constants = Utils.ReadConstants(reader);

					for (int i = 0; i < Mutation.IntKey2; i++)
					{
						var id = reader.ReadUInt32();
						var length = reader.ReadInt32();

						byte[] buffer = reader.ReadBytes(length);
						Strings.Add(id, Encoding.Unicode.GetString(buffer));
					}

					for (int i = 0; i < Mutation.IntKey3; i++)
					{
						var id = reader.ReadUInt32();
						var token = reader.ReadInt32();
						var encryptKey = reader.ReadDouble();

						References.Add(id, new RefInfo(token, encryptKey));
					}

					for (int i = 0; i < Mutation.IntKey4; i++)
					{
						var mdtoken = reader.ReadInt32();
						var exportInfo = VMExportInfo.FromReader(reader, ref ptrBuffer);

						Exports.Add(VMInstance.__ExecuteModule.ResolveMethod(mdtoken), exportInfo);
					}
				}
				finally
				{
					stream.Dispose();
					reader.Close();
				}
			}
			else
            {
				Strings = new Dictionary<uint, string>();
				References = new Dictionary<uint, RefInfo>();
				Exports = new Dictionary<MethodBase, VMExportInfo>();
			}
		}

		public static VMData GetVMData()
		{
			var enumerator = ModuleVMData.Values.GetEnumerator();
			enumerator.MoveNext();

			return enumerator.Current;
		}

		public string LookupString(uint id)
		{
			if (id == 0)
				return null;

			return Strings[id];
		}

		public MemberInfo LookupReference(uint id)
		{
			return References[id].Member();
		}
	
		public VMExportInfo LookupExport(MethodBase methodBase)
		{
			return Exports[methodBase];
		}
	}
}