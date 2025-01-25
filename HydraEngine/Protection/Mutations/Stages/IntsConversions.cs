using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x02000015 RID: 21
	public class IntsConversions
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600004D RID: 77 RVA: 0x00005D7B File Offset: 0x00003F7B
		// (set) Token: 0x0600004E RID: 78 RVA: 0x00005D83 File Offset: 0x00003F83
		private MethodDef Method { get; set; }

		// Token: 0x0600004F RID: 79 RVA: 0x00005D8C File Offset: 0x00003F8C
		public IntsConversions(MethodDef method)
		{
			this.Method = method;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00005DA0 File Offset: 0x00003FA0
		public void Execute()
		{
			int i = 0;
			while (i < this.Method.Body.Instructions.Count)
			{
				bool flag = this.Method.Body.Instructions[i].IsLdcI4();
				if (flag)
				{
					int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
					bool flag2 = ldcI4Value < 10 && ldcI4Value > 0;
					if (flag2)
					{
						this.ConvToStrLen(ref i);
					}
					else
					{
						this.ConvToIntPtr(ref i);
					}
				}
				IL_69:
				i++;
				continue;
				goto IL_69;
			}
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00005E3C File Offset: 0x0000403C
		private bool HasEmptyTypes()
		{
			for (int i = 0; i < this.Method.Body.Instructions.Count; i++)
			{
				bool flag = this.Method.Body.Instructions[i].IsLdcI4();
				if (flag)
				{
					bool flag2 = this.Method.Body.Instructions[i].GetLdcI4Value() < 1;
					if (flag2)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00005EBC File Offset: 0x000040BC
		private void ConvToIntPtr(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = ldcI4Value;
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Call.ToInstruction(this.Method.Module.Import(typeof(IntPtr).GetMethod("op_Explicit", new Type[]
			{
				typeof(int)
			}))));
			this.Method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Conv_Ovf_I4));
			i += 2;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00005FB0 File Offset: 0x000041B0
		private void ConvToStrLen(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldstr;
			this.Method.Body.Instructions[i].Operand = this.RandomString(ldcI4Value);
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Ldlen.ToInstruction());
			i++;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x0000604C File Offset: 0x0000424C
		private string RandomString(int len)
		{
			string text = "abcdefghijklmnopqrstuvwxyz1234567890";
			string text2 = "";
			for (int i = 0; i < len; i++)
			{
				text2 += text[IntsConversions.rnd.Next(0, text.Length)].ToString();
			}
			return text2;
		}

		// Token: 0x0400001E RID: 30
		private static Random rnd = new Random();
	}
}
