using dnlib.DotNet.Emit;

namespace EXGuard.Core.EXECProtections.CEXCFlow
{
	public abstract class BlockBase
	{
		public BlockBase(BlockType type)
		{
			Type = type;
		}

		public ScopeBlock Parent { get; private set; }

		public BlockType Type { get; private set; }

		public abstract void ToBody(CilBody body);
	}
}

