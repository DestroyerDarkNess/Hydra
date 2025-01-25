using System;

namespace HydraEngine.Protection.Mutations
{
	// Token: 0x02000030 RID: 48
	public class RandomHelper
	{
		// Token: 0x060000DF RID: 223 RVA: 0x0000C344 File Offset: 0x0000A544
		public RandomHelper(int Seed)
		{
			int num = (Seed == int.MinValue) ? int.MaxValue : Math.Abs(Seed);
			int num2 = 161803398 - num;
			this.SeedArray[55] = num2;
			int num3 = 1;
			for (int i = 1; i < 55; i++)
			{
				int num4 = 21 * i % 55;
				this.SeedArray[num4] = num3;
				num3 = num2 - num3;
				bool flag = num3 < 0;
				if (flag)
				{
					num3 += int.MaxValue;
				}
				num2 = this.SeedArray[num4];
			}
			for (int j = 1; j < 5; j++)
			{
				for (int k = 1; k < 56; k++)
				{
					this.SeedArray[k] -= this.SeedArray[1 + (k + 30) % 55];
					bool flag2 = this.SeedArray[k] < 0;
					if (flag2)
					{
						this.SeedArray[k] += int.MaxValue;
					}
				}
			}
			this.inext = 0;
			this.inextp = 21;
			Seed = 1;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000C46C File Offset: 0x0000A66C
		public double InternalSample()
		{
			int num = this.inext = 0;
			int num2 = this.inextp = 21;
			bool flag = ++num >= 56;
			if (flag)
			{
				num = 1;
			}
			bool flag2 = ++num2 >= 56;
			if (flag2)
			{
				num2 = 1;
			}
			int num3 = this.SeedArray[num] - this.SeedArray[num2];
			bool flag3 = num3 == int.MaxValue;
			if (flag3)
			{
				num3--;
			}
			bool flag4 = num3 < 0;
			if (flag4)
			{
				num3 += int.MaxValue;
			}
			this.SeedArray[num] = num3;
			this.inext = num;
			this.inextp = num2;
			return (double)num3;
		}

		// Token: 0x04000059 RID: 89
		private int inext;

		// Token: 0x0400005A RID: 90
		private int inextp;

		// Token: 0x0400005B RID: 91
		private int[] SeedArray = new int[56];

		// Token: 0x0400005C RID: 92
		private const int MBIG = 2147483647;

		// Token: 0x0400005D RID: 93
		private const int MSEED = 161803398;

		// Token: 0x0400005E RID: 94
		private const int MZ = 0;
	}
}
