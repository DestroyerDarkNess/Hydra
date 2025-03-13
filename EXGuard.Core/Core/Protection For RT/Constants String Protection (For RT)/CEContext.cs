using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.RT;
using EXGuard.DynCipher;
using EXGuard.Core.Services;
using EXGuard.Core.Helpers.System;

namespace EXGuard.Core.RTProtections.Constants
{
	public class CEContext
	{
        public VMRuntime VMRuntime
		{
			get;
			set;
		}

		public ModuleDef Module
		{
			get;
			set;
		}

		public ModuleWriterOptions Options
		{
			get;
			set;
		}

		public MethodDef InitMethod
		{
			get;
			set;
		}

		public int DecoderCount
		{
			get;
			set;
		}

		public CompressionService Compressor
		{
			get;
			set;
		}

		public List<DecoderInfo> Decoders
		{
			get;
			set;
		}

		public List<uint> EncodedBuffer
		{
			get;
			set;
		}

		public DynamicMode ModeHandler
		{
			get;
			set;
		}

		public DynCipherService DynCipher
		{
			get;
			set;
		}

		public RandomGenerator Random
		{
			get;
			set;
		}

		public Dictionary<MethodDef, List<ReplaceableInstructionReference>> ReferenceRepl
		{
			get;
			set;
		}
	}

	public class DecoderDesc
	{
		public object Data;
		public byte InitializerID;
		public byte NumberID;
		public byte StringID;
	}
}
