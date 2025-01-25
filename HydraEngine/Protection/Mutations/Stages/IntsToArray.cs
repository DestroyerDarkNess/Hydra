using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x02000017 RID: 23
	public class IntsToArray
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600005B RID: 91 RVA: 0x000062F0 File Offset: 0x000044F0
		// (set) Token: 0x0600005C RID: 92 RVA: 0x000062F8 File Offset: 0x000044F8
		public MethodDef method { get; set; }

		// Token: 0x0600005D RID: 93 RVA: 0x00006301 File Offset: 0x00004501
		public IntsToArray(MethodDef method)
		{
			this.method = method;
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00006314 File Offset: 0x00004514
		public void Execute(int i, int start)
		{
			Local local = new Local(this.method.Module.ImportAsTypeSig(typeof(int[])));
			this.method.Body.Variables.Add(local);
			int ldcI4Value = this.method.Body.Instructions[i].GetLdcI4Value();
			this.method.Body.Instructions[i].OpCode = OpCodes.Ldloc_S;
			this.method.Body.Instructions[i].Operand = local;
			this.method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4_0.ToInstruction());
			this.method.Body.Instructions.Insert(i + 2, OpCodes.Ldelem_I4.ToInstruction());
			this.method.Body.Instructions.Insert(start, OpCodes.Ldc_I4_1.ToInstruction());
			this.method.Body.Instructions.Insert(++start, OpCodes.Newarr.ToInstruction(this.method.Module.CorLibTypes.Int32));
			this.method.Body.Instructions.Insert(++start, OpCodes.Dup.ToInstruction());
			this.method.Body.Instructions.Insert(++start, OpCodes.Ldc_I4_0.ToInstruction());
			this.method.Body.Instructions.Insert(++start, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
			this.method.Body.Instructions.Insert(++start, OpCodes.Stelem_I4.ToInstruction());
			this.method.Body.Instructions.Insert(++start, OpCodes.Stloc_S.ToInstruction(local));
		}
	}
}
