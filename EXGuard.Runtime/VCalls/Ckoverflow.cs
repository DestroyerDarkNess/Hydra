using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.VCalls {
	internal class Ckoverflow : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_CKOVERFLOW; }
		}

		public unsafe void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var fSlot = ctx.Stack[sp--];

			if (fSlot.U4 != 0)
				throw new OverflowException();

			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;
			state = ExecutionState.Next;
		}
	}
}