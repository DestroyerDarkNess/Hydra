using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations
{
	// Token: 0x0200002F RID: 47
	public class MutationHelper
	{
		// Token: 0x060000DC RID: 220 RVA: 0x0000C058 File Offset: 0x0000A258
		public static bool CanObfuscate(IList<Instruction> instructions, int i)
		{
			bool flag = instructions[i + 1].GetOperand() != null;
			if (flag)
			{
				bool flag2 = instructions[i + 1].Operand.ToString().Contains("bool");
				if (flag2)
				{
					return false;
				}
			}
			bool flag3 = instructions[i + 1].GetOpCode() == OpCodes.Newobj;
			bool result;
			if (flag3)
			{
				result = false;
			}
			else
			{
				bool flag4 = instructions[i].GetLdcI4Value() == 0 || instructions[i].GetLdcI4Value() == 1;
				result = !flag4;
			}
			return result;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000C0EC File Offset: 0x0000A2EC
		public static List<MutationHelper.Block> ParseMethod(MethodDef method)
		{
			List<MutationHelper.Block> list = new List<MutationHelper.Block>();
			List<Instruction> list2 = new List<Instruction>(method.Body.Instructions);
			MutationHelper.Block block = new MutationHelper.Block();
			int num = 0;
			int num2 = 0;
			block.Number = num;
			block.Instructions.Add(Instruction.Create(OpCodes.Nop));
			list.Add(block);
			block = new MutationHelper.Block();
			Stack<ExceptionHandler> stack = new Stack<ExceptionHandler>();
			foreach (Instruction instruction in method.Body.Instructions)
			{
				foreach (ExceptionHandler exceptionHandler in method.Body.ExceptionHandlers)
				{
					bool flag = exceptionHandler.HandlerStart == instruction || exceptionHandler.TryStart == instruction || exceptionHandler.FilterStart == instruction;
					if (flag)
					{
						stack.Push(exceptionHandler);
					}
				}
				foreach (ExceptionHandler exceptionHandler2 in method.Body.ExceptionHandlers)
				{
					bool flag2 = exceptionHandler2.HandlerEnd == instruction || exceptionHandler2.TryEnd == instruction;
					if (flag2)
					{
						stack.Pop();
					}
				}
				int num3;
				int num4;
				instruction.CalculateStackUsage(out num3, out num4);
				block.Instructions.Add(instruction);
				num2 += num3 - num4;
				bool flag3 = num3 == 0;
				if (flag3)
				{
					bool flag4 = instruction.OpCode != OpCodes.Nop;
					if (flag4)
					{
						bool flag5 = (num2 == 0 || instruction.OpCode == OpCodes.Ret) && stack.Count == 0;
						if (flag5)
						{
							num = (block.Number = num + 1);
							list.Add(block);
							block = new MutationHelper.Block();
						}
					}
				}
			}
			return list;
		}

		// Token: 0x02000051 RID: 81
		public class Block
		{
			// Token: 0x06000185 RID: 389 RVA: 0x0000F24F File Offset: 0x0000D44F
			public Block()
			{
				this.Instructions = new List<Instruction>();
			}

			// Token: 0x1700002E RID: 46
			// (get) Token: 0x06000186 RID: 390 RVA: 0x0000F265 File Offset: 0x0000D465
			// (set) Token: 0x06000187 RID: 391 RVA: 0x0000F26D File Offset: 0x0000D46D
			public List<Instruction> Instructions { get; set; }

			// Token: 0x1700002F RID: 47
			// (get) Token: 0x06000188 RID: 392 RVA: 0x0000F276 File Offset: 0x0000D476
			// (set) Token: 0x06000189 RID: 393 RVA: 0x0000F27E File Offset: 0x0000D47E
			public int Number { get; set; }

			// Token: 0x17000030 RID: 48
			// (get) Token: 0x0600018A RID: 394 RVA: 0x0000F287 File Offset: 0x0000D487
			// (set) Token: 0x0600018B RID: 395 RVA: 0x0000F28F File Offset: 0x0000D48F
			public int Next { get; set; }
		}
	}
}
