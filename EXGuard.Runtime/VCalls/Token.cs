using System;
using System.Reflection;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;
using EXGuard.Runtime.Execution.Internal;

namespace EXGuard.Runtime.VCalls {
	internal class Token : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_TOKEN; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var typeSlot = ctx.Stack[sp];

			var reference = ctx.Instance.Data.LookupReference(typeSlot.U4);
			if (reference is Type)
				typeSlot.O = ValueTypeBox.Box(((Type)reference).TypeHandle, typeof(RuntimeTypeHandle));
			else if (reference is MethodBase)
				typeSlot.O = ValueTypeBox.Box(((MethodBase)reference).MethodHandle, typeof(RuntimeMethodHandle));
			else if (reference is FieldInfo)
				typeSlot.O = ValueTypeBox.Box(((FieldInfo)reference).FieldHandle, typeof(RuntimeFieldHandle));
			ctx.Stack[sp] = typeSlot;

			state = ExecutionState.Next;
		}
	}
}