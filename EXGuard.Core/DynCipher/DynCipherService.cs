using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core;
using EXGuard.Core.Services;
using EXGuard.DynCipher.AST;
using EXGuard.Core.Helpers.System;
using EXGuard.DynCipher.Generation;

namespace EXGuard.DynCipher {
	public class DynCipherService {
		public void GenerateCipherPair(RandomGenerator random, out StatementBlock encrypt, out StatementBlock decrypt) {
			CipherGenerator.GeneratePair(random, out encrypt, out decrypt);
		}

		public void GenerateExpressionPair(RandomGenerator random, Expression var, Expression result, int depth, out Expression expression, out Expression inverse) {
			ExpressionGenerator.GeneratePair(random, var, result, depth, out expression, out inverse);
		}


		class MutationGen : CILCodeGen {
			readonly Local state;

			public MutationGen(Local state, MethodDef method, IList<Instruction> instrs)
				: base(method, instrs) {
				this.state = state;
			}

			protected override Local Var(Variable var) {
				if (var.Name == "{RESULT}")
					return state;
				return base.Var(var);
			}
		}

		public void GenerateExpressionMutation(RandomGenerator random, MethodDef method, Local stateVariable, IList<Instruction> instructions, int initialValue, int depth) {

			var list = new List<Instruction>();

			Variable var = new Variable("{VAR}");
			Variable result = new Variable("{RESULT}");

			GenerateExpressionPair(random,
				new VariableExpression { Variable = var }, 
				new VariableExpression { Variable = result },
				depth,
				out var encrypt, 
				out var decrypt);

			CilBody body = method.Body;

			body.MaxStack += (ushort)depth;

			body.InitLocals = true;

			IList<Instruction> emit = new List<Instruction>();
			MutationGen gen = new MutationGen(stateVariable, method, emit);
			gen.GenerateCIL(decrypt);

			var encryptionFunc = new DMCodeGen(typeof(int), new[] { Tuple.Create("{VAR}", typeof(int)) })
			.GenerateCIL(encrypt)
			.Compile<Func<int, int>>();

			int encrypted = encryptionFunc(initialValue);

			instructions.Add(Instruction.CreateLdcI4(encrypted));
			instructions.Add(Instruction.Create(OpCodes.Stloc, stateVariable));
			instructions.AddRange(emit);
		}

		public void GenerateStatementMutation(RandomGenerator random, MethodDef method, Local stateVariable, IList<Instruction> instructions, int initialValue, int depth) {

			var list = new List<Instruction>();

			Variable var = new Variable("{VAR}");
			Variable result = new Variable("{RESULT}");

			StatementGenerator.GeneratePair(random,
				new VariableExpression { Variable = var },
				new VariableExpression { Variable = result },
				depth,
				out var encrypt,
				out var decrypt);

			CilBody body = method.Body;

			body.MaxStack += (ushort)depth;

			body.InitLocals = true;

			IList<Instruction> emit = new List<Instruction>();
			MutationGen gen = new MutationGen(stateVariable, method, emit);
			gen.GenerateCIL(decrypt);

			var encryptionFunc = new DMCodeGen(typeof(int), new[] { Tuple.Create("{VAR}", typeof(int)) })
			.GenerateCIL(encrypt)
			.GenerateCIL(new VariableExpression {
				Variable = var,
			})
			.Compile<Func<int, int>>();

			int encrypted = encryptionFunc(initialValue);

			instructions.Add(Instruction.CreateLdcI4(encrypted));
			instructions.Add(Instruction.Create(OpCodes.Stloc, stateVariable));
			instructions.AddRange(emit);
			instructions.Add(OpCodes.Ldloc.ToInstruction(stateVariable));
		}
	}
}
