using System;
using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime.OpCodes {
	internal class DivDword : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_DIV_DWORD; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var op1Slot = ctx.Stack[sp - 1];
			var op2Slot = ctx.Stack[sp];
			sp -= 1;
			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var fl = ctx.Registers[ctx.Data.Constants.REG_FL].U1;

			var slot = new VMSlot();
			if ((fl & ctx.Data.Constants.FL_UNSIGNED) != 0)
				slot.U4 = op1Slot.U4 / op2Slot.U4;
			else
				slot.U4 = (uint)((int)op1Slot.U4 / (int)op2Slot.U4);
			ctx.Stack[sp] = slot;

			byte mask = (byte)(ctx.Data.Constants.FL_ZERO | ctx.Data.Constants.FL_SIGN | ctx.Data.Constants.FL_UNSIGNED);
			Utils.UpdateFL(ctx, op1Slot.U4, op2Slot.U4, slot.U4, slot.U4, ref fl, mask);
			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = fl;

			state = ExecutionState.Next;
		}
	}

	internal class DivQword : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_DIV_QWORD; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var op1Slot = ctx.Stack[sp - 1];
			var op2Slot = ctx.Stack[sp];
			sp -= 1;
			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var fl = ctx.Registers[ctx.Data.Constants.REG_FL].U1;

			var slot = new VMSlot();
			if ((fl & ctx.Data.Constants.FL_UNSIGNED) != 0)
				slot.U8 = op1Slot.U8 / op2Slot.U8;
			else
				slot.U8 = (uint)((int)op1Slot.U8 / (int)op2Slot.U8);
			ctx.Stack[sp] = slot;

			byte mask = (byte)(ctx.Data.Constants.FL_ZERO | ctx.Data.Constants.FL_SIGN | ctx.Data.Constants.FL_UNSIGNED);
			Utils.UpdateFL(ctx, op1Slot.U8, op2Slot.U8, slot.U8, slot.U8, ref fl, mask);
			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = fl;

			state = ExecutionState.Next;
		}
	}

	internal class DivR32 : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_DIV_R32; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var op1Slot = ctx.Stack[sp - 1];
			var op2Slot = ctx.Stack[sp];
			sp -= 1;
			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var slot = new VMSlot();
			slot.R4 = op1Slot.R4 / op2Slot.R4;
			ctx.Stack[sp] = slot;

			byte mask = (byte)(ctx.Data.Constants.FL_ZERO | ctx.Data.Constants.FL_SIGN);
			var fl = (byte)(ctx.Registers[ctx.Data.Constants.REG_FL].U1 & ~mask);
			if (slot.R4 == 0)
				fl |= ctx.Data.Constants.FL_ZERO;
			else if (slot.R4 < 0)
				fl |= ctx.Data.Constants.FL_SIGN;
			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = fl;

			state = ExecutionState.Next;
		}
	}

	internal class DivR64 : IOpCode {
		public byte Code {
			get { return VMInstance.STATIC_Instance.Data.Constants.OP_DIV_R64; }
		}

		public void Run(VMContext ctx, out ExecutionState state) {
			var sp = ctx.Registers[ctx.Data.Constants.REG_SP].U4;
			var op1Slot = ctx.Stack[sp - 1];
			var op2Slot = ctx.Stack[sp];
			sp -= 1;
			ctx.Stack.SetTopPosition(sp);
			ctx.Registers[ctx.Data.Constants.REG_SP].U4 = sp;

			var slot = new VMSlot();
			slot.R8 = op1Slot.R8 / op2Slot.R8;
			ctx.Stack[sp] = slot;

			byte mask = (byte)(ctx.Data.Constants.FL_ZERO | ctx.Data.Constants.FL_SIGN);
			var fl = (byte)(ctx.Registers[ctx.Data.Constants.REG_FL].U1 & ~mask);
			if (slot.R8 == 0)
				fl |= ctx.Data.Constants.FL_ZERO;
			else if (slot.R8 < 0)
				fl |= ctx.Data.Constants.FL_SIGN;
			ctx.Registers[ctx.Data.Constants.REG_FL].U1 = fl;

			state = ExecutionState.Next;
		}
	}
}