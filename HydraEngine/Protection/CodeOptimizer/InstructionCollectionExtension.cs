using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.CodeOptimizer
{
	internal static class InstructionCollectionExtension
	{
		public static void FixJumps(this IEnumerable<Instruction> instructions, Dictionary<Instruction, Instruction> oldToNewTargets)
		{
			if (oldToNewTargets.Count == 0)
			{
				return;
			}
			foreach (Instruction instruction in instructions)
			{
				if (instruction.Operand is Instruction)
				{
					Instruction key = (Instruction)instruction.Operand;
					Instruction operand;
					if (oldToNewTargets.TryGetValue(key, out operand))
					{
						instruction.Operand = operand;
					}
				}
				else if (instruction.Operand is Instruction[])
				{
					Instruction[] operandArray = (Instruction[])instruction.Operand;
					Instruction[] operandArrayTW = new Instruction[operandArray.Length];
					for (int i = 0; i < operandArray.Length; i++)
					{
						Instruction inst;
						if (oldToNewTargets.TryGetValue(operandArray[i], out inst))
						{
							operandArrayTW[i] = inst;
						}
						else
						{
							operandArrayTW[i] = operandArray[i];
						}
					}
					instruction.Operand = operandArrayTW;
				}
			}
		}
		public static bool AnyJumpTo(this IEnumerable<Instruction> instructions, Instruction target)
		{
			foreach (Instruction instruction in instructions)
			{
				if (instruction.Operand is Instruction)
				{
					if ((Instruction)instruction.Operand == target)
					{
						return true;
					}
				}
				else if (instruction.Operand is Instruction[])
				{
					Instruction[] array = (Instruction[])instruction.Operand;
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] == target)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
	}
}
