using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class Nop : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_NOP; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			state = ExecutionState.Next;
		}
	}
}