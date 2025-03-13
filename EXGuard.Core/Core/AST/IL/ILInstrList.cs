using System;
using System.Collections.Generic;

using EXGuard.Core.Helpers;

namespace EXGuard.Core.AST.IL {
	public class ILInstrList : List<ILInstruction> {
		public void VisitInstrs<T>(VisitFunc<ILInstrList, ILInstruction, T> visitFunc, T arg) {
			for (int i = 0; i < Count; i++)
				visitFunc(this, this[i], ref i, arg);
		}
	}
}