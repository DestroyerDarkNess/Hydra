using System;

namespace EXGuard.Core.AST.IL {
	public interface IHasOffset {
		uint Offset { get; }
	}
}