using System;
using System.Collections.Generic;

using EXGuard.Core.Helpers;

namespace EXGuard.Core.AST.IR {
	public class IRInstrList : List<IRInstruction> {
		public void VisitInstrs<T>(VisitFunc<IRInstrList, IRInstruction, T> visitFunc, T arg) {
			for (int i = 0; i < Count; i++)
				visitFunc(this, this[i], ref i, arg);
		}
	}
}