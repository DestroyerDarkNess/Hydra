using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.VCalls {
	internal class Rangechk : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_RANGECHK; }
		}

		public unsafe void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var valueSlot = ctx.Stack[sp--];
			var maxSlot = ctx.Stack[sp--];
			var minSlot = ctx.Stack[sp];

			valueSlot.U8 = ((long)valueSlot.U8 > (long)maxSlot.U8 || (long)valueSlot.U8 < (long)minSlot.U8) ? 1u : 0;

			ctx.Stack[sp] = valueSlot;

			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;
			state = ExecutionState.Next;
		}
	}
}