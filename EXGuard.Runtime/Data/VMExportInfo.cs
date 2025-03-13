using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace EXGuard.Runtime.Data {
	internal unsafe class VMExportInfo {
		public static VMExportInfo FromReader(BinaryReader reader, ref IntPtr data)
		{
			uint offset = reader.ReadUInt32();
			uint entryKey = offset != 0 ? reader.ReadUInt32() : 0;

			var exportInfo = new VMExportInfo
			{
				CodeOffset = offset,
				EntryKey = entryKey,
				CodeAddress = (IntPtr)((byte*)data + offset),
				Signature = new VMFuncSig(reader)
			};

			return exportInfo;
		}

		public uint CodeOffset
		{
			get;
			private set;
		}

		public uint EntryKey
		{
			get;
			private set;
		}

		public IntPtr CodeAddress
		{
			get;
			private set;
		}

		public VMFuncSig Signature
		{
			get;
			private set;
		}
	}
}