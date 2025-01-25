using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.Mutations;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x0200001C RID: 28
	public class IntsToRandom
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600007B RID: 123 RVA: 0x0000774F File Offset: 0x0000594F
		// (set) Token: 0x0600007C RID: 124 RVA: 0x00007757 File Offset: 0x00005957
		private MethodDef method { get; set; }

		// Token: 0x0600007D RID: 125 RVA: 0x00007760 File Offset: 0x00005960
		public IntsToRandom(MethodDef method)
		{
			this.method = method;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00007774 File Offset: 0x00005974
		public void Execute(ref int i)
		{
			int ldcI4Value = this.method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToRandom.rnd.Next(1, int.MaxValue);
			int num2 = this.randomAssist(num, ldcI4Value);
			bool flag = num2 == 0;
			if (!flag)
			{
				this.method.Body.Instructions[i] = OpCodes.Ldc_I4.ToInstruction(num);
				IList<Instruction> instructions = this.method.Body.Instructions;
				int num3 = i + 1;
				i = num3;
				instructions.Insert(num3, OpCodes.Newobj.ToInstruction(this.method.Module.Import(typeof(Random).GetConstructor(new Type[]
				{
					typeof(int)
				}))));
				IList<Instruction> instructions2 = this.method.Body.Instructions;
				num3 = i + 1;
				i = num3;
				instructions2.Insert(num3, OpCodes.Ldc_I4.ToInstruction(num2));
				IList<Instruction> instructions3 = this.method.Body.Instructions;
				num3 = i + 1;
				i = num3;
				instructions3.Insert(num3, OpCodes.Callvirt.ToInstruction(this.method.Module.Import(typeof(Random).GetMethod("Next", new Type[]
				{
					typeof(int)
				}))));
			}
		}

		// Token: 0x0600007F RID: 127 RVA: 0x000078D8 File Offset: 0x00005AD8
		private int randomAssist(int seed, int returnValue)
		{
			RandomHelper randomHelper = new RandomHelper(seed);
			int num = (int)Math.Round((double)returnValue / (randomHelper.InternalSample() * 4.656612875245797E-10));
			bool flag = num < 0;
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				bool flag2 = new Random(seed).Next(num) == returnValue;
				if (flag2)
				{
					result = num;
				}
				else
				{
					bool flag3 = new Random(seed).Next(num + 1) == returnValue;
					if (flag3)
					{
						result = num + 1;
					}
					else
					{
						result = 0;
					}
				}
			}
			return result;
		}

		// Token: 0x04000028 RID: 40
		public static Random rnd = new Random();
	}
}
