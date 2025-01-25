using System;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations
{
	// Token: 0x02000014 RID: 20
	public class Calculator
	{
		// Token: 0x06000048 RID: 72 RVA: 0x00005CA8 File Offset: 0x00003EA8
		public Calculator(int value, int value2)
		{
			this.result = this.Calculate(value, value2);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00005CD0 File Offset: 0x00003ED0
		public int getResult()
		{
			return this.result;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00005CE8 File Offset: 0x00003EE8
		public OpCode getOpCode()
		{
			return this.cOpCode;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00005D00 File Offset: 0x00003F00
		private int Calculate(int num, int num2)
		{
			int num3 = 0;
			switch (Calculator.rnd.Next(0, 3))
			{
			case 0:
				num3 = num + num2;
				this.cOpCode = OpCodes.Sub;
				break;
			case 1:
				num3 = (num ^ num2);
				this.cOpCode = OpCodes.Xor;
				break;
			case 2:
				num3 = num - num2;
				this.cOpCode = OpCodes.Add;
				break;
			}
			return num3;
		}

		// Token: 0x0400001A RID: 26
		public static Random rnd = new Random();

		// Token: 0x0400001B RID: 27
		private OpCode cOpCode = null;

		// Token: 0x0400001C RID: 28
		private int result = 0;
	}
}
