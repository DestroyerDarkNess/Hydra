using System;
using System.IO;
using System.Diagnostics;

using dnlib.DotNet;
using dnlib.DotNet.MD;

using EXGuard.Core;
using EXGuard.Core.Services;

namespace EXGuard.Core.RT
{
    internal class HeaderChunk : IChunk
    {
        VMRuntime rt;
        byte[] data;

        public HeaderChunk(VMRuntime rt)
        {
            this.rt = rt;
            Length = ComputeLength(rt);
        }

        public uint Length { get; set; }

        public void OnOffsetComputed(uint offset) { }

        public byte[] GetData()
        {
            return data;
        }

        int GetCodedToken(MDToken token)
        {
            switch (token.Table)
            {
                case Table.TypeDef:
                    return (int)(token.Rid | 0x02000000);
                case Table.TypeRef:
                    return (int)(token.Rid | 0x01000000);
                case Table.TypeSpec:
                    return (int)(token.Rid | 0x1b000000);
                case Table.MemberRef:
                    return (int)(token.Rid | 0x0a000000);
                case Table.Method:
                    return (int)(token.Rid | 0x06000000);
                case Table.Field:
                    return (int)(token.Rid | 0x04000000);
                case Table.MethodSpec:
                    return (int)(token.Rid | 0x2b000000);
                default:
                    throw new NotSupportedException();
            }
        }

        uint ComputeLength(VMRuntime rt)
        {
            uint len = (uint)rt.Descriptor.Data.constantsMap.Length;

            foreach (var str in rt.Descriptor.Data.strMap)
            {
                len += ((uint)str.Key.Length) + 8;
            }

            foreach (var str in rt.Descriptor.Data.refMap)
                len += 16;

            foreach (var sig in rt.Descriptor.Data.sigs)
            {
                foreach (var paramType in sig.FuncSig.ParamSigs)
                    len += 12;

                if (sig.Method != null)
                    len += 28;
                else
                    len += 24;
            }

            return len;
        }

        internal void WriteData(VMRuntime rt)
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            foreach (var opcode in rt.Descriptor.Data.constantsMap)
                writer.Write(opcode);

            foreach (var str in rt.Descriptor.Data.strMap)
            {
                writer.Write(str.Value);
                writer.Write(str.Key.Length);

                writer.Write(str.Key);
            }

            foreach (var refer in rt.Descriptor.Data.refMap)
            {
                var refMember_Key = (double)(new RandomGenerator().NextDouble() / new RandomGenerator().NextInt32());

                writer.Write(refer.Value);

                writer.Write(GetCodedToken(refer.Key.MDToken).EncryptInt(refMember_Key));
                writer.Write(refMember_Key);
            }

            foreach (var sig in rt.Descriptor.Data.sigs)
            {
                var retTok_Key = (double)(new RandomGenerator().NextDouble() / new RandomGenerator().NextInt32());

                writer.Write(sig.MDToken);

                if (sig.Method != null)
                {
                    var entry = rt.MethodMap[sig.Method].Item2;
                    var entryOffset = entry.Content[0].Offset;

                    Debug.Assert(entryOffset != 0);

                    writer.Write(entryOffset);

                    var key = rt.Descriptor.RandomGenerator.NextUInt32();
                    key = (key << 8) | rt.Descriptor.Data.LookupInfo(sig.Method).EntryKey;
                    writer.Write(key);
                }
                else
                    writer.Write(0u);

                writer.Write(sig.FuncSig.ParamSigs.Length);

                foreach (var paramType in sig.FuncSig.ParamSigs)
                {
                    var paramTok_Key = (double)(new RandomGenerator().NextDouble() / new RandomGenerator().NextInt32());

                    writer.Write(GetCodedToken(paramType.MDToken).EncryptInt(paramTok_Key));
                    writer.Write(paramTok_Key);
                }

                writer.Write(GetCodedToken(sig.FuncSig.RetType.MDToken).EncryptInt(retTok_Key));
                writer.Write(retTok_Key);
            }

            data = stream.ToArray();

            Debug.Assert(data.Length == Length);
        }
    }
}