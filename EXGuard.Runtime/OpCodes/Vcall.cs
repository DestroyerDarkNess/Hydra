using System;
using EXGuard.Runtime.Data;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class Vcall : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_VCALL; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var slot = ctx.Stack[sp];
			ctx.Stack.SetTopPosition(--sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var vCall = VCallMap.Lookup(slot.U1);
			vCall.Run(ctx, out state);
		}
	}
}