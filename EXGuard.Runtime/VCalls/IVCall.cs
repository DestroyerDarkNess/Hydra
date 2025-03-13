using System;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.VCalls {
	internal interface IVCall {
		byte Code { get; }
		void Run(VMContext ctx, out ExecutionState state);
	}
}