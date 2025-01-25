using System;
using dnlib.DotNet;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x0200001B RID: 27
	public class IntsToMethodPointer
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000077 RID: 119 RVA: 0x00007729 File Offset: 0x00005929
		// (set) Token: 0x06000078 RID: 120 RVA: 0x00007731 File Offset: 0x00005931
		private MethodDef method { get; set; }

		// Token: 0x06000079 RID: 121 RVA: 0x0000773A File Offset: 0x0000593A
		public IntsToMethodPointer(MethodDef method)
		{
			this.method = method;
		}

		// Token: 0x0600007A RID: 122 RVA: 0x0000774C File Offset: 0x0000594C
		public void Execute()
		{
		}
	}
}
