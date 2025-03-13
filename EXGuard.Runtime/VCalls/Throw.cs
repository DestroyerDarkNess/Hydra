using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.VCalls {
	internal class Throw : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_THROW; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var type = ctx.Stack[sp--].U4;
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;
			if (type == 1)
				state = ExecutionState.Rethrow;
			else
				state = ExecutionState.Throw;
		}
	}
}