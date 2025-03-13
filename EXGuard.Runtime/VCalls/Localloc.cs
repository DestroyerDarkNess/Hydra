using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.VCalls {
	internal class Localloc : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_LOCALLOC; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var bp = ctx.Registers[ctx.Data.Constants.REG_BP].U4;
			var size = ctx.Stack[sp].U4;
			ctx.Stack[sp] = new VMSlot {
				U8 = (ulong)ctx.Stack.Localloc(bp, size)
			};

			state = ExecutionState.Next;
		}
	}
}