using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.CodeEncryption
{
    public class MutationHelper : IDisposable
    {
        string m_mtFullName;
        public string MutationType
        {
            get { return m_mtFullName; }
            set { m_mtFullName = value; }
        }
        public MutationHelper() : this(typeof(MutationHelper).FullName) { }
        public MutationHelper(string mtFullName)
        {
            m_mtFullName = mtFullName;
        }
        private static void SetInstrForInjectKey(Instruction instr, Type type, object value)
        {
            instr.OpCode = GetOpCode(type);
            instr.Operand = GetOperand(type, value);
        }
        private static OpCode GetOpCode(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return OpCodes.Ldc_I4;
                case TypeCode.SByte:
                    return OpCodes.Ldc_I4_S;
                case TypeCode.Byte:
                    return OpCodes.Ldc_I4;
                case TypeCode.Int32:
                    return OpCodes.Ldc_I4;
                case TypeCode.UInt32:
                    return OpCodes.Ldc_I4;
                case TypeCode.Int64:
                    return OpCodes.Ldc_I8;
                case TypeCode.UInt64:
                    return OpCodes.Ldc_I8;
                case TypeCode.Single:
                    return OpCodes.Ldc_R4;
                case TypeCode.Double:
                    return OpCodes.Ldc_R8;
                case TypeCode.String:
                    return OpCodes.Ldstr;
                default:
                    throw new SystemException("Unreachable code reached.");
            }
        }
        private static object GetOperand(Type type, object value)
        {
            if (type == typeof(bool))
            {
                return (bool)value ? 1 : 0;
            }
            return value;
        }
        public void InjectKey<T>(MethodDef method, int keyId, T key)
        {
            if (string.IsNullOrWhiteSpace(m_mtFullName))
                throw new ArgumentException();

            var instrs = method.Body.Instructions;
            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode == OpCodes.Call && instrs[i].Operand is IMethod keyMD)
                {
                    if (keyMD.DeclaringType.FullName == m_mtFullName &&
                        keyMD.Name == "Key")
                    {
                        var keyMDId = method.Body.Instructions[i - 1].GetLdcI4Value();
                        if (keyMDId == keyId)
                        {
                            if (typeof(T).IsAssignableFrom(Type.GetType(keyMD.FullName.Split(' ')[0])))
                            {
                                method.Body.Instructions.RemoveAt(i);

                                SetInstrForInjectKey(instrs[i - 1], typeof(T), key);
                            }
                            else
                                throw new ArgumentException("The specified type does not match the type to be injected.");
                        }
                    }
                }
            }
        }
        public void InjectKeys<T>(MethodDef method, int[] keyIds, T[] keys)
        {
            if (string.IsNullOrWhiteSpace(m_mtFullName))
            {
                throw new ArgumentException();
            }
            var instrs = method.Body.Instructions;
            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i].OpCode == OpCodes.Call && instrs[i].Operand is IMethod keyMD)
                {
                    if (keyMD.DeclaringType.FullName == m_mtFullName &&
                        keyMD.Name == "Key")
                    {
                        var keyMDId = method.Body.Instructions[i - 1].GetLdcI4Value();
                        if (keyMDId == 0 || Array.IndexOf(keyIds, keyMDId) != -1)
                        {
                            if (typeof(T).IsAssignableFrom(Type.GetType(keyMD.FullName.Split(' ')[0])))
                            {
                                method.Body.Instructions.RemoveAt(i);
                                SetInstrForInjectKey(instrs[i - 1], typeof(T), keys[keyMDId]);
                            }
                            else
                            {
                                throw new ArgumentException("The specified type does not match the type to be injected.");
                            }
                        }
                    }
                }
            }
        }
        public bool GetInstrLocationIndex(MethodDef method, bool removeCall, out int index)
        {
            if (string.IsNullOrWhiteSpace(m_mtFullName))
                throw new ArgumentException();
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instr = method.Body.Instructions[i];
                if (instr.OpCode == OpCodes.Call)
                {
                    var md = instr.Operand as IMethod;
                    if (md.DeclaringType.FullName == m_mtFullName && md.Name == "LocationIndex")
                    {
                        index = i;
                        if (removeCall)
                            method.Body.Instructions.RemoveAt(i);

                        return true;
                    }
                }
            }
            index = -1;
            return false;
        }
        public void Dispose()
        {
            m_mtFullName = null;
            GC.SuppressFinalize(this);
        }
    }
}
