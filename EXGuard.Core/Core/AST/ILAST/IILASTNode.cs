using System;

namespace EXGuard.Core.AST.ILAST {
	public interface IILASTNode {
		ASTType? Type { get; }
	}
}