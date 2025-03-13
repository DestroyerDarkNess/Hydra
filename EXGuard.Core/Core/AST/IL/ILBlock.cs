using System;
using dnlib.DotNet;
using EXGuard.Core.CFG;
using EXGuard.Core.RT;

namespace EXGuard.Core.AST.IL {
	public class ILBlock : BasicBlock<ILInstrList> {
		public ILBlock(int id, ILInstrList content)
			: base(id, content) {
		}

		public virtual IChunk CreateChunk(VMRuntime rt, MethodDef method) {
			return new BasicBlockChunk(rt, method, this);
		}
	}
}