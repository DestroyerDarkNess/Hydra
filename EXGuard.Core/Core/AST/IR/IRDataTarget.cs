using System;
using EXGuard.Core.RT;

namespace EXGuard.Core.AST.IR {
	public class IRDataTarget : IIROperand {
		public IRDataTarget(BinaryChunk target) {
			Target = target;
		}

		public BinaryChunk Target { get; set; }
		public string Name { get; set; }

		public ASTType Type {
			get { return ASTType.Ptr; }
		}

		public override string ToString() {
			return Name;
		}
	}
}