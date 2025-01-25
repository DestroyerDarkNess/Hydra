using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x0200001D RID: 29
	internal class IntsToStackalloc
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000081 RID: 129 RVA: 0x0000795A File Offset: 0x00005B5A
		// (set) Token: 0x06000082 RID: 130 RVA: 0x00007962 File Offset: 0x00005B62
		private MethodDef method { get; set; }

		// Token: 0x06000083 RID: 131 RVA: 0x0000796B File Offset: 0x00005B6B
		public IntsToStackalloc(MethodDef method)
		{
			this.method = method;
			this.Create();
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00007998 File Offset: 0x00005B98
		public unsafe void Create()
		{
			this.local = new Local(this.method.Module.Import(typeof(int*)).ToTypeSig(true));
			this.method.Body.Variables.Add(this.local);
			this.toInject.Add(OpCodes.Ldc_I4.ToInstruction(4));
			this.toInject.Add(OpCodes.Conv_U.ToInstruction());
			this.toInject.Add(OpCodes.Localloc.ToInstruction());
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00007A30 File Offset: 0x00005C30
		public void Execute(ref int i)
		{
			int ldcI4Value = this.method.Body.Instructions[i].GetLdcI4Value();
			this.toInject.Add(OpCodes.Dup.ToInstruction());
			this.toInject.Add(OpCodes.Ldc_I4.ToInstruction(this.offset * 4));
			this.toInject.Add(OpCodes.Add.ToInstruction());
			this.toInject.Add(OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
			this.toInject.Add(OpCodes.Stind_I4.ToInstruction());
			this.method.Body.Instructions[i] = OpCodes.Ldloc_S.ToInstruction(this.local);
			IList<Instruction> instructions = this.method.Body.Instructions;
			int num = i + 1;
			i = num;
			int index = num;
			OpCode ldc_I = OpCodes.Ldc_I4;
			num = this.offset;
			this.offset = num + 1;
			instructions.Insert(index, ldc_I.ToInstruction(num * 4));
			IList<Instruction> instructions2 = this.method.Body.Instructions;
			num = i + 1;
			i = num;
			instructions2.Insert(num, OpCodes.Add.ToInstruction());
			IList<Instruction> instructions3 = this.method.Body.Instructions;
			num = i + 1;
			i = num;
			instructions3.Insert(num, OpCodes.Ldind_I4.ToInstruction());
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00007B88 File Offset: 0x00005D88
		public void Inject()
		{
			int num = this.offset * 4;
			bool flag = num != 0;
			if (flag)
			{
				this.toInject[0] = OpCodes.Ldc_I4.ToInstruction(num);
				bool flag2 = this.toInject.Last<Instruction>().OpCode == OpCodes.Dup;
				if (flag2)
				{
					this.toInject.RemoveAt(this.toInject.Count - 1);
				}
				this.toInject.Add(OpCodes.Stloc_S.ToInstruction(this.local));
				for (int i = 0; i < this.toInject.Count; i++)
				{
					this.method.Body.Instructions.Insert(i, this.toInject[i]);
				}
			}
		}

		// Token: 0x0400002A RID: 42
		private Local local;

		// Token: 0x0400002B RID: 43
		private List<Instruction> toInject = new List<Instruction>();

		// Token: 0x0400002C RID: 44
		private int offset = 0;
	}
}
