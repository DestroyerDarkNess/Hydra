using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class Call : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_CALL; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var slot = ctx.Stack[sp];
			ctx.Stack[sp] = ctx.Registers[ctx.Data.Constants.REG_IP];
			ctx.Registers[ctx.Data.Constants.REG_IP].U8 = slot.U8;
			state = ExecutionState.Next;
		}
	}
}