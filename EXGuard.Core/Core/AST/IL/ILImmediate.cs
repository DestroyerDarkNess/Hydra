using System;

namespace EXGuard.Core.AST.IL {
	public class ILImmediate : ASTConstant, IILOperand {
		public static ILImmediate Create(object value, ASTType type) {
			return new ILImmediate {
				Value = value,
				Type = type
			};
		}
	}
}