using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;
using EXGuard.Runtime.Execution.Internal;

namespace EXGuard.Runtime.VCalls {
	internal class Unbox : IVCall {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.VCALL_UNBOX; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var typeSlot = ctx.Stack[sp--];
			var valSlot = ctx.Stack[sp];

			bool unboxPtr = (typeSlot.U4 & 0x80000000) != 0;
			var valType = (Type)ctx.Instance.Data.LookupReference(typeSlot.U4 & ~0x80000000);
			if (unboxPtr) {
				unsafe {
					TypedReference typedRef;
					TypedReferenceHelpers.UnboxTypedRef(valSlot.O, &typedRef);
					var reference = new TypedRef(typedRef);
					valSlot = VMSlot.FromObject(valSlot.O, valType);
					ctx.Stack[sp] = valSlot;
				}
			}
			else {
				if (valType == typeof(object) && valSlot.O != null)
					valType = valSlot.O.GetType();
				valSlot = VMSlot.FromObject(valSlot.O, valType);
				ctx.Stack[sp] = valSlot;
			}

			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;
			state = ExecutionState.Next;
		}
	}
}