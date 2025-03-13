using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.VCalls {
	internal class Exit : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_EXIT; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			state = ExecutionState.Exit;
		}
	}
}