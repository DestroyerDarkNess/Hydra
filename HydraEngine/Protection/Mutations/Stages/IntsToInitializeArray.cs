using System;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace HydraEngine.Protection.Mutations.Stages
{
	// Token: 0x02000018 RID: 24
	public class IntsToInitializeArray
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00006513 File Offset: 0x00004713
		// (set) Token: 0x06000060 RID: 96 RVA: 0x0000651B File Offset: 0x0000471B
		private MethodDef method { get; set; }

		// Token: 0x06000061 RID: 97 RVA: 0x00006524 File Offset: 0x00004724
		public IntsToInitializeArray(MethodDef method)
		{
			this.method = method;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00006538 File Offset: 0x00004738
		public void Execute()
		{
			bool flag = this.method.Name != "button1_Click";
			if (!flag)
			{
				ITypeDefOrRef baseType = this.method.Module.ImportAsTypeSig(typeof(ValueType)).ToTypeDefOrRef();
				TypeDef typeDef = new TypeDefUser("'__StaticArrayInitTypeSize=24'", baseType);
				typeDef.Attributes |= (TypeAttributes.ExplicitLayout | TypeAttributes.Sealed);
				typeDef.ClassLayout = new ClassLayoutUser(1, 24U);
				this.method.Module.Types.Add(typeDef);
				FieldDef fieldDef = new FieldDefUser("'$$method0x6000003-1'", new FieldSig(typeDef.ToTypeSig(true)), FieldAttributes.FieldAccessMask | FieldAttributes.Static | FieldAttributes.HasFieldRVA)
				{
					InitialValue = new byte[]
					{
						1,
						0,
						0,
						0,
						2,
						0,
						0,
						0,
						3,
						0,
						0,
						0,
						4,
						0,
						0,
						0,
						5,
						0,
						0,
						0,
						6,
						0,
						0,
						0
					}
				};
				this.method.Module.GlobalType.Fields.Add(fieldDef);
				int num = 0;
				Local local = new Local(this.method.Module.ImportAsTypeSig(typeof(int[])));
				this.method.Body.Variables.Add(local);
				this.method.Body.Instructions.Insert(num, OpCodes.Ldc_I4_6.ToInstruction());
				this.method.Body.Instructions.Insert(++num, OpCodes.Newarr.ToInstruction(this.method.Module.CorLibTypes.Int32));
				this.method.Body.Instructions.Insert(++num, OpCodes.Dup.ToInstruction());
				this.method.Body.Instructions.Insert(++num, OpCodes.Ldtoken.ToInstruction(fieldDef));
				this.method.Body.Instructions.Insert(++num, OpCodes.Call.ToInstruction(this.method.Module.Import(typeof(RuntimeHelpers).GetMethod("InitializeArray", new Type[]
				{
					typeof(Array),
					typeof(RuntimeFieldHandle)
				}))));
				this.method.Body.Instructions.Insert(num + 1, OpCodes.Stloc_S.ToInstruction(local));
				this.method.Body.UpdateInstructionOffsets();
				this.method.Body.OptimizeBranches();
				this.method.Body.OptimizeMacros();
			}
		}
	}
}
