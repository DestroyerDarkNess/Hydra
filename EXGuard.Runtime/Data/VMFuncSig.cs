using System;
using System.IO;
using System.Reflection;

namespace EXGuard.Runtime.Data {
	internal class VMFuncSig {
		readonly int[] paramToks;
		readonly double[] paramToks_Keys;

		readonly int retTok;
		readonly double retTok_Key;

		Type[] paramTypes;
		Type retType;

		public unsafe VMFuncSig(BinaryReader reader) {
			paramToks = new int[reader.ReadInt32()];
			paramToks_Keys = new double[paramToks.Length];

			for (int i = 0; i < paramToks.Length; i++)
            {
				paramToks[i] = reader.ReadInt32();
				paramToks_Keys[i] = reader.ReadDouble();
			}

			retTok = reader.ReadInt32();
			retTok_Key = reader.ReadDouble();
		}

		public Type[] ParamTypes {
			get {
				if (paramTypes != null)
					return paramTypes;

				var p = new Type[paramToks.Length];
				for (int i = 0; i < p.Length; i++) {
					p[i] = VMInstance.__ExecuteModule.ResolveType(Utils.Decrypt(paramToks[i], paramToks_Keys[i]));
				}

				paramTypes = p;
				return p;
			}
		}

		public Type RetType {
			get { return retType ?? (retType = VMInstance.__ExecuteModule.ResolveType(Utils.Decrypt(retTok, retTok_Key))); }
		}
	}
}