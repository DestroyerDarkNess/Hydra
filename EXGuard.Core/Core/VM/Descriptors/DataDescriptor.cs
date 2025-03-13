using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using dnlib.DotNet;

using EXGuard.Core.Services;

namespace EXGuard.Core.VM
{
    public class DataDescriptor
    {
        private readonly Dictionary<MethodDef, int> exportMap = new Dictionary<MethodDef, int>();
        private readonly Dictionary<MethodDef, VMMethodInfo> methodInfos = new Dictionary<MethodDef, VMMethodInfo>();

        private uint nextRefId;
        private uint nextStrId;

        private readonly Virtualizer vr;
        private readonly RandomGenerator randomgen;

        internal byte[] constantsMap = new byte[0];
        internal Dictionary<IMemberRef, uint> refMap = new Dictionary<IMemberRef, uint>();
        internal List<FuncSigDesc> sigs = new List<FuncSigDesc>();
        internal Dictionary<byte[], uint> strMap = new Dictionary<byte[], uint>();

        internal DataDescriptor(Virtualizer vr, RandomGenerator randomgen)
        {
            // 0 = null, 1 = ""
            strMap[new byte[0]] = 1;

            nextStrId = 2;
            nextRefId = 1;

            this.vr = vr;
            this.randomgen = randomgen;
        }

        public uint GetId(IMemberRef memberRef)
        {
            uint ret;
            if(!refMap.TryGetValue(memberRef, out ret))
                refMap[memberRef] = ret = nextRefId++;
            return ret;
        }

        public void ReplaceReference(IMemberRef old, IMemberRef @new)
        {
            uint id;
            if (!refMap.TryGetValue(old, out id))
                return;

            refMap.Remove(old);
            refMap[@new] = id;
        }

        public uint GetId(byte[] str)
        {
            uint ret;
            if(!strMap.TryGetValue(str, out ret))
                strMap[str] = ret = nextStrId++;

            return ret;
        }

        public void ReadExportMDToken(MethodDef method, MDToken mdToken)
        {
            if (!exportMap.TryGetValue(method, out _))
            {
                exportMap[method] = mdToken.ToInt32();
                sigs.Add(new FuncSigDesc(method, mdToken));
            }
        }

        public VMMethodInfo LookupInfo(MethodDef method)
        {
            VMMethodInfo ret;

            if(!methodInfos.TryGetValue(method, out ret))
            {
                var k = randomgen.NextByte();
                ret = new VMMethodInfo
                {
                    EntryKey = k,
                    ExitKey = (byte) (k >> 8)
                };
                methodInfos[method] = ret;
            }

            return ret;
        }

        public void SetInfo(MethodDef method, VMMethodInfo info)
        {
            methodInfos[method] = info;
        }
    }
}