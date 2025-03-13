using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace EXGuard.Core.JIT
{
	internal class JITMethodBodyReader : MethodBodyWriterBase
	{
		readonly CilBody body;
		readonly bool keepMaxStack;
		readonly Metadata metadata;

		public byte[] ILCode
        {
			get;
			private set;
        }

		public uint ILCodeSize
		{
			get;
			private set;
		}

		public uint MaxStack
		{
			get;
			private set;
		}

		public JITMethodBodyReader(Metadata metadata, CilBody body, bool keepMaxStack) :
			base(body.Instructions, body.ExceptionHandlers)
		{
			this.body = body;
			this.keepMaxStack = keepMaxStack;
			this.metadata = metadata;
		}

		public void Read()
		{
			uint codeSize = InitializeInstructionOffsets();
			MaxStack = keepMaxStack ? body.MaxStack : GetMaxStack();

			var newCode = new byte[codeSize];
			var writer = new ArrayWriter(newCode);
			uint _codeSize = WriteInstructions(ref writer);

			Debug.Assert(codeSize == _codeSize);

			ILCode = newCode;
			ILCodeSize = codeSize;
		}

		protected override void WriteInlineField(ref ArrayWriter writer, Instruction instr)
		{
			writer.WriteUInt32(metadata.GetToken(instr.Operand).Raw);
		}

		protected override void WriteInlineMethod(ref ArrayWriter writer, Instruction instr)
		{
			writer.WriteUInt32(metadata.GetToken(instr.Operand).Raw);
		}

		protected override void WriteInlineSig(ref ArrayWriter writer, Instruction instr)
		{
			writer.WriteUInt32(metadata.GetToken(instr.Operand).Raw);
		}

		protected override void WriteInlineString(ref ArrayWriter writer, Instruction instr)
		{
			// Stringler "#US" datada yarrak gibi gözüktüğü için burası kapalı.
			// Eğer bu jiti saf jit yaparsan bunu açmalısın.

			//writer.WriteUInt32(metadata.GetToken(instr.Operand).Raw);
		}

		protected override void WriteInlineTok(ref ArrayWriter writer, Instruction instr)
		{
			writer.WriteUInt32(metadata.GetToken(instr.Operand).Raw);
		}

		protected override void WriteInlineType(ref ArrayWriter writer, Instruction instr)
		{
			writer.WriteUInt32(metadata.GetToken(instr.Operand).Raw);
		}
	}
}
