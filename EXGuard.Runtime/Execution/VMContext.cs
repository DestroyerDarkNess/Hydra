using System;
using System.Collections.Generic;

using EXGuard.Runtime.Data;

namespace EXGuard.Runtime.Execution {
	public class VMContext {
		const int NumRegisters = 16;

		internal readonly VMSlot[] Registers = new VMSlot[16];
        internal readonly VMStack Stack = new VMStack();
        internal readonly VMInstance Instance;
        internal readonly VMData Data;
        internal readonly List<EHFrame> EHStack = new List<EHFrame>();
        internal readonly List<EHState> EHStates = new List<EHState>();

        internal VMContext(VMInstance inst) {
			Instance = inst;
			Data = inst.Data;
        }

        internal unsafe byte ReadByte() {
			var key = Registers[Data.Constants.REG_K1].U4;
			var ip = (byte*)Registers[Data.Constants.REG_IP].U8++;
			byte b = (byte)(*ip ^ key);
			key = key * 7 + b;
			Registers[Data.Constants.REG_K1].U4 = key;
			return b;
		}
	}
}