using System;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet.Emit;

namespace EXGuard.Core.EXECProtections.CEXCFlow
{
	public class InstrBlock : BlockBase
	{
		public InstrBlock()
			: base(BlockType.Normal)
		{
			Instructions = new List<Instruction>();
		}

		public List<Instruction> Instructions { get; set; }

		public override string ToString()
		{
			var ret = new StringBuilder();
			foreach (Instruction instr in Instructions)
				ret.AppendLine(instr.ToString());
			return ret.ToString();
		}

		public override void ToBody(CilBody body)
		{
			foreach (Instruction instr in Instructions)
				body.Instructions.Add(instr);
		}
	}
}

