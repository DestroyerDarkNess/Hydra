using System;
using System.Linq;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.RT;
using EXGuard.Core.Services;

namespace EXGuard.Core.Helpers {
	internal static class MutationHelper
    {
        #region Field2Index (Changed Version)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static Dictionary<string, int> Field2IntIndex = new Dictionary<string, int> {
            { "IntKey0", 0 },
            { "IntKey1", 1 },
            { "IntKey2", 2 },
            { "IntKey3", 3 },
            { "IntKey4", 4 },
            { "IntKey5", 5 },
            { "IntKey6", 6 },
            { "IntKey7", 7 },
            { "IntKey8", 8 },
            { "IntKey9", 9 },
            { "IntKey10", 10 },
            { "IntKey11", 11 },
            { "IntKey12", 12 },
            { "IntKey13", 13 },
            { "IntKey14", 14 },
            { "IntKey15", 15 },
            { "IntKey16", 16 },
            { "IntKey17", 17 },
            { "IntKey18", 18 },
            { "IntKey19", 19 },
            { "IntKey20", 20 }
        };

        internal static Dictionary<string, int> Field2LongIndex = new Dictionary<string, int> {
			{ "LongKey0", 0 },
			{ "LongKey1", 1 },
			{ "LongKey2", 2 },
			{ "LongKey3", 3 },
			{ "LongKey4", 4 },
			{ "LongKey5", 5 },
			{ "LongKey6", 6 },
			{ "LongKey7", 7 },
			{ "LongKey8", 8 },
			{ "LongKey9", 9 },
			{ "LongKey10", 10 },
			{ "LongKey11", 11 },
			{ "LongKey12", 12 },
			{ "LongKey13", 13 },
			{ "LongKey14", 14 },
			{ "LongKey15", 15 },
            { "LongKey16", 16 },
            { "LongKey17", 17 },
            { "LongKey18", 18 },
            { "LongKey19", 19 },
            { "LongKey20", 20 }
        };

        internal static Dictionary<string, int> Field2ULongIndex = new Dictionary<string, int> {
            { "ULongKey0", 0 },
            { "ULongKey1", 1 },
            { "ULongKey2", 2 },
            { "ULongKey3", 3 },
            { "ULongKey4", 4 },
            { "ULongKey5", 5 },
            { "ULongKey6", 6 },
            { "ULongKey7", 7 },
            { "ULongKey8", 8 },
            { "ULongKey9", 9 },
            { "ULongKey10", 10 },
            { "ULongKey11", 11 },
            { "ULongKey12", 12 },
            { "ULongKey13", 13 },
            { "ULongKey14", 14 },
            { "ULongKey15", 15 },
            { "ULongKey16", 16 },
            { "ULongKey17", 17 },
            { "ULongKey18", 18 },
            { "ULongKey19", 19 },
            { "ULongKey20", 20 }
        };

        internal static Dictionary<string, string> Field2LdstrIndex = new Dictionary<string, string> {
            { "LdstrKey0", Convert.ToString(0) },
            { "LdstrKey1", Convert.ToString(1) },
            { "LdstrKey2", Convert.ToString(2) },
            { "LdstrKey3", Convert.ToString(3) },
            { "LdstrKey4", Convert.ToString(4) },
            { "LdstrKey5", Convert.ToString(5) },
            { "LdstrKey6", Convert.ToString(6) },
            { "LdstrKey7", Convert.ToString(7) },
            { "LdstrKey8", Convert.ToString(8) },
            { "LdstrKey9", Convert.ToString(9) },
            { "LdstrKey10", Convert.ToString(10) },
            { "LdstrKey11", Convert.ToString(11) },
            { "LdstrKey12", Convert.ToString(12) },
            { "LdstrKey13", Convert.ToString(13) },
            { "LdstrKey14", Convert.ToString(14) },
            { "LdstrKey15", Convert.ToString(15) },
            { "LdstrKey16", Convert.ToString(16) },
            { "LdstrKey17", Convert.ToString(17) },
            { "LdstrKey18", Convert.ToString(18) },
            { "LdstrKey19", Convert.ToString(19) },
            { "LdstrKey20", Convert.ToString(20) },
        };
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region Original Field2Index (No Changed)
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        internal static readonly Dictionary<string, int> Original_Field2IntIndex = new Dictionary<string, int> {
            { "IntKey0", 0 },
            { "IntKey1", 1 },
            { "IntKey2", 2 },
            { "IntKey3", 3 },
            { "IntKey4", 4 },
            { "IntKey5", 5 },
            { "IntKey6", 6 },
            { "IntKey7", 7 },
            { "IntKey8", 8 },
            { "IntKey9", 9 },
            { "IntKey10", 10 },
            { "IntKey11", 11 },
            { "IntKey12", 12 },
            { "IntKey13", 13 },
            { "IntKey14", 14 },
            { "IntKey15", 15 },
            { "IntKey16", 16 },
            { "IntKey17", 17 },
            { "IntKey18", 18 },
            { "IntKey19", 19 },
            { "IntKey20", 20 }
        };

        internal static readonly Dictionary<string, int> Original_Field2LongIndex = new Dictionary<string, int> {
            { "LongKey0", 0 },
            { "LongKey1", 1 },
            { "LongKey2", 2 },
            { "LongKey3", 3 },
            { "LongKey4", 4 },
            { "LongKey5", 5 },
            { "LongKey6", 6 },
            { "LongKey7", 7 },
            { "LongKey8", 8 },
            { "LongKey9", 9 },
            { "LongKey10", 10 },
            { "LongKey11", 11 },
            { "LongKey12", 12 },
            { "LongKey13", 13 },
            { "LongKey14", 14 },
            { "LongKey15", 15 },
            { "LongKey16", 16 },
            { "LongKey17", 17 },
            { "LongKey18", 18 },
            { "LongKey19", 19 },
            { "LongKey20", 20 }
        };

        internal static Dictionary<string, int> Original_Field2ULongIndex = new Dictionary<string, int> {
            { "ULongKey0", 0 },
            { "ULongKey1", 1 },
            { "ULongKey2", 2 },
            { "ULongKey3", 3 },
            { "ULongKey4", 4 },
            { "ULongKey5", 5 },
            { "ULongKey6", 6 },
            { "ULongKey7", 7 },
            { "ULongKey8", 8 },
            { "ULongKey9", 9 },
            { "ULongKey10", 10 },
            { "ULongKey11", 11 },
            { "ULongKey12", 12 },
            { "ULongKey13", 13 },
            { "ULongKey14", 14 },
            { "ULongKey15", 15 },
            { "ULongKey16", 16 },
            { "ULongKey17", 17 },
            { "ULongKey18", 18 },
            { "ULongKey19", 19 },
            { "ULongKey20", 20 }
        };

        internal static readonly Dictionary<string, string> Original_Field2LdstrIndex = new Dictionary<string, string> {
            { "LdstrKey0", Convert.ToString(0) },
            { "LdstrKey1", Convert.ToString(1) },
            { "LdstrKey2", Convert.ToString(2) },
            { "LdstrKey3", Convert.ToString(3) },
            { "LdstrKey4", Convert.ToString(4) },
            { "LdstrKey5", Convert.ToString(5) },
            { "LdstrKey6", Convert.ToString(6) },
            { "LdstrKey7", Convert.ToString(7) },
            { "LdstrKey8", Convert.ToString(8) },
            { "LdstrKey9", Convert.ToString(9) },
            { "LdstrKey10", Convert.ToString(10) },
            { "LdstrKey11", Convert.ToString(11) },
            { "LdstrKey12", Convert.ToString(12) },
            { "LdstrKey13", Convert.ToString(13) },
            { "LdstrKey14", Convert.ToString(14) },
            { "LdstrKey15", Convert.ToString(15) },
            { "LdstrKey16", Convert.ToString(16) },
            { "LdstrKey17", Convert.ToString(17) },
            { "LdstrKey18", Convert.ToString(18) },
            { "LdstrKey19", Convert.ToString(19) },
            { "LdstrKey20", Convert.ToString(20) },
        };
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        #endregion

        internal static void InjectKey_Int(MethodDef method, int keyId, int key)
        {
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;

                    int _keyId;
                    if (field.DeclaringType.FullName == RTMap.Mutation && Field2IntIndex.TryGetValue(field.Name, out _keyId) && _keyId == keyId)
                    {
                        instr.OpCode = OpCodes.Ldc_I4;
                        instr.Operand = key;
                    }
                }
            }
        }

        internal static void InjectKey_Long(MethodDef method, int keyId, long key) {
			foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;

                    int _keyId;
                    if (field.DeclaringType.FullName == RTMap.Mutation && Field2LongIndex.TryGetValue(field.Name, out _keyId) && _keyId == keyId)
                    {
                        instr.OpCode = OpCodes.Ldc_I8;
                        instr.Operand = key;
                    }
                }
            }
		}

        internal static void InjectKey_ULong(MethodDef method, int keyId, ulong key)
        {
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;

                    int _keyId;
                    if (field.DeclaringType.FullName == RTMap.Mutation && Field2ULongIndex.TryGetValue(field.Name, out _keyId) && _keyId == keyId)
                    {
                        instr.OpCode = OpCodes.Ldc_I8;
                        instr.Operand = (long)key;
                    }
                }
            }
        }

        internal static void InjectKey_String(MethodDef method, int keyId, string key)
        {
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;

                    string _keyId;
                    if (field.DeclaringType.FullName == RTMap.Mutation && Field2LdstrIndex.TryGetValue(field.Name, out _keyId) && _keyId == keyId.ToString())
                    {
                        instr.OpCode = OpCodes.Ldstr;
                        instr.Operand = key;
                    }
                }
            }
        }

        internal static void InjectKeys_Int(MethodDef method, int[] keyIds, int[] keys)
        {
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;
                    int _keyIndex;
                    if (field.DeclaringType.FullName == RTMap.Mutation &&
                        Field2IntIndex.TryGetValue(field.Name, out _keyIndex) && (_keyIndex = Array.IndexOf(keyIds, _keyIndex)) != -1)
                    {
                        instr.OpCode = OpCodes.Ldc_I4;
                        instr.Operand = keys[_keyIndex];
                    }
                }
            }
        }

        internal static void InjectKeys_Long(MethodDef method, int[] keyIds, long[] keys) {
			foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;
                    int _keyIndex;
                    if (field.DeclaringType.FullName == RTMap.Mutation &&
                        Field2LongIndex.TryGetValue(field.Name, out _keyIndex) && (_keyIndex = Array.IndexOf(keyIds, _keyIndex)) != -1)
                    {
                        instr.OpCode = OpCodes.Ldc_I8;
                        instr.Operand = keys[_keyIndex];
                    }
                }
            }
		}

        internal static void InjectKeys_ULong(MethodDef method, int[] keyIds, ulong[] keys)
        {
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;
                    int _keyIndex;
                    if (field.DeclaringType.FullName == RTMap.Mutation &&
                        Field2ULongIndex.TryGetValue(field.Name, out _keyIndex) && (_keyIndex = Array.IndexOf(keyIds, _keyIndex)) != -1)
                    {
                        instr.OpCode = OpCodes.Ldc_I8;
                        instr.Operand = (long)keys[_keyIndex];
                    }
                }
            }
        }

        internal static void InjectKeys_String(MethodDef method, int[] keyIds, string[] keys)
        {
            foreach (Instruction instr in method.Body.Instructions)
            {
                if (instr.OpCode == OpCodes.Ldsfld)
                {
                    var field = instr.Operand as IField;
                    string _keyIndex;
                    if (field.DeclaringType.FullName == RTMap.Mutation &&
                        Field2LdstrIndex.TryGetValue(field.Name, out _keyIndex) && Convert.ToInt32(_keyIndex = Array.IndexOf(keyIds, int.Parse(_keyIndex)).ToString()) != -1)
                    {
                        instr.OpCode = OpCodes.Ldstr;
                        instr.Operand = keys[int.Parse(_keyIndex)];
                    }
                }
            }
        }

        internal static void ReplaceValue_T(MethodDef method, Instruction ret_inst)
        {
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instr = method.Body.Instructions[i];
                var md = instr.Operand as IMethod;
                if (instr.OpCode == OpCodes.Call &&
                    md.DeclaringType.Name == RTMap.Mutation &&
                    md.Name == RTMap.Mutation_Value_T)
                {
                    method.Body.Instructions[i] = ret_inst;
                }
            }
        }

        internal static void GetInstructionsLocationIndex(MethodDef method, bool removeCall, out int index)
        {
            int idex = -1;

            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instr = method.Body.Instructions[i];
                var md = instr.Operand as IMethod;
                if (instr.OpCode == OpCodes.Call &&
                    md.DeclaringType.Name == RTMap.Mutation &&
                    md.Name == RTMap.Mutation_LocationIndex)
                {
                    idex = i;

                    if (removeCall)
                        method.Body.Instructions.RemoveAt(i);
                }
            }

            index = idex;
        }

        internal static void ReplacePlaceholder_Inject_ByteArray(MethodDef method, byte[] data)
        {
            ReplacePlaceholder(method, arg => 
            {
                var repl = new List<Instruction>();
                repl.AddRange(arg);

                for (var j = 0; j < data.Length; j++)
                {
                    repl.Add(Instruction.Create(OpCodes.Dup));
                    repl.Add(Instruction.Create(OpCodes.Ldc_I4, j));
                    repl.Add(Instruction.Create(OpCodes.Ldc_I4, (int)data[j]));
                    repl.Add(Instruction.Create(OpCodes.Stelem_Ref));
                }

                return repl.ToArray();
            });
        }

        internal static void ReplacePlaceholder(MethodDef method, Func<Instruction[], Instruction[]> repl) {
			MethodTrace trace = new MethodTrace(method).Trace();
			for (int i = 0; i < method.Body.Instructions.Count; i++) {
				Instruction instr = method.Body.Instructions[i];
				if (instr.OpCode == OpCodes.Call) {
					var operand = (IMethod)instr.Operand;
					if (operand.DeclaringType.FullName == RTMap.Mutation &&
					    operand.Name == RTMap.Mutation_Placeholder) {
						int[] argIndexes = trace.TraceArguments(instr);
						if (argIndexes == null)
							throw new ArgumentException("Failed to trace placeholder argument.");

						int argIndex = argIndexes[0];
						Instruction[] arg = method.Body.Instructions.Skip(argIndex).Take(i - argIndex).ToArray();
						for (int j = 0; j < arg.Length; j++)
							method.Body.Instructions.RemoveAt(argIndex);
						method.Body.Instructions.RemoveAt(argIndex);
						arg = repl(arg);
						for (int j = arg.Length - 1; j >= 0; j--)
							method.Body.Instructions.Insert(argIndex, arg[j]);
						return;
					}
				}
			}
		}
	}
}