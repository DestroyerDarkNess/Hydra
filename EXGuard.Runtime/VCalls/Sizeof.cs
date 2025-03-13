using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;
using EXGuard.Runtime.Execution.Internal;

namespace EXGuard.Runtime.VCalls {
	internal class Sizeof : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_SIZEOF; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var bp = ctx.Registers[ctx.Data.Constants.REG_BP].U4;
			var type = (Type)ctx.Instance.Data.LookupReference(ctx.Stack[sp].U4);
			ctx.Stack[sp] = new VMSlot {
				U4 = (uint)SizeOfHelper.SizeOf(type)
			};

			state = ExecutionState.Next;
		}
	}
}