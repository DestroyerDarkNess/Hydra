using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class Leave : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_LEAVE; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var handler = ctx.Stack[sp--].U8;

			var frameIndex = ctx.EHStack.Count - 1;
			var frame = ctx.EHStack[frameIndex];

			if (frame.HandlerAddr != handler)
				throw new InvalidProgramException();
			ctx.EHStack.RemoveAt(frameIndex);

			if (frame.EHType == ctx.Data.Constants.EH_FINALLY) {
				ctx.Stack[++sp] = ctx.Registers[ctx.Data.Constants.REG_IP];
				ctx.Registers[ctx.Data.Constants.REG_K1].U1 = 0;
				ctx.Registers[ctx.Data.Constants.REG_IP].U8 = frame.HandlerAddr;
			}

			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			state = ExecutionState.Next;
		}
	}
}