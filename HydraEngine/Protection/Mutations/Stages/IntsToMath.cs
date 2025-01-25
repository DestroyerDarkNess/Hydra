using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using HydraEngine.Protection.Mutations;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x0200001A RID: 26
	public class IntsToMath
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00006A0B File Offset: 0x00004C0B
		// (set) Token: 0x06000069 RID: 105 RVA: 0x00006A13 File Offset: 0x00004C13
		private MethodDef Method { get; set; }

		// Token: 0x0600006A RID: 106 RVA: 0x00006A1C File Offset: 0x00004C1C
		public IntsToMath(MethodDef method)
		{
			this.Method = method;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00006A30 File Offset: 0x00004C30
		public void Execute(ref int i)
		{
			switch (IntsToMath.rnd.Next(0, 7))
			{
			case 0:
				this.Neg(ref i);
				break;
			case 1:
				this.Not(ref i);
				break;
			case 2:
				this.Shr(ref i);
				break;
			case 3:
				this.Shl(ref i);
				break;
			case 4:
				this.Or(ref i);
				break;
			case 5:
				this.Rem(ref i);
				break;
			case 6:
				this.ConditionalMath(ref i);
				break;
			case 7:
				this.Add(ref i);
				break;
			case 8:
				this.Sub(ref i);
				break;
			case 9:
				this.Xor(ref i);
				break;
			}
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00006AE4 File Offset: 0x00004CE4
		private void Add(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int num2 = ldcI4Value - num;
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = num2;
			IList<Instruction> instructions = this.Method.Body.Instructions;
			int num3 = i + 1;
			i = num3;
			instructions.Insert(num3, OpCodes.Ldc_I4.ToInstruction(num));
			IList<Instruction> instructions2 = this.Method.Body.Instructions;
			num3 = i + 1;
			i = num3;
			instructions2.Insert(num3, OpCodes.Add.ToInstruction());
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00006BC0 File Offset: 0x00004DC0
		private void Xor(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int num2 = ldcI4Value ^ num;
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = num2;
			IList<Instruction> instructions = this.Method.Body.Instructions;
			int num3 = i + 1;
			i = num3;
			instructions.Insert(num3, OpCodes.Ldc_I4.ToInstruction(num));
			IList<Instruction> instructions2 = this.Method.Body.Instructions;
			num3 = i + 1;
			i = num3;
			instructions2.Insert(num3, OpCodes.Xor.ToInstruction());
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00006C9C File Offset: 0x00004E9C
		private void Sub(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int num2 = ldcI4Value + num;
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = num2;
			IList<Instruction> instructions = this.Method.Body.Instructions;
			int num3 = i + 1;
			i = num3;
			instructions.Insert(num3, OpCodes.Ldc_I4.ToInstruction(num));
			IList<Instruction> instructions2 = this.Method.Body.Instructions;
			num3 = i + 1;
			i = num3;
			instructions2.Insert(num3, OpCodes.Sub.ToInstruction());
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00006D78 File Offset: 0x00004F78
		private void Neg(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int value = -num;
			Calculator calculator = new Calculator(ldcI4Value, value);
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = calculator.getResult();
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(num));
			this.Method.Body.Instructions.Insert(i + 2, OpCodes.Neg.ToInstruction());
			this.Method.Body.Instructions.Insert(i + 3, calculator.getOpCode().ToInstruction());
			i += 3;
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00006E80 File Offset: 0x00005080
		private void Rem(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int num2 = IntsToMath.rnd.Next(10000, 50000);
			int value = num2 % num;
			Calculator calculator = new Calculator(ldcI4Value, value);
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = calculator.getResult();
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(num2));
			this.Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(num));
			this.Method.Body.Instructions.Insert(i + 3, OpCodes.Rem.ToInstruction());
			this.Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
			i += 4;
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00006FC8 File Offset: 0x000051C8
		private void Not(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int value = ~num;
			Calculator calculator = new Calculator(ldcI4Value, value);
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = calculator.getResult();
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(num));
			this.Method.Body.Instructions.Insert(i + 2, OpCodes.Not.ToInstruction());
			this.Method.Body.Instructions.Insert(i + 3, calculator.getOpCode().ToInstruction());
			i += 3;
		}

		// Token: 0x06000072 RID: 114 RVA: 0x000070D0 File Offset: 0x000052D0
		private void Shl(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int num2 = IntsToMath.rnd.Next(10000, 50000);
			int value = num2 << num;
			Calculator calculator = new Calculator(ldcI4Value, value);
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = calculator.getResult();
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(num2));
			this.Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(num));
			this.Method.Body.Instructions.Insert(i + 3, OpCodes.Shl.ToInstruction());
			this.Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
			i += 4;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00007218 File Offset: 0x00005418
		private void Or(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int num2 = IntsToMath.rnd.Next(10000, 50000);
			int value = num2 | num;
			Calculator calculator = new Calculator(ldcI4Value, value);
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = calculator.getResult();
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(num2));
			this.Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(num));
			this.Method.Body.Instructions.Insert(i + 3, OpCodes.Or.ToInstruction());
			this.Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
			i += 4;
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00007360 File Offset: 0x00005560
		private void Shr(ref int i)
		{
			int ldcI4Value = this.Method.Body.Instructions[i].GetLdcI4Value();
			int num = IntsToMath.rnd.Next(10000, 50000);
			int num2 = IntsToMath.rnd.Next(10000, 50000);
			int value = num2 >> num;
			Calculator calculator = new Calculator(ldcI4Value, value);
			this.Method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
			this.Method.Body.Instructions[i].Operand = calculator.getResult();
			this.Method.Body.Instructions.Insert(i + 1, OpCodes.Ldc_I4.ToInstruction(num2));
			this.Method.Body.Instructions.Insert(i + 2, OpCodes.Ldc_I4.ToInstruction(num));
			this.Method.Body.Instructions.Insert(i + 3, OpCodes.Shr.ToInstruction());
			this.Method.Body.Instructions.Insert(i + 4, calculator.getOpCode().ToInstruction());
			i += 4;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000074A8 File Offset: 0x000056A8
		private void ConditionalMath(ref int i)
		{
			Instruction instruction = this.Method.Body.Instructions[i];
			Local local = new Local(this.Method.Module.ImportAsTypeSig(typeof(int)));
			int ldcI4Value = instruction.GetLdcI4Value();
			int num = IntsToMath.rnd.Next();
			int num2 = IntsToMath.rnd.Next();
			bool flag = num > num2;
			int value;
			int value2;
			if (flag)
			{
				value = ldcI4Value;
				value2 = ldcI4Value + ldcI4Value / 3;
			}
			else
			{
				value2 = ldcI4Value;
				value = ldcI4Value + ldcI4Value / 3;
			}
			this.Method.Body.Variables.Add(local);
			instruction.OpCode = OpCodes.Ldc_I4;
			instruction.Operand = num2;
			this.Method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Ldc_I4, num));
			this.Method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Nop));
			this.Method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, value));
			this.Method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Nop));
			this.Method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Ldc_I4, value2));
			this.Method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Stloc, local));
			this.Method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Ldloc, local));
			this.Method.Body.Instructions[i + 2].OpCode = OpCodes.Bgt_S;
			this.Method.Body.Instructions[i + 2].Operand = this.Method.Body.Instructions[i + 5];
			this.Method.Body.Instructions[i + 4].OpCode = OpCodes.Br_S;
			this.Method.Body.Instructions[i + 4].Operand = this.Method.Body.Instructions[i + 6];
			i += 7;
		}

		// Token: 0x04000025 RID: 37
		private static Random rnd = new Random();
	}
}
