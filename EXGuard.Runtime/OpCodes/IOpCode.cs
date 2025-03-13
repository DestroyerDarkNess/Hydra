using System;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal interface IOpCode {
		byte Code { get; }
		void Run(VMContext ctx, out ExecutionState state);
	}
}