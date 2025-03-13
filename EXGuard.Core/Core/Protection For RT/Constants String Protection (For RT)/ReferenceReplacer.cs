using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.Helpers.System;
using EXGuard.Core.Helpers.System.Runtime.CompilerServices;

namespace EXGuard.Core.RTProtections.Constants
{
	public class ReferenceReplacer
	{
		[TupleElementNames(new string[] { "method", "indexInstruction", "ldci4Instruction" })]
		private static List<ValueTuple<MethodDef, Instruction, Instruction>> _keys = new List<ValueTuple<MethodDef, Instruction, Instruction>>();

		public static void ReplaceReference(CEContext ctx)
		{
			ctx.Options.WriterEvent += WriterEvent;

			foreach (var entry in ctx.ReferenceRepl)
			{
				EnsureNoInlining(entry.Key);
				ReplaceNormal(entry.Key, entry.Value, ctx);
			}
		}

		private static int CalculateStartIndex(IList<Instruction> instructions, int instructionIndex)
		{
			int index = 0;
			for (var i = 0; i < instructionIndex; i++)
			{
				var instr = instructions[i];
				index += instr.OpCode.Size;
				if (instr.OpCode.OperandType == OperandType.InlineNone)
					continue;

				switch (instr.OpCode.OperandType)
				{
					case OperandType.ShortInlineVar:
					case OperandType.ShortInlineBrTarget:
					case OperandType.ShortInlineI:
						index++;
						break;
					case OperandType.InlineSwitch:
						index += 4 + ((Instruction[])instr.Operand).Length * 4;
						break;
					case OperandType.InlineVar:
						index += 2;
						break;
					case OperandType.InlineType:
					case OperandType.InlineMethod:
					case OperandType.InlineSig:
					case OperandType.InlineTok:
					case OperandType.InlineI:
					case OperandType.ShortInlineR:
					case OperandType.InlineField:
					case OperandType.InlineString:
					case OperandType.InlineBrTarget:
						index += 4;
						break;
					case OperandType.InlineI8:
					case OperandType.InlineR:
						index += 8;
						break;
				}
			}
			return index;
		}

		private static void WriterEvent(object sender, ModuleWriterEventArgs e)
		{
			if (e.Event != ModuleWriterEvent.Begin)
				return;

			foreach (var pair in _keys)
			{
				IList<Instruction> instructions = pair.Item1.Body.Instructions;
				int index = CalculateStartIndex(instructions, instructions.IndexOf(pair.Item3));

				pair.Item2.Operand = index + 1;
			}
		}

		static void ReplaceNormal(MethodDef method, List<ReplaceableInstructionReference> references, CEContext ctx)
		{
			foreach (var reference in references)
			{
				var instructions = method.Body.Instructions;

				int i = instructions.IndexOf(reference.Target);
				reference.Target.OpCode = OpCodes.Ldc_I4;
				reference.Target.Operand = (int)reference.Id;

				//We have to ensure that the ldc.i4 cannot be represented as ldc.i4.s otherwise index calculation may fail.
				Instruction indexReference = Instruction.Create(OpCodes.Ldc_I4, byte.MaxValue + 1);
				instructions.Insert(i + 1, indexReference);
				instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldtoken, method));
				instructions.Insert(i + 3, Instruction.Create(OpCodes.Call, reference.Decoder));

				//Insert the key randomly. And calculate the index later on.
				Instruction ldci4Reference = Instruction.CreateLdcI4((int)reference.Key);
				instructions.Insert(0, ldci4Reference);
				instructions.Insert(1, Instruction.Create(OpCodes.Pop));

				_keys.Add(new ValueTuple<MethodDef, Instruction, Instruction>(method, indexReference, ldci4Reference));
			}
		}

		static void EnsureNoInlining(MethodDef method)
		{
			method.ImplAttributes &= ~MethodImplAttributes.AggressiveInlining;
			method.ImplAttributes |= MethodImplAttributes.NoInlining;
		}
	}
}
