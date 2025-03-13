using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class NorDword : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_NOR_DWORD; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var op1Slot = ctx.Stack[sp - 1];
			var op2Slot = ctx.Stack[sp];
			sp -= 1;
			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var slot = new VMSlot();
			slot.U4 = ~(op1Slot.U4 | op2Slot.U4);
			ctx.Stack[sp] = slot;

			byte mask = (byte)(ctx.Data.Constants.FL_ZERO | ctx.Data.Constants.FL_SIGN);
			var fl = ctx.Registers[ctx.Data.Constants.REG_FL].U1;
			Utils.UpdateFL(ctx, op1Slot.U4, op2Slot.U4, slot.U4, slot.U4, ref fl, mask);
			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = fl;

			state = ExecutionState.Next;
		}
	}

	internal class NorQword : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_NOR_QWORD; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var op1Slot = ctx.Stack[sp - 1];
			var op2Slot = ctx.Stack[sp];
			sp -= 1;
			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var slot = new VMSlot();
			slot.U8 = ~(op1Slot.U8 | op2Slot.U8);
			ctx.Stack[sp] = slot;

			byte mask = (byte)(ctx.Data.Constants.FL_ZERO | ctx.Data.Constants.FL_SIGN);
			var fl = ctx.Registers[ctx.Data.Constants.REG_FL].U1;
			Utils.UpdateFL(ctx, op1Slot.U8, op2Slot.U8, slot.U8, slot.U8, ref fl, mask);
			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = fl;

			state = ExecutionState.Next;
		}
	}
}