using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.VM;
using EXGuard.Core.VMIL;
using EXGuard.Core.Services;

namespace EXGuard.Core.RT.Mutation {
	public class RTConstants {
        private readonly Dictionary<string, byte> Constants = new Dictionary<string, byte>();
        private double EncryptionKey;

        public RTConstants(double encryptionKey)
        {
            EncryptionKey = encryptionKey;
        }

        private void AddField(string fieldName, byte fieldValue)
        {
            Constants[fieldName] = fieldValue;
        }

        public void ReadConstants(VMDescriptor desc)
        {
            for (var i = 0; i < (int)VMRegisters.Max; i++)
            {
                var reg = (VMRegisters)i;
                var regId = desc.Architecture.Registers[reg];
                var regField = reg.ToString();
                AddField(regField, regId);
            }

            for (var i = 0; i < (int)VMFlags.Max; i++)
            {
                var fl = (VMFlags)i;
                var flId = desc.Architecture.Flags[fl];
                var flField = fl.ToString();
                AddField(flField, (byte)(1 << flId));
            }

            for (var i = 0; i < (int)ILOpCode.Max; i++)
            {
                var op = (ILOpCode)i;
                var opId = desc.Architecture.OpCodes[op];
                var opField = op.ToString();
                AddField(opField, opId);
            }

            for (var i = 0; i < (int)VMCalls.Max; i++)
            {
                var vc = (VMCalls)i;
                var vcId = desc.Runtime.VMCall[vc];
                var vcField = vc.ToString();
                AddField(vcField, (byte)vcId);
            }

            AddField(ConstantFields.E_CALL.ToString(), (byte)desc.Runtime.VCallOps.ECALL_CALL);
            AddField(ConstantFields.E_CALLVIRT.ToString(), (byte)desc.Runtime.VCallOps.ECALL_CALLVIRT);
            AddField(ConstantFields.E_NEWOBJ.ToString(), (byte)desc.Runtime.VCallOps.ECALL_NEWOBJ);
            AddField(ConstantFields.E_CALLVIRT_CONSTRAINED.ToString(), (byte)desc.Runtime.VCallOps.ECALL_CALLVIRT_CONSTRAINED);

            //AddField(ConstantFields.INSTANCE.ToString(), desc.Runtime.RTFlags.INSTANCE);

            AddField(ConstantFields.CATCH.ToString(), desc.Runtime.RTFlags.EH_CATCH);
            AddField(ConstantFields.FILTER.ToString(), desc.Runtime.RTFlags.EH_FILTER);
            AddField(ConstantFields.FAULT.ToString(), desc.Runtime.RTFlags.EH_FAULT);
            AddField(ConstantFields.FINALLY.ToString(), desc.Runtime.RTFlags.EH_FINALLY);

            #region Set Constants Data
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #region Constants Data Write "const_stream"
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var const_stream = new MemoryStream();
            
            using (var const_writer = new BinaryWriter(const_stream))
            {
                List<byte> buffer = new List<byte>();
                buffer.AddRange(Constants.Values);

                for (int i = 0; i < buffer.Count; i++)
                    const_writer.Write(((int)buffer[i]).EncryptInt(EncryptionKey));
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            desc.Data.constantsMap = const_stream.ToArray();
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion
        }
    }
}