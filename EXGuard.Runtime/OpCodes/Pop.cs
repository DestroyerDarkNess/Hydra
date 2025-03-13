using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class Pop : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_POP; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var slot = ctx.Stack[sp];
			ctx.Stack.SetTopPosition(--sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var regId = ctx.ReadByte();
			if ((regId == ctx.Data.Constants.REG_SP || regId == ctx.Data.Constants.REG_BP) && slot.O is StackRef)
				ctx.Registers[regId] = new VMSlot { U4 = ((StackRef)slot.O).StackPos };
			else
				ctx.Registers[regId] = slot;
			state = ExecutionState.Next;
		}
	}
}