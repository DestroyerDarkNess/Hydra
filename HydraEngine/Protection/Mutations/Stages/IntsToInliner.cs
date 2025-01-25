using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x02000019 RID: 25
	public class IntsToInliner
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000063 RID: 99 RVA: 0x000067CF File Offset: 0x000049CF
		// (set) Token: 0x06000064 RID: 100 RVA: 0x000067D7 File Offset: 0x000049D7
		public MethodDef method { get; set; }

		// Token: 0x06000065 RID: 101 RVA: 0x000067E0 File Offset: 0x000049E0
		public IntsToInliner(MethodDef method)
		{
			this.method = method;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x000067F4 File Offset: 0x000049F4
		public void Execute(int i, int start)
		{
			Local local = new Local(this.method.Module.ImportAsTypeSig(typeof(int)));
			this.method.Body.Variables.Add(local);
			int ldcI4Value = this.method.Body.Instructions[i].GetLdcI4Value();
			this.method.Body.Instructions[i].OpCode = OpCodes.Ldloc;
			this.method.Body.Instructions[i].Operand = local;
			Instruction instruction = OpCodes.Nop.ToInstruction();
			this.method.Body.Instructions.Insert(start, Instruction.CreateLdcI4(IntsToInliner.rnd.Next(100, 500)));
			this.method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Stloc, local));
			this.method.Body.Instructions.Insert(++start, Instruction.CreateLdcI4(IntsToInliner.rnd.Next(1000, 5000)));
			this.method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Ldloc, local));
			this.method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Ceq));
			this.method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Brtrue, instruction));
			this.method.Body.Instructions.Insert(++start, Instruction.CreateLdcI4(ldcI4Value));
			this.method.Body.Instructions.Insert(++start, Instruction.Create(OpCodes.Stloc, local));
			this.method.Body.Instructions.Insert(++start, instruction);
		}

		// Token: 0x04000023 RID: 35
		private static Random rnd = new Random();
	}
}
