using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.Mutations;
using HydraEngine.Protection.Proxy;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x0200001F RID: 31
	public class MethodPreparation
	{
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00008069 File Offset: 0x00006269
		// (set) Token: 0x0600008F RID: 143 RVA: 0x00008071 File Offset: 0x00006271
		private MethodDef Method { get; set; }

		// Token: 0x06000090 RID: 144 RVA: 0x0000807A File Offset: 0x0000627A
		public MethodPreparation(MethodDef method)
		{
			this.Method = method;
		}

		// Token: 0x06000091 RID: 145 RVA: 0x0000808C File Offset: 0x0000628C
		public void Execute()
		{
			bool flag = this.hasInts();
			if (flag)
			{
				this.Ints();
			}
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000080B0 File Offset: 0x000062B0
		private bool hasInts()
		{
			for (int i = 0; i < this.Method.Body.Instructions.Count; i++)
			{
				bool flag = this.Method.Body.Instructions[i].IsLdcI4();
				if (flag)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x0000810C File Offset: 0x0000630C
		private void Ints()
		{
			for (int i = 0; i < this.Method.Body.Instructions.Count; i++)
			{
				bool flag = this.Method.Body.Instructions[i].IsLdcI4() && MutationHelper.CanObfuscate(this.Method.Body.Instructions, i);
				if (flag)
				{
					switch (MethodPreparation.rnd.Next(0, 10))
					{
					case 1:
						this.ConvToField(ref i);
						break;
					case 2:
						this.ConvToLocal(ref i);
						break;
					case 3:
						this.ConvToFieldModule(ref i);
						break;
					}
				}
			}
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000081D0 File Offset: 0x000063D0
		private void ConvToField(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			FieldDef fieldDef = new FieldDefUser(Helper.InvisibleName, new FieldSig(this.Method.Module.CorLibTypes.Int32), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
			this.Method.DeclaringType.Fields.Add(fieldDef);
			this.Method.Body.Instructions.Insert(0, Instruction.CreateLdcI4(ldcI4Value));
			this.Method.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, fieldDef));
			i += 2;
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldsfld;
			this.Method.Body.Instructions[i].Operand = fieldDef;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x000082C4 File Offset: 0x000064C4
		private void ConvToFieldModule(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			MethodDef methodDef = this.Method.Module.GlobalType.FindOrCreateStaticConstructor();
			FieldDef fieldDef = new FieldDefUser(Helper.InvisibleName, new FieldSig(this.Method.Module.CorLibTypes.Int32), FieldAttributes.FamANDAssem | FieldAttributes.Family | FieldAttributes.Static);
			this.Method.Module.GlobalType.Fields.Add(fieldDef);
			methodDef.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(MethodPreparation.rnd.Next()));
			methodDef.Body.Instructions.Insert(1, OpCodes.Stsfld.ToInstruction(fieldDef));
			this.Method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
			this.Method.Body.Instructions.Insert(1, OpCodes.Stsfld.ToInstruction(fieldDef));
			i += 2;
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldsfld;
			this.Method.Body.Instructions[i].Operand = fieldDef;
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00008418 File Offset: 0x00006618
		private void ConvToLocal(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			Local local = new Local(this.Method.Module.ImportAsTypeSig(typeof(int)));
			this.Method.Body.Variables.Add(local);
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldloc_S;
			this.Method.Body.Instructions[i].Operand = local;
			this.Method.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(ldcI4Value));
			this.Method.Body.Instructions.Insert(1, OpCodes.Stloc_S.ToInstruction(local));
			i += 2;
		}

		// Token: 0x04000030 RID: 48
		private static Random rnd = new Random();
	}
}
