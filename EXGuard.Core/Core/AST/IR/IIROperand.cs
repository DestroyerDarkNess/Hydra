using System;

namespace EXGuard.Core.AST.IR {
	public interface IIROperand {
		ASTType Type { get; }
	}
}