using System;

namespace EXGuard.Core.AST.IR {
	public enum IRVariableType {
		VirtualRegister,
		Local,
		Argument,
		ExceptionObj
	}
}