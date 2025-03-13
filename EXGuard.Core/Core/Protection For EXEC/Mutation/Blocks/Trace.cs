using System;
using System.Collections.Generic;

using dnlib.DotNet.Emit;

namespace EXGuard.Core.EXECProtections._Mutation.Blocks
{
	/*
    *  From ConfuserEx ControlFlow (Adapted)
    *  Thanks for ki :)
    *  https://github.com/yck1509/ConfuserEx/blob/master/Confuser.Protections/ControlFlow/SwitchMangler.cs
    */
	public struct Trace
	{
		public Dictionary<uint, int> RefCount;
		public Dictionary<uint, List<Instruction>> BrRefs;
		public Dictionary<uint, int> BeforeStack;
		public Dictionary<uint, int> AfterStack;

		static void Increment(Dictionary<uint, int> counts, uint key)
		{
			int value;
			if (!counts.TryGetValue(key, out value))
				value = 0;
			counts[key] = value + 1;
		}

		public Trace(CilBody body, bool hasReturnValue)
		{
			RefCount = new Dictionary<uint, int>();
			BrRefs = new Dictionary<uint, List<Instruction>>();
			BeforeStack = new Dictionary<uint, int>();
			AfterStack = new Dictionary<uint, int>();

			body.UpdateInstructionOffsets();

			foreach (ExceptionHandler eh in body.ExceptionHandlers)
			{
				BeforeStack[eh.TryStart.Offset] = 0;
				BeforeStack[eh.HandlerStart.Offset] = (eh.HandlerType != ExceptionHandlerType.Finally ? 1 : 0);
				if (eh.FilterStart != null)
					BeforeStack[eh.FilterStart.Offset] = 1;
			}

			int currentStack = 0;
			for (int i = 0; i < body.Instructions.Count; i++)
			{
				var instr = body.Instructions[i];

				if (BeforeStack.ContainsKey(instr.Offset))
					currentStack = BeforeStack[instr.Offset];

				BeforeStack[instr.Offset] = currentStack;
				instr.UpdateStack(ref currentStack, hasReturnValue);
				AfterStack[instr.Offset] = currentStack;

				uint offset;
				switch (instr.OpCode.FlowControl)
				{
					case FlowControl.Branch:
						offset = ((Instruction)instr.Operand).Offset;
						if (!BeforeStack.ContainsKey(offset))
							BeforeStack[offset] = currentStack;

						Increment(RefCount, offset);
						BrRefs.AddListEntry(offset, instr);

						currentStack = 0;
						continue;
					case FlowControl.Call:
						if (instr.OpCode.Code == Code.Jmp)
							currentStack = 0;
						break;
					case FlowControl.Cond_Branch:
						if (instr.OpCode.Code == Code.Switch)
						{
							foreach (Instruction target in (Instruction[])instr.Operand)
							{
								if (!BeforeStack.ContainsKey(target.Offset))
									BeforeStack[target.Offset] = currentStack;

								Increment(RefCount, target.Offset);
								BrRefs.AddListEntry(target.Offset, instr);
							}
						}
						else
						{
							offset = ((Instruction)instr.Operand).Offset;
							if (!BeforeStack.ContainsKey(offset))
								BeforeStack[offset] = currentStack;

							Increment(RefCount, offset);
							BrRefs.AddListEntry(offset, instr);
						}
						break;
					case FlowControl.Meta:
					case FlowControl.Next:
					case FlowControl.Break:
						break;
					case FlowControl.Return:
					case FlowControl.Throw:
						continue;
					default:
						throw new NotSupportedException();
				}

				if (i + 1 < body.Instructions.Count)
				{
					offset = body.Instructions[i + 1].Offset;
					Increment(RefCount, offset);
				}
			}
		}

		public bool IsBranchTarget(uint offset)
		{
			List<Instruction> src;
			if (BrRefs.TryGetValue(offset, out src))
				return src.Count > 0;
			return false;
		}

		public bool HasMultipleSources(uint offset)
		{
			int src;
			if (RefCount.TryGetValue(offset, out src))
				return src > 1;
			return false;
		}

	}
}
