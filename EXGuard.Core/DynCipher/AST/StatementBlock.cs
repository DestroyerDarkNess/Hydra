using System.Text;
using System.Collections.Generic;

namespace EXGuard.DynCipher.AST {
	public class StatementBlock : Statement {
		public StatementBlock() {
			Statements = new List<Statement>();
		}

		public IList<Statement> Statements { get; private set; }

		public override string ToString() {
			var sb = new StringBuilder();
			sb.AppendLine("{");
			foreach (Statement i in Statements)
				sb.AppendLine(i.ToString());
			sb.AppendLine("}");
			return sb.ToString();
		}
	}
}