using System;

namespace EXGuard.Core.AST.ILAST {
	public enum ILASTVariableType {
		StackVar,
		ExceptionVar,
		FilterVar,
		PhiVar
	}
}