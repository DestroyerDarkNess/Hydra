using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.Helpers;

namespace EXGuard.Core.RT.Mutation {
	internal class RuntimeMutator {
		Metadata RTMetadata;
		VMRuntime VMRT;
		internal RTConstants Constants;

        public RuntimeMutator(VMRuntime rt) {
			VMRT = rt;

            Constants = new RTConstants(rt.EncryptionKey);
            Constants.ReadConstants(rt.Descriptor);
        }

        public void MutateRuntime()
        {
            #region Patch And Repace RT
            ////////////////////////////////////////////
            RuntimePatcher.Patch(VMRT.RTModule);
            ////////////////////////////////////////////
            #endregion

            #region Read And Write RT OpCodes (Constants)
            ////////////////////////////////////////////////////////////
            Constants.ReadConstants(VMRT.Descriptor);
			////////////////////////////////////////////////////////////
			#endregion

			#region Renamer for RT
			/////////////////////////////
			VMRT.RNMService.Process();
			/////////////////////////////
			#endregion

			#region "Anti ILDasm" Protection for RT
			//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			TypeRef SuppressIldasmAttribute = VMRT.RTModule.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "SuppressIldasmAttribute");
			MemberRefUser SuppressIldasmAttribute_ctor = new MemberRefUser(VMRT.RTModule, ".ctor", MethodSig.CreateInstance(VMRT.RTModule.CorLibTypes.Void), SuppressIldasmAttribute);
			CustomAttribute SuppressIldasmAttribute_item = new CustomAttribute(SuppressIldasmAttribute_ctor);
			VMRT.RTModule.Assembly.CustomAttributes.Add(SuppressIldasmAttribute_item);
			VMRT.RTModule.CustomAttributes.Add(SuppressIldasmAttribute_item);


			TypeRef SuppressUnmanagedCodeSecurity = VMRT.RTModule.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "SuppressUnmanagedCodeSecurity");
			MemberRefUser SuppressUnmanagedCodeSecurity_ctor = new MemberRefUser(VMRT.RTModule, ".ctor", MethodSig.CreateInstance(VMRT.RTModule.CorLibTypes.Void), SuppressUnmanagedCodeSecurity);
			CustomAttribute SuppressUnmanagedCodeSecurity_item = new CustomAttribute(SuppressUnmanagedCodeSecurity_ctor);
			VMRT.RTModule.Assembly.CustomAttributes.Add(SuppressUnmanagedCodeSecurity_item);
			VMRT.RTModule.CustomAttributes.Add(SuppressUnmanagedCodeSecurity_item);


			TypeRef UnsafeValueTypeAttribute = VMRT.RTModule.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "UnsafeValueTypeAttribute");
			MemberRefUser UnsafeValueTypeAttribute_ctor = new MemberRefUser(VMRT.RTModule, ".ctor", MethodSig.CreateInstance(VMRT.RTModule.CorLibTypes.Void), UnsafeValueTypeAttribute);
			CustomAttribute UnsafeValueTypeAttribute_item = new CustomAttribute(UnsafeValueTypeAttribute_ctor);
			VMRT.RTModule.Assembly.CustomAttributes.Add(UnsafeValueTypeAttribute_item);
			VMRT.RTModule.CustomAttributes.Add(UnsafeValueTypeAttribute_item);


			TypeRef RuntimeWrappedException = VMRT.RTModule.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "RuntimeWrappedException");
			MemberRefUser RuntimeWrappedException_ctor = new MemberRefUser(VMRT.RTModule, ".ctor", MethodSig.CreateInstance(VMRT.RTModule.CorLibTypes.Void), RuntimeWrappedException);
			CustomAttribute RuntimeWrappedException_item = new CustomAttribute(RuntimeWrappedException_ctor);
			VMRT.RTModule.Assembly.CustomAttributes.Add(RuntimeWrappedException_item);
			VMRT.RTModule.CustomAttributes.Add(RuntimeWrappedException_item);
			//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			#endregion
		}

		public void CommitModule(ModuleDef module, Metadata metadata)
        {
            RTMetadata = metadata;

			ImportReferences(module);

            MutateMetadata();

            VMRT.OnKoiRequested();
            VMRT.ResetData(); // Reset VMData Settings
        }

		public void ImportEntryInitialize(ModuleDef module)
		{
			var method = module.GlobalType.FindOrCreateStaticConstructor();
			var get_Version = typeof(Environment).GetMethod("get_Version", BindingFlags.Public | BindingFlags.Static);

			method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldftn, method.Module.Import(get_Version)));
			method.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Calli, method.Module.Import(get_Version).MethodSig));

			method.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Ldftn, module.Import(VMRT.RTSearch.VMEntry_EntryInitialize)));
			method.Body.Instructions.Insert(3, Instruction.Create(OpCodes.Calli, module.Import(VMRT.RTSearch.VMEntry_EntryInitialize).MethodSig));

			// Old Hide Method
			//////////////////////////////////////////////////////////////////////////////////////////////////////
			method.Body.Instructions.Insert(1, new Instruction(OpCodes.Br_S, method.Body.Instructions[1]));
			method.Body.Instructions.Insert(2, new Instruction(OpCodes.Unaligned, (byte)0));
			//////////////////////////////////////////////////////////////////////////////////////////////////////
		}

		void ImportReferences(ModuleDef module) {
			var refCopy = VMRT.Descriptor.Data.refMap.ToList();
			VMRT.Descriptor.Data.refMap.Clear();

			foreach (var mdRef in refCopy) {
				object item;
				if (mdRef.Key is ITypeDefOrRef)
					item = module.Import((ITypeDefOrRef)mdRef.Key);
				else if (mdRef.Key is MemberRef)
					item = module.Import((MemberRef)mdRef.Key);
				else if (mdRef.Key is MethodDef)
					item = module.Import((MethodDef)mdRef.Key);
				else if (mdRef.Key is MethodSpec)
					item = module.Import((MethodSpec)mdRef.Key);
				else if (mdRef.Key is FieldDef)
					item = module.Import((FieldDef)mdRef.Key);
				else
					item = mdRef.Key;

				VMRT.Descriptor.Data.refMap.Add((IMemberRef)item, mdRef.Value);
			}

			foreach (var sig in VMRT.Descriptor.Data.sigs) {
				var methodSig = sig.Signature;
				var funcSig = sig.FuncSig;

				if (methodSig.HasThis)
					funcSig.Flags |= VMRT.Descriptor.Runtime.RTFlags.INSTANCE;

				var paramTypes = new List<ITypeDefOrRef>();

				if (methodSig.HasThis && !methodSig.ExplicitThis) {
					IType thisType;
					if (sig.DeclaringType.IsValueType)
						thisType = module.Import(new ByRefSig(sig.DeclaringType.ToTypeSig()).ToTypeDefOrRef());
					else
						thisType = module.Import(sig.DeclaringType);
					paramTypes.Add((ITypeDefOrRef)thisType);
				}

				foreach (var param in methodSig.Params) {
					var paramType = (ITypeDefOrRef)module.Import(param.ToTypeDefOrRef());
					paramTypes.Add(paramType);
				}

				funcSig.ParamSigs = paramTypes.ToArray();

				var retType = (ITypeDefOrRef)module.Import(methodSig.RetType.ToTypeDefOrRef());
				funcSig.RetType = retType;
			}
		}

		void MutateMetadata() {
			foreach (var mdRef in VMRT.Descriptor.Data.refMap)
				mdRef.Key.Rid = RTMetadata.GetToken(mdRef.Key).Rid;

			foreach (var sig in VMRT.Descriptor.Data.sigs) {
				var funcSig = sig.FuncSig;

				foreach (var paramType in funcSig.ParamSigs)
					paramType.Rid = RTMetadata.GetToken(paramType).Rid;

				funcSig.RetType.Rid = RTMetadata.GetToken(funcSig.RetType).Rid;
			}
		}
    }
}