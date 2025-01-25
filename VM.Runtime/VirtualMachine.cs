using VM.Runtime.Handler;
using VM.Runtime.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// github.com/hexck
namespace VM.Runtime
{
    public class VirtualMachine
    {
        public static object RunVM(object[] pm, string OriginalMethod)
        {
            if (Assembly.GetCallingAssembly().ToString() == "{{TExecutingHAssemblyT}}")
            {
                var code = Helper.Extract(XXHash.CalculateXXHash32(OriginalMethod).ToString());

                var ms = new MemoryStream(code);
                var br = new BinaryReader(ms);
                var count = br.ReadInt32();
                var instrs = new List<HxInstruction>();

                for (var i = 0; i < count; i++)
                {
                    var opcode = (HxOpCodes)br.ReadInt32();
                    Value operand = null;
                    if (br.ReadBoolean()) // has operand
                    {
                        var type = br.ReadInt32();
                        operand = ReadValue(br, type);
                    }
                    //maybe is better to do this in real time as it may be a bad idea to store all of the instructions in an list like this because it would be easier to devirtualize
                    instrs.Add(new HxInstruction(opcode, operand));
                }

                var ctx = new Context(instrs, pm ?? new object[0]);
                return ctx.Run().GetObject();
            }
            return new object[] { "https://github.com/DestroyerDarkNess" };
        }

        public static Value ReadValue(BinaryReader br, int type)
        {
            Dictionary<int, Func<Value>> valueReaders = new Dictionary<int, Func<Value>>
            {
                { 0, () => new Value(br.ReadString()) },
                { 1, () => new Value(br.ReadInt16()) },
                { 2, () => new Value(br.ReadInt32()) },
                { 3, () => new Value(br.ReadInt64()) },
                { 4, () => new Value(br.ReadUInt16()) },
                { 5, () => new Value(br.ReadUInt32()) },
                { 6, () => new Value(br.ReadUInt64()) },
                { 7, () => new Value(br.ReadDouble()) },
                { 8, () => new Value(br.ReadDecimal()) },
                { 9, () => new Value(br.ReadByte()) },
                { 10, () => new Value(br.ReadSByte()) },
                { 11, () => new Value(br.ReadSingle()) },
                { 12, () => new Value(null) }
            };

            return valueReaders.TryGetValue(type, out var valueReader) ? valueReader() : valueReaders[12](); // default case is null
        }
    }
}