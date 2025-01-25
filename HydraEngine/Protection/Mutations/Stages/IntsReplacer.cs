using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.Proxy;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x02000016 RID: 22
	public class IntsReplacer
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000056 RID: 86 RVA: 0x000060B1 File Offset: 0x000042B1
		// (set) Token: 0x06000057 RID: 87 RVA: 0x000060B9 File Offset: 0x000042B9
		public MethodDef Method { get; set; }

		// Token: 0x06000058 RID: 88 RVA: 0x000060C2 File Offset: 0x000042C2
		public IntsReplacer(MethodDef method)
		{
			this.Method = method;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000060D4 File Offset: 0x000042D4
		public void Execute()
		{
			MethodImplAttributes implFlags = MethodImplAttributes.IL;
			MethodAttributes flags = MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.HideBySig;
			MethodDefUser methodDefUser = new MethodDefUser(Helper.InvisibleName, MethodSig.CreateStatic(this.Method.Module.CorLibTypes.Int32, this.Method.Module.CorLibTypes.Double), implFlags, flags)
			{
				Body = new CilBody()
			};
			methodDefUser.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Call.ToInstruction(this.Method.Module.Import(typeof(Math).GetMethod("Sqrt", new Type[]
			{
				typeof(double)
			}))));
			methodDefUser.Body.Instructions.Add(OpCodes.Conv_I4.ToInstruction());
			methodDefUser.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			this.Method.Module.GlobalType.Methods.Add(methodDefUser);
			for (int i = 0; i < this.Method.Body.Instructions.Count; i++)
			{
				bool flag = this.Method.Body.Instructions[i].IsLdcI4();
				if (flag)
				{
					this.MathReplacer(methodDefUser, ref i);
				}
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x0000624C File Offset: 0x0000444C
		public void MathReplacer(MethodDef method, ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			double num = (double)ldcI4Value * (double)ldcI4Value;
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_R8;
			this.Method.Body.Instructions[i].Operand = num;
			IList<Instruction> instructions = this.Method.Body.Instructions;
			int num2 = i + 1;
			i = num2;
			instructions.Insert(num2, OpCodes.Call.ToInstruction(method));
			i++;
		}
	}
}
