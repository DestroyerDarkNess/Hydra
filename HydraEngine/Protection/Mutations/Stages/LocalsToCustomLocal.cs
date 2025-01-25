using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x0200001E RID: 30
	public class LocalsToCustomLocal
	{
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000087 RID: 135 RVA: 0x00007C57 File Offset: 0x00005E57
		// (set) Token: 0x06000088 RID: 136 RVA: 0x00007C5F File Offset: 0x00005E5F
		private MethodDef method { get; set; }

		// Token: 0x06000089 RID: 137 RVA: 0x00007C68 File Offset: 0x00005E68
		public LocalsToCustomLocal(MethodDef method)
		{
			this.method = method;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00007C7C File Offset: 0x00005E7C
		public void Execute(ref int i)
		{
			int num = LocalsToCustomLocal.rnd.Next(0, 2);
			int num2 = num;
			if (num2 != 0)
			{
				if (num2 == 1)
				{
					this.RefLocal(ref i);
				}
			}
			else
			{
				this.PointerLocal(ref i);
			}
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00007CB8 File Offset: 0x00005EB8
		public void RefLocal(ref int i)
		{
			Local local = new Local(this.method.Module.ImportAsTypeSig(typeof(int)));
			this.method.Body.Variables.Add(local);
			int ldcI4Value = this.method.Body.Instructions[i].GetLdcI4Value();
			this.method.Body.Instructions[i].OpCode = OpCodes.Ldloc_S;
			this.method.Body.Instructions[i].Operand = local;
			this.method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(LocalsToCustomLocal.rnd.Next(100, 200)));
			this.method.Body.Instructions.Insert(1, OpCodes.Stloc_S.ToInstruction(local));
			this.method.Body.Instructions.Insert(2, OpCodes.Ldloca_S.ToInstruction(local));
			this.method.Body.Instructions.Insert(3, OpCodes.Mkrefany.ToInstruction(this.method.Module.CorLibTypes.Int32));
			this.method.Body.Instructions.Insert(4, OpCodes.Refanyval.ToInstruction(this.method.Module.CorLibTypes.Int32));
			this.method.Body.Instructions.Insert(5, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
			this.method.Body.Instructions.Insert(6, OpCodes.Stind_I4.ToInstruction());
			i += 7;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00007E84 File Offset: 0x00006084
		public unsafe void PointerLocal(ref int i)
		{
			int ldcI4Value = this.method.Body.Instructions[i].GetLdcI4Value();
			Local local = new Local(this.method.Module.ImportAsTypeSig(typeof(int)));
			Local local2 = new Local(this.method.Module.ImportAsTypeSig(typeof(int*)));
			this.method.Body.Variables.Add(local);
			this.method.Body.Variables.Add(local2);
			this.method.Body.Instructions[i] = OpCodes.Ldloc_S.ToInstruction(local);
			this.method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(LocalsToCustomLocal.rnd.Next()));
			this.method.Body.Instructions.Insert(1, OpCodes.Stloc_S.ToInstruction(local));
			this.method.Body.Instructions.Insert(2, OpCodes.Ldloca_S.ToInstruction(local));
			this.method.Body.Instructions.Insert(3, OpCodes.Conv_U.ToInstruction());
			this.method.Body.Instructions.Insert(4, OpCodes.Stloc_S.ToInstruction(local2));
			this.method.Body.Instructions.Insert(5, OpCodes.Ldloc_S.ToInstruction(local2));
			this.method.Body.Instructions.Insert(6, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
			this.method.Body.Instructions.Insert(7, OpCodes.Stind_I4.ToInstruction());
			i += 8;
		}

		// Token: 0x0400002E RID: 46
		private static Random rnd = new Random();
	}
}
