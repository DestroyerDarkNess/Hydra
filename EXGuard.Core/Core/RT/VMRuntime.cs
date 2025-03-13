using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.VM;
using EXGuard.Core.CFG;
using EXGuard.Core.AST;
using EXGuard.Core.JIT;
using EXGuard.Core.AST.IL;
using EXGuard.Core.Services;
using EXGuard.Core.Helpers;
using EXGuard.Core.RT.Mutation;
using EXGuard.Core.RTProtections;
using EXGuard.Core.Helpers.System;
using EXGuard.Core.RTProtections.Constants;

namespace EXGuard.Core.RT {
	public class VMRuntime {
		internal Dictionary<MethodDef, Tuple<ScopeBlock, ILBlock>> MethodMap;
		List<Tuple<MethodDef, ILBlock>> BasicBlocks;

		List<IChunk> ExtraChunks;
		List<IChunk> FinalChunks;

        List<byte> __ILVDATA;

        public ModuleDefMD RTModule
        {
            get;
            private set;
        }

        public ModuleWriterOptions RTModuleWriterOptions
        {
            get;
            private set;
        }

        public CompressionService CompressionService
        {
            get;
            private set;
        }

        public Virtualizer Virtualizer
        {
            get;
            private set;
        }

        public List<JITEDMethodInfo> JITMethods
        {
            get;
            private set;
        }

        public VMDescriptor Descriptor
        {
            get;
            private set;
        }

        internal BasicBlockSerializer Serializer
        {
            get;
            private set;
        }

        internal RuntimeMutator RTMutator
        {
            get;
            private set;
        }

        internal RuntimeSearch RTSearch
        {
            get;
            set;
        }

        internal NameService RNMService
        {
            get;
            private set;
        }

        internal MemoryStream RuntimeLibrary
        {
            get;
            set;
        }

        public double EncryptionKey
        {
            get;
            private set;
        }

        public ILVDynamicDeriver __ILVDATA_Deriver
        {
            get;
            private set;
        }

        public static string Watermark { get; set; } = "Discord: user76";

        public VMRuntime(Virtualizer vr, ModuleDef rt) {
            MethodMap = new Dictionary<MethodDef, Tuple<ScopeBlock, ILBlock>>();
            BasicBlocks = new List<Tuple<MethodDef, ILBlock>>();

            ExtraChunks = new List<IChunk>();
            FinalChunks = new List<IChunk>();

            __ILVDATA = new List<byte>();

            EncryptionKey = (double)(new RandomGenerator().NextDouble() / new RandomGenerator().NextInt32());

            RTModule = (ModuleDefMD)rt;
            Virtualizer = vr;

            #region Runtime ModuleWriterOptions
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            RTModuleWriterOptions = new ModuleWriterOptions(RTModule)
            {
                Logger = DummyLogger.NoThrowInstance,
                PdbOptions = PdbWriterOptions.None,
                WritePdb = false
            };
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            CompressionService = new CompressionService();

            JITMethods = new List<JITEDMethodInfo>();
            Descriptor = new VMDescriptor(vr);
            Serializer = new BasicBlockSerializer(this);
            RTMutator = new RuntimeMutator(this);
            RTSearch = new RuntimeSearch(rt, this).Search();
            RNMService = new NameService(rt);
            __ILVDATA_Deriver = new ILVDynamicDeriver();
        }

        public void AddMethod(MethodDef method, ScopeBlock rootScope) {
			ILBlock entry = null;

			foreach (ILBlock block in rootScope.GetBasicBlocks()) {
				if (block.Id == 0)
					entry = block;
				BasicBlocks.Add(Tuple.Create(method, block));
			}

			Debug.Assert(entry != null);
			MethodMap[method] = Tuple.Create(rootScope, entry);           
        }

		internal void AddHelper(MethodDef method, ScopeBlock rootScope, ILBlock entry) {
			MethodMap[method] = Tuple.Create(rootScope, entry);
		}

		public void AddBlock(MethodDef method, ILBlock block) {
			BasicBlocks.Add(Tuple.Create(method, block));
		}

		public ScopeBlock LookupMethod(MethodDef method) {
			var m = MethodMap[method];
			return m.Item1;
		}

		public ScopeBlock LookupMethod(MethodDef method, out ILBlock entry) {
			var m = MethodMap[method];
			entry = m.Item2;
			return m.Item1;
		}

		public void AddChunk(IChunk chunk) {
			ExtraChunks.Add(chunk);
		}

		public void ExportMethod(MethodDef method, MDToken mdToken) {
            Descriptor.Data.ReadExportMDToken(method, mdToken);
            MethodPatcher.Patch(RTSearch, method);
        }

        public void OnKoiRequested()
        {
            var header = new HeaderChunk(this);

            foreach (var block in BasicBlocks)
                FinalChunks.Add(block.Item2.CreateChunk(this, block.Item1));

            FinalChunks.AddRange(ExtraChunks);
            Descriptor.RandomGenerator.Shuffle(FinalChunks);
            FinalChunks.Insert(0, header);

            ComputeOffsets();
            FixupReferences();

            header.WriteData(this);

            #region ***********| Create ILVData |***********
            ///////////////////////////////////////////////////////
            List<byte> Data = new List<byte>();

            foreach (var chunk in FinalChunks)
                Data.AddRange(chunk.GetData());
            ///////////////////////////////////////////////////////
            #endregion

            #region ***********| Write Encrypted And Compressed DATA To __ILVDATA LIST |***********
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////
            __ILVDATA_Deriver.Initialize(this);

            var Encrypted_ILVDATA = __ILVDATA_Deriver.Encrypt(Data.ToArray(), 0);

            for (var l = 0; l < Encrypted_ILVDATA.Length; l++)
                __ILVDATA.Add(Encrypted_ILVDATA[l]);
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            #region Encryption Key Write Utils.Decrypt
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            var EncryptionKeyBuffer = BitConverter.GetBytes(EncryptionKey);

            MutationHelper.InjectKeys_Int(RTSearch.Utils_Decrypt, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 },
                new int[] { EncryptionKeyBuffer[0], EncryptionKeyBuffer[1], EncryptionKeyBuffer[2],
                EncryptionKeyBuffer[3], EncryptionKeyBuffer[4], EncryptionKeyBuffer[5],
                EncryptionKeyBuffer[6], EncryptionKeyBuffer[7] });
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            #region Data Counts Write VMData.ctor
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            MutationHelper.InjectKeys_Int(RTSearch.VMData_Ctor, new int[] { 2, 3, 4 }, new int[] {
                Descriptor.Data.strMap.Count, Descriptor.Data.refMap.Count, Descriptor.Data.sigs.Count });
            /////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            #region Write VMData to VMData.ctor
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var DataType = new TypeDefUser(RNMService.NewName(Descriptor.RandomGenerator.NextString()),
                RTModule.CorLibTypes.GetTypeRef("System", "ValueType"))
            {
                Layout = dnlib.DotNet.TypeAttributes.ExplicitLayout,
                Visibility = dnlib.DotNet.TypeAttributes.Sealed,
                IsSealed = true,

                ClassLayout = new ClassLayoutUser(0, (uint)__ILVDATA.Count)
            };

            RTModule.Types.Add(DataType);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var FieldWithRVA = new FieldDefUser(RNMService.NewName(Descriptor.RandomGenerator.NextString()),
                new FieldSig(DataType.ToTypeSig()),
                dnlib.DotNet.FieldAttributes.Private | dnlib.DotNet.FieldAttributes.Static | dnlib.DotNet.FieldAttributes.HasFieldRVA)
            {
                HasFieldRVA = true,
                InitialValue = __ILVDATA.ToArray()
            };

            RTSearch.VMData.Fields.Add(FieldWithRVA);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            MutationHelper.InjectKey_Int(RTSearch.VMData_Ctor, 0, __ILVDATA.Count / 4);
            MutationHelper.GetInstructionsLocationIndex(RTSearch.VMData_Ctor, true, out var index);

            RTSearch.VMData_Ctor.Body.Instructions.Insert(index, Instruction.Create(OpCodes.Ldtoken, FieldWithRVA));
            RTSearch.VMData_Ctor.Body.Instructions.Insert(index + 1, Instruction.Create(OpCodes.Call, RTModule.Import(RTSearch.FieldInfo_GetFieldFromHandle_1)));
            RTSearch.VMData_Ctor.Body.Instructions.Insert(index + 2, Instruction.Create(OpCodes.Callvirt, RTModule.Import(RTSearch.FieldInfo_get_FieldHandle)));
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            #region Write Mutation.Crypt (For Encrypted And Compressed Data)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            MutationHelper.InjectKey_Int(RTSearch.VMData_Ctor, 1, __ILVDATA_Deriver.Seed); // Write Seed

            RTSearch.VMData_Ctor.Body.SimplifyMacros(RTSearch.VMData_Ctor.Parameters);

            List<Instruction> instrs = RTSearch.VMData_Ctor.Body.Instructions.ToList();
            for (int i = 0; i < instrs.Count; i++)
            {
                Instruction instr = instrs[i];
                var method = instr.Operand as IMethod;
                if (instr.OpCode == OpCodes.Call)
                {
                    if (method.DeclaringType.Name == RTMap.Mutation &&
                        method.Name == RTMap.Mutation_Crypt)
                    {
                        Instruction ldBlock = instrs[i - 2];
                        Instruction ldKey = instrs[i - 1];

                        instrs.RemoveAt(i);
                        instrs.RemoveAt(i - 1);
                        instrs.RemoveAt(i - 2);

                        instrs.InsertRange(i - 2, __ILVDATA_Deriver.EmitDecrypt(RTSearch.VMData_Ctor, (Local)ldBlock.Operand, (Local)ldKey.Operand));
                    }
                }
            }

            RTSearch.VMData_Ctor.Body.Instructions.Clear();

            foreach (Instruction instr in instrs)
                RTSearch.VMData_Ctor.Body.Instructions.Add(instr);
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            #region End Protection (For RT)
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #region All String Encryption (ConfuserEX Constants Protection (Modded)
            /////////////////////////////////////////////////////////////////
            new ConstantsProtection().Execute(RTModule, this);
            /////////////////////////////////////////////////////////////////
            #endregion

            #region Anti De4dot
            ////////////////////////////////////////////
            Anti_De4dot.Execute(RTModule, Watermark);
            ////////////////////////////////////////////
            #endregion
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion
        }

        void ComputeOffsets() {
			uint offset = 0;
			foreach (var chunk in FinalChunks) {
				chunk.OnOffsetComputed(offset);
				offset += chunk.Length;
			}
		}

		void FixupReferences() {
			foreach (var block in BasicBlocks) {
				foreach (var instr in block.Item2.Content) {
					if (instr.Operand is ILRelReference) {
						var reference = (ILRelReference)instr.Operand;
						instr.Operand = ILImmediate.Create(reference.Resolve(this), ASTType.I4);
					}
				}
			}
		}

		public void ResetData() {
			MethodMap = new Dictionary<MethodDef, Tuple<ScopeBlock, ILBlock>>();
			BasicBlocks = new List<Tuple<MethodDef, ILBlock>>();

			ExtraChunks = new List<IChunk>();
			FinalChunks = new List<IChunk>();

			Descriptor.ResetData();
		}
	}
}