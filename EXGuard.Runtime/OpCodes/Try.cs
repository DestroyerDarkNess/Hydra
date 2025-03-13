using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class Try : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_TRY; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var type = ctx.Stack[sp--].U1;

			var frame = new EHFrame();
			frame.EHType = type;
			if (type == ctx.Data.Constants.EH_CATCH) {
				frame.CatchType = (Type)ctx.Instance.Data.LookupReference(ctx.Stack[sp--].U4);
			}
			else if (type == ctx.Data.Constants.EH_FILTER) {
				frame.FilterAddr = ctx.Stack[sp--].U8;
			}
			frame.HandlerAddr = ctx.Stack[sp--].U8;

			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			frame.BP = ctx.Registers[ctx.Data.Constants.REG_BP];
			frame.SP = ctx.Registers[ctx.Data.Constants.REG_SP];
			ctx.EHStack.Add(frame);

			state = ExecutionState.Next;
		}
	}
}