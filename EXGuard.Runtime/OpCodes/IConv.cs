using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class IConvPtr : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_ICONV_PTR; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var valueSlot = ctx.Stack[sp];

			byte fl = (byte)(ctx.Registers[ctx.Data.Constants.REG_FL].U1 & ~ctx.Data.Constants.FL_OVERFLOW);
			if (!(IntPtr.Size == 8) && (valueSlot.U8 >> 32) != 0)
				fl |= ctx.Data.Constants.FL_OVERFLOW;
			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = fl;

			ctx.Stack[sp] = valueSlot;

			state = ExecutionState.Next;
		}
	}

	internal class IConvR64 : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_ICONV_R64; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			// coreclr/src/vm/jithelpers.cpp JIT_Dbl2ULngOvf & JIT_Dbl2LngOvf

			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var valueSlot = ctx.Stack[sp];

			const double two63 = 2147483648.0 * 4294967296.0;
			const double two64 = 4294967296.0 * 4294967296.0;

			double value = valueSlot.R8;
			valueSlot.U8 = (ulong)(long)value;
			byte fl = (byte)(ctx.Registers[ctx.Data.Constants.REG_FL].U1 & ~ctx.Data.Constants.FL_OVERFLOW);

			if ((fl & ctx.Data.Constants.FL_UNSIGNED) != 0) {
				if (!(value > -1.0 && value < two64))
					fl |= ctx.Data.Constants.FL_OVERFLOW;

				if (!(value < two63))
					valueSlot.U8 = (ulong)((long)value - two63) + 0x8000000000000000UL;
			}
			else {
				if (!(value > -two63 - 0x402 && value < two63))
					fl |= ctx.Data.Constants.FL_OVERFLOW;
			}

			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = (byte)(fl & ~ctx.Data.Constants.FL_UNSIGNED);

			ctx.Stack[sp] = valueSlot;

			state = ExecutionState.Next;
		}
	}
}