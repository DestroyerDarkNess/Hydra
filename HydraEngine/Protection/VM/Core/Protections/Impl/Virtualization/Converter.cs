using dnlib.DotNet;
using dnlib.DotNet.Emit;
using VM.Runtime.Handler;
using VM.Runtime.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HydraEngine.Protection.VM;

// github.com/hexck
namespace VM.Core.Protections.Impl.Virtualization
{
    public class Converter
    {
        public MethodDef Method { get; }
        
        public bool Compatible { get; set; }
        
        public string Name { get; }

        private BinaryWriter _binaryWriter;

        private MemoryStream _memoryStream;

        public Converter(MethodDef methodDef, string name)
        {
            Method = methodDef;
            Name = methodDef.Name;
            Compatible = true;
            _memoryStream = new MemoryStream();
            _binaryWriter = new BinaryWriter(_memoryStream);
        }

        public byte[] ConvertMethod()
        {
            var instrs = AsHxInstructions();

            _binaryWriter.Write(instrs.Count);
            foreach (var i in instrs)
            {
                _binaryWriter.Write((int)i.OpCode);
                _binaryWriter.Write(i.Operand != null);

                if (i.Operand == null) continue;

                var value = i.Operand;
                if (value.IsString())
                {
                    _binaryWriter.Write(0);
                    _binaryWriter.Write((string)value.GetObject());
                }
                else if (value.IsInt16())
                {
                    _binaryWriter.Write(1);
                    _binaryWriter.Write((short)value.GetObject());
                }
                else if (value.IsInt32())
                {
                    _binaryWriter.Write(2);
                    _binaryWriter.Write((int)value.GetObject());
                }
                else if (value.IsInt64())
                {
                    _binaryWriter.Write(3);
                    _binaryWriter.Write((long)value.GetObject());
                }
                else if (value.IsUInt16())
                {
                    _binaryWriter.Write(4);
                    _binaryWriter.Write((ushort)value.GetObject());
                }
                else if (value.IsUInt32())
                {
                    _binaryWriter.Write(5);
                    _binaryWriter.Write((uint)value.GetObject());
                }
                else if (value.IsUInt64())
                {
                    _binaryWriter.Write(6);
                    _binaryWriter.Write((ulong)value.GetObject());
                }
                else if (value.IsDouble())
                {
                    _binaryWriter.Write(7);
                    _binaryWriter.Write((double)value.GetObject());
                }
                else if (value.IsDecimal())
                {
                    _binaryWriter.Write(8);
                    _binaryWriter.Write((decimal)value.GetObject());
                }
                else if (value.IsByte())
                {
                    _binaryWriter.Write(9);
                    _binaryWriter.Write((byte)value.GetObject());
                }
                else if (value.IsSByte())
                {
                    _binaryWriter.Write(10);
                    _binaryWriter.Write((sbyte)value.GetObject());
                }
                else if (value.IsFloat())
                {
                    _binaryWriter.Write(11);
                    _binaryWriter.Write((float)value.GetObject());
                }
                else if (value.IsNull())
                {
                    _binaryWriter.Write(12);
                }

            }

            return _memoryStream.ToArray();
        }

        //The second Dictionary was not originally here I was getting the number value by the key position. For some reasons the output is not exactly the same.
        /*public byte[] ConvertMethod()
        {
            Dictionary<TypeCode, Action<Value>> writeHandlers = new Dictionary<TypeCode, Action<Value>>
            {
                { TypeCode.String, value => _binaryWriter.Write((string)value.GetObject()) },
                { TypeCode.Int16, value => _binaryWriter.Write((short)value.GetObject())  },
                { TypeCode.Int32, value => _binaryWriter.Write((int)value.GetObject())  },
                { TypeCode.Int64, value => _binaryWriter.Write((long)value.GetObject())  },
                { TypeCode.UInt16, value => _binaryWriter.Write((ushort)value.GetObject())  },
                { TypeCode.UInt32, value => _binaryWriter.Write((uint)value.GetObject())  },
                { TypeCode.UInt64, value => _binaryWriter.Write((ulong)value.GetObject())  },
                { TypeCode.Double, value => _binaryWriter.Write((double)value.GetObject())  },
                { TypeCode.Decimal, value => _binaryWriter.Write((decimal)value.GetObject())  },
                { TypeCode.Byte, value => _binaryWriter.Write((byte)value.GetObject())  },
                { TypeCode.SByte, value => _binaryWriter.Write((sbyte)value.GetObject())  },
                { TypeCode.Single, value => _binaryWriter.Write((float)value.GetObject())  },
            };

            Dictionary<TypeCode, int> Numbers = new Dictionary<TypeCode, int>
            {
                { TypeCode.String, 0 },
                { TypeCode.Int16, 1 },
                { TypeCode.Int32, 2 },
                { TypeCode.Int64, 3 },
                { TypeCode.UInt16, 4 },
                { TypeCode.UInt32, 5 },
                { TypeCode.UInt64, 6 },
                { TypeCode.Double, 7 },
                { TypeCode.Decimal, 8 },
                { TypeCode.Byte, 9 },
                { TypeCode.SByte, 10 },
                { TypeCode.Single, 11 },
            };

            var instrs = AsHxInstructions();

            _binaryWriter.Write(instrs.Count);

            foreach (var i in instrs)
            {
                try
                {
                    //Console.WriteLine($"{i.ToString()} | {(int)i.OpCode} | {i.Operand}");
                    _binaryWriter.Write((int)i.OpCode);
                    _binaryWriter.Write(i.Operand != null);

                    var value = i.Operand;

                    if (value.GetObject() != null || value != null)
                    {
                        //Console.WriteLine($"OP: {Type.GetTypeCode(value.GetType())} | OP_GetObject(): {Type.GetTypeCode(value.GetObject().GetType())}");
                        //Type.GetTypeCode(_obj.GetType()) == TypeCode.String
                        if (writeHandlers.TryGetValue(Type.GetTypeCode(value.GetObject().GetType()), out var writeHandler))
                        {
                            try
                            {
                                *//*var indexOfWriteHandler = writeHandlers.Keys.ToList().IndexOf(Type.GetTypeCode(value.GetType()));
                                _binaryWriter.Write(indexOfWriteHandler);*//*
                                if (Numbers.TryGetValue(Type.GetTypeCode(value.GetObject().GetType()), out var number))
                                {
                                    _binaryWriter.Write(number);
                                    writeHandler(value);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }
                    else
                    {
                        _binaryWriter.Write(12);
                    }
                } catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                }
            }

            return _memoryStream.ToArray();
        }*/

        static OpCode OP = null;
        static bool IsOP(OpCode OPToMatch) => OP == OPToMatch;
        public List<HxInstruction> AsHxInstructions()
        {
            Method.Body.OptimizeBranches();
            Method.Body.OptimizeMacros();
            Method.Body.SimplifyBranches();
            Method.Body.SimplifyMacros(Method.Parameters);
            
            var instrs = Method.Body.Instructions.ToList();
            
            var conv = new List<HxInstruction>();
            foreach (var i in instrs)
            {
                var x = Enum.GetNames(typeof(HxOpCodes)).ToList().FirstOrDefault(
                    //name => string.Equals(name.Substring(1), i.OpCode.Name, StringComparison.CurrentCultureIgnoreCase));
                    name => string.Equals(name.Substring(1).Replace('.', '_'), i.OpCode.Name.Replace('.', '_'), StringComparison.CurrentCultureIgnoreCase));

                // simple stuff, no operand
                if (x != null)
                {
                    var opc = (HxOpCodes) Enum.Parse(typeof(HxOpCodes), x, true);
                    conv.Add(new HxInstruction(opc));
                    continue;
                }

                OP = i.OpCode;

                if (i.IsLdcI4() || i.OpCode == OpCodes.Ldc_R4 || i.OpCode == OpCodes.Ldc_R8 ||
                    i.OpCode == OpCodes.Ldc_I8 || i.OpCode == OpCodes.Ldc_I4_M1 || i.OpCode == OpCodes.Ldnull ||
                    i.OpCode == OpCodes.Ldstr)
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxLdc,
                        new Value(i.OpCode == OpCodes.Ldnull ? null : i.Operand)));
                }
                else if (i.OpCode == OpCodes.Ldtoken)
                {
                    // eh string is fine here, we can just do substring later
                    var value = new Value("");
                    if (i.Operand is MethodDef)
                        value = new Value("0" + ((MethodDef)i.Operand).MDToken.ToInt32());
                    else if (i.Operand is MemberRef)
                        value = new Value("1" + ((MemberRef)i.Operand).MDToken.ToInt32());
                    else if (i.Operand is IField)
                        value = new Value("2" + ((IField)i.Operand).MDToken.ToInt32());
                    else if (i.Operand is ITypeDefOrRef)
                        value = new Value("3" + ((ITypeDefOrRef)i.Operand).MDToken.ToInt32());
                    else
                        throw new NotSupportedException();

                    conv.Add(new HxInstruction(HxOpCodes.Ldtoken, value));
                }
                else if (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
                {
                    var op = (IMethod)i.Operand;

                    /*ByRef*/
                    if (op.GetParams().Where(P => P.IsByRef).Count() > 0)
                    {
                        Compatible = false;
                        conv.Clear();
                        //Context.Instance.Log.Warning($"`ref/out` keywords are not supported: {Method.FullName}");
                    }
                    /*ByRef*/

                    var CallVirt = (i.OpCode == OpCodes.Callvirt ? "0" : "1");

                    if (op.Name == ".ctor" || op.Name == ".cctor") 
                        conv.Add(new HxInstruction(HxOpCodes.HxCall, new Value(CallVirt + "0" + op.MDToken.ToInt32())));
                    else if (op.IsMethodDef)
                        conv.Add(new HxInstruction(HxOpCodes.HxCall, new Value(CallVirt + "1" + op.MDToken.ToInt32())));
                    else
                        conv.Add(new HxInstruction(HxOpCodes.HxCall, new Value(CallVirt + "2" + op.MDToken.ToInt32())));
                }
                else if (i.IsBr() || i.OpCode == OpCodes.Br_S|| i.IsBrtrue() || i.IsBrfalse() || i.OpCode == OpCodes.Leave || i.OpCode == OpCodes.Leave_S)
                {
                    var val = new Value(Method.Body.Instructions.IndexOf((Instruction) i.Operand));
                    if (i.IsBrtrue())
                    {
                        conv.Add(new HxInstruction(HxOpCodes.Brtrue, val));
                    }
                    else if (i.IsBrfalse())
                    {
                        conv.Add(new HxInstruction(HxOpCodes.Brfalse, val));
                    }
                    else
                    {
                        conv.Add(new HxInstruction(HxOpCodes.HxBr, val));
                    }
                }
                else if (i.OpCode == OpCodes.Box)
                {
                    conv.Add(new HxInstruction(HxOpCodes.Box, new Value(((ITypeDefOrRef)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode.Name.StartsWith("ldelem") || i.OpCode.Name.StartsWith("stelem"))
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxArray, new Value(i.OpCode.Name.StartsWith("lde") ? 0 : 1)));
                }
                else if (i.IsLdloc() || i.IsStloc() || i.OpCode == OpCodes.Ldloca || i.OpCode == OpCodes.Ldloca_S)
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxLoc,
                        new Value((i.IsLdloc() || i.OpCode.Name.StartsWith("ldloc") ? "0" : "1") + Method.Body.Variables.IndexOf((Local) i.Operand))));
                }
                else if (i.IsLdarg() || i.IsStarg())
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxArg,
                        new Value((i.IsLdarg() ? "0" : "1") + Method.Parameters.IndexOf((Parameter) i.Operand))));
                }
                /////////////////////
                /*else if (i.OpCode == OpCodes.Ldfld || i.OpCode == OpCodes.Ldflda || i.OpCode == OpCodes.Ldsfld || i.OpCode == OpCodes.Ldsflda || i.OpCode == OpCodes.Stfld || i.OpCode == OpCodes.Stsfld)
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxFld,
                        new Value((i.OpCode.Name.StartsWith("ldf") ? "0" : i.OpCode.Name.StartsWith("ldsf") ?  "1" : "2") + ((IField) i.Operand).MDToken.ToInt32())));
                }*/
                else if (i.OpCode == OpCodes.Ldfld || i.OpCode == OpCodes.Ldsfld || i.OpCode == OpCodes.Ldflda ||
                        i.OpCode == OpCodes.Ldsflda)
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxFld, new Value("0" + ((IField)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode == OpCodes.Stfld || i.OpCode == OpCodes.Stsfld)
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxFld, new Value("1" + ((IField)i.Operand).MDToken.ToInt32())));
                }
                /////////////////////

                else if(i.OpCode == OpCodes.Conv_R4 || i.OpCode == OpCodes.Conv_R8 || i.OpCode == OpCodes.Conv_I4 || i.OpCode == OpCodes.Conv_I8 || i.OpCode == OpCodes.Conv_U1 || i.OpCode == OpCodes.Conv_U8 || i.OpCode == OpCodes.Conv_U2 || i.OpCode == OpCodes.Conv_U4)
                {
                    var val = IsOP(OpCodes.Conv_R4) ? new Value(0) : IsOP(OpCodes.Conv_R8) ? new Value(1) : IsOP(OpCodes.Conv_I4) ? new Value(2) : IsOP(OpCodes.Conv_I8) ? new Value(3) : IsOP(OpCodes.Conv_U1) ? new Value(4) : IsOP(OpCodes.Conv_U8) ? new Value(5) : IsOP(OpCodes.Conv_U2) ? new Value(6) : IsOP(OpCodes.Conv_U4) ? new Value(7) : new Value(8);
                    conv.Add(new HxInstruction(HxOpCodes.HxConv,  val));
                }
                else if (i.OpCode == OpCodes.Newobj)
                {
                    conv.Add(new HxInstruction(HxOpCodes.Newobj, new Value(((IMethod)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode == OpCodes.Ldftn)
                {
                    conv.Add(new HxInstruction(HxOpCodes.Ldftn, new Value(((IMethod)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode == OpCodes.Newarr)
                {
                    conv.Add(new HxInstruction(HxOpCodes.Newarr, new Value(((IType)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode == OpCodes.Castclass) 
                {
                    conv.Add(new HxInstruction(HxOpCodes.Castclass, new Value(((IType)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode == OpCodes.Initobj)
                {
                    conv.Add(new HxInstruction(HxOpCodes.Initobj, new Value(((ITypeDefOrRef)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode == OpCodes.Constrained)
                {
                    conv.Add(new HxInstruction(HxOpCodes.Constrained, new Value(((ITypeDefOrRef)i.Operand).MDToken.ToInt32())));
                }
                else if (i.OpCode == OpCodes.Cgt || i.OpCode == OpCodes.Cgt_Un)
                {
                    var val = new Value((i.OpCode.Name.Contains("un") ? "1" : "0") + Method.Body.Instructions.IndexOf((Instruction)i.Operand));
                    conv.Add(new HxInstruction(HxOpCodes.HxCgt, val));
                }
                else if (i.OpCode == OpCodes.Clt || i.OpCode == OpCodes.Clt_Un)
                {
                    conv.Add(new HxInstruction(HxOpCodes.HxClt));
                }
                else if (i.OpCode == OpCodes.Endfinally)
                {
                    conv.Add(new HxInstruction(HxOpCodes.Endfinally));
                }
                else if (i.OpCode == OpCodes.Beq || i.OpCode == OpCodes.Beq_S)
                {
                    var val = new Value(Method.Body.Instructions.IndexOf((Instruction)i.Operand));
                    conv.Add(new HxInstruction(HxOpCodes.HxBeq, val));
                }
                else if (i.OpCode == OpCodes.Bge || i.OpCode == OpCodes.Bge_S || i.OpCode == OpCodes.Bge_Un || i.OpCode == OpCodes.Bge_Un_S)
                {
                    var val = new Value((i.OpCode.Name.StartsWith("bge.un") ? "1" : "0") + Method.Body.Instructions.IndexOf((Instruction)i.Operand));
                    conv.Add(new HxInstruction(HxOpCodes.HxBge, val));
                }
                else
                {
                    conv.Clear();
                    Compatible = false;
                    //Context.Instance.Log.Warning($"Unsupported opcode: {i.OpCode}");
                }
            }

            return conv;
        }

        public void Save()
        {
            string resourceName = XXHash.CalculateXXHash32(Method.MDToken.ToInt32().ToString()).ToString();
            byte[] methodBytes = ConvertMethod();
            int[] key = resourceName.Select(c => int.Parse(c.ToString())).ToArray();

            // XOR Cipher encryption with array reversal and key rotation
            int n = methodBytes.Length - 1;

            for (int i = 0; i < n; i++, n--)
            {
                methodBytes[i] ^= methodBytes[n];
                methodBytes[n] ^= (byte)(methodBytes[i] ^ key[i % key.Length]);
                methodBytes[i] ^= methodBytes[n];

                Array.Reverse(methodBytes);

                // Rotate the key
                RotateKey(ref key);
            }

            if (methodBytes.Length % 2 != 0)
                methodBytes[methodBytes.Length >> 1] ^= (byte)key[key.Length - 1];

            Virtualizer.Instance.Module.Resources.Add(new EmbeddedResource(
                resourceName,
                methodBytes
            ));
        }

        private static void RotateKey(ref int[] key)
        {
            int rotationFactor = key.Length % 5 + 1;
            // Automatically decide the multiplier based on key characteristics
            int multiplier = (key.Sum() + key.Aggregate(1, (current, val) => current * val)) % (int.MaxValue / key.Length);
            int preDivide = multiplier != 0 ? int.MaxValue / multiplier : int.MaxValue / 1337;

            for (int rotationCount = 0; rotationCount < rotationFactor; rotationCount++)
            {
                int lastElement = key[key.Length - 1];

                for (int i = key.Length - 1; i > 0; i--)
                {
                    key[i] = (key[i - 1] * multiplier + key.Length) % preDivide;
                }

                key[0] = (lastElement * multiplier + key.Length) % preDivide;
            }
        }

        internal static class XXHash
        {
            internal static uint CalculateXXHash32(string input)
            {
                const uint prime1 = 2654435761U;
                const uint prime2 = 2246822519U;
                const uint prime3 = 3266489917U;
                const uint prime4 = 668265263U;
                const uint prime5 = 374761393U;

                int len = input.Length;
                int index = 0;
                uint seed = 0; // You can change the seed value if needed

                uint h32;

                if (len >= 16)
                {
                    int limit = len - 16;
                    uint v1 = seed + prime1;
                    uint v2 = seed + prime2;
                    uint v3 = seed + prime3;
                    uint v4 = seed + prime4;

                    do
                    {
                        uint block1 = GetUInt32(input, index) * prime2;
                        index += 4;
                        uint block2 = GetUInt32(input, index) * prime2;
                        index += 4;

                        v1 = Rol(v1 + block1, 13) * prime1;
                        v2 = Rol(v2 + block2, 13) * prime1;
                        v1 = Rol(v1, 13);
                        v2 = Rol(v2, 13);

                        uint block3 = GetUInt32(input, index) * prime2;
                        index += 4;
                        uint block4 = GetUInt32(input, index) * prime2;
                        index += 4;

                        v3 = Rol(v3 + block3, 13) * prime1;
                        v4 = Rol(v4 + block4, 13) * prime1;
                        v3 = Rol(v3, 13);
                        v4 = Rol(v4, 13);
                    } while (index <= limit);

                    h32 = Rol(v1, 1) + Rol(v2, 7) + Rol(v3, 12) + Rol(v4, 18);
                }
                else
                {
                    h32 = seed + prime5;
                }

                h32 += (uint)len;

                while (index <= len - 4)
                {
                    h32 += GetUInt32(input, index) * prime3;
                    h32 = Rol(h32, 17) * prime4;
                    index += 4;
                }

                while (index < len)
                {
                    h32 += input[index] * prime5;
                    h32 = Rol(h32, 11) * prime1;
                    index++;
                }

                h32 ^= h32 >> 15;
                h32 *= prime2;
                h32 ^= h32 >> 13;
                h32 *= prime3;
                h32 ^= h32 >> 16;

                return h32;
            }

            private static uint GetUInt32(string input, int index) => (uint)input[index] | ((uint)input[index + 1] << 8) | ((uint)input[index + 2] << 16) | ((uint)input[index + 3] << 24);

            private static uint Rol(uint value, int count) => (value << count) | (value >> (32 - count));
        }
    }
}