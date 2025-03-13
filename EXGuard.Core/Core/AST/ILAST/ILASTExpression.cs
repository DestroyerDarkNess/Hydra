using dnlib.DotNet.Emit;

namespace EXGuard.Core.AST.ILAST {
	public class ILASTExpression : ASTExpression, IILASTNode, IILASTStatement {
		public Code ILCode { get; set; }
		public Instruction CILInstr { get; set; }
		public object Operand { get; set; }
		public IILASTNode[] Arguments { get; set; }
		public Instruction[] Prefixes { get; set; }

		public ILASTExpression Clone() {
			return (ILASTExpression)MemberwiseClone();
		}
	}
}