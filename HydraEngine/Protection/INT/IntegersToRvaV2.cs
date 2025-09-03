using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;

using AsmResolver;
using AsmResolver.IO;

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.DotNet.Code.Cil;          // CilMethodBody, CilLocalVariable

using AsmResolver.PE;
using AsmResolver.PE.DotNet;
using AsmResolver.PE.File.Headers;
using AsmResolver.PE.DotNet.Cil;            // CilOpCodes, CilInstructionLabel
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;
using CilInstr = AsmResolver.PE.DotNet.Cil.CilInstruction;

namespace HydraEngine.Protection.Integer
{
    public class StrIntToRvaV2 : Models.Protection
    {
        public bool ObfuscateStrings { get; set; } = true;

        public bool Encrypt { get; set; } = true;

        public StrIntToRvaV2()
            : base("Protection.Renamer.StrIntToRvaV2", "Renamer Phase",
                   "RVA Pool")
        {
            ManualReload = true;
        }

        private const int ENTRY_SIZE = 44;
        private const byte KIND_I4 = 1;
        private const byte KIND_I8 = 2;
        private const byte KIND_R4 = 3;
        private const byte KIND_R8 = 4;
        private const byte KIND_STR = 5;
        private const byte KIND_ENC = 0x80;

        private sealed class PendingEntry
        {
            public byte Kind;          // base kind (sin bit 0x80)
            public bool IsEncrypted;   // si la entrada en POOL va cifrada
            public int Index;          // índice en el directorio
            public int Offset;         // offset en POOL
            public int Length;         // longitud del blob (cipher o plain)
            public byte[] Plain;       // bytes en claro
            public byte[] Cipher;      // bytes cifrados (si Encrypt=true)
            public byte[] IV;          // 16 bytes (si Encrypt=false → ceros)
            public byte[] Tag;         // 16 bytes (si Encrypt=false → ceros)
        }

        public override async Task<bool> Execute(string modulePath)
        {
            try
            {
                var module = ModuleDefinition.FromFile(modulePath);
                var importer = new ReferenceImporter(module);
                var fac = module.CorLibTypeFactory;

                // Referencias de BCL usadas en IL:
                var initArray = importer.ImportMethod(
                    typeof(System.Runtime.CompilerServices.RuntimeHelpers)
                        .GetMethod(nameof(System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray),
                                   new[] { typeof(Array), typeof(RuntimeFieldHandle) }));

                var bitConvToI32 = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt32), new[] { typeof(byte[]), typeof(int) }));
                var bitConvToI64 = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt64), new[] { typeof(byte[]), typeof(int) }));
                var bitConvToSingle = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToSingle), new[] { typeof(byte[]), typeof(int) }));
                var bitConvToDouble = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToDouble), new[] { typeof(byte[]), typeof(int) }));

                var arrayCopy = importer.ImportMethod(typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) }));
                var encUnicodeGet = importer.ImportMethod(typeof(Encoding).GetProperty(nameof(Encoding.Unicode)).GetGetMethod());
                var encGetString = importer.ImportMethod(typeof(Encoding).GetMethod(nameof(Encoding.GetString), new[] { typeof(byte[]) })); // string

                var aesCtor = importer.ImportMethod(typeof(AesManaged).GetConstructor(Type.EmptyTypes));
                var symmCreateDec = importer.ImportMethod(typeof(SymmetricAlgorithm).GetMethod(nameof(SymmetricAlgorithm.CreateDecryptor), new[] { typeof(byte[]), typeof(byte[]) }));
                var ictfType = importer.ImportType(typeof(ICryptoTransform));
                var ictfFinalBlock = importer.ImportMethod(typeof(ICryptoTransform).GetMethod(nameof(ICryptoTransform.TransformFinalBlock), new[] { typeof(byte[]), typeof(int), typeof(int) }));

                var hmacCtor = importer.ImportMethod(typeof(HMACSHA256).GetConstructor(new[] { typeof(byte[]) }));
                var hmacCompute = importer.ImportMethod(typeof(HMAC).GetMethod(nameof(HMAC.ComputeHash), new[] { typeof(byte[]) }));

                var byteTypeRef = importer.ImportType(typeof(byte));
                var charTypeRef = importer.ImportType(typeof(char));

                // ====== 1) Recolectar literales y asignar índices ======
                var mapI4 = new Dictionary<int, int>();
                var mapI8 = new Dictionary<long, int>();
                var mapR4 = new Dictionary<int, int>();    // bits (float)
                var mapR8 = new Dictionary<long, int>();   // bits (double)
                var mapStr = new Dictionary<string, int>();

                var entries = new List<PendingEntry>();
                var poolBytes = new List<byte>();

                var shard1 = new byte[16];
                var shard2 = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(shard1);
                    rng.GetBytes(shard2);
                }
                var masterKey = Xor16(shard1, shard2);

                Func<byte, byte[], int> ReserveEntry = (baseKind, plain) =>
                {
                    bool enc = Encrypt;
                    var pe = new PendingEntry
                    {
                        Kind = baseKind,
                        IsEncrypted = enc,
                        Plain = plain ?? Array.Empty<byte>(),
                        IV = new byte[16],
                        Tag = new byte[16],
                    };

                    if (enc)
                    {
                        using (var rng = RandomNumberGenerator.Create())
                        {
                            rng.GetBytes(pe.IV);
                            rng.GetBytes(pe.Tag);
                        }

                        var ki = Hmac16(masterKey, pe.Tag);
                        pe.Cipher = AesCbcEnc(plain, ki, pe.IV);
                        pe.Length = pe.Cipher.Length;
                    }
                    else
                    {
                        pe.Cipher = pe.Plain;
                        pe.Length = pe.Plain.Length;
                        Array.Clear(pe.IV, 0, pe.IV.Length);
                        Array.Clear(pe.Tag, 0, pe.Tag.Length);
                    }

                    pe.Offset = poolBytes.Count;
                    poolBytes.AddRange(pe.Cipher);

                    pe.Index = entries.Count;
                    entries.Add(pe);
                    return pe.Index;
                };

                foreach (var type in module.GetAllTypes().ToArray())
                {
                    // no existe runtime helper aún, así que no hay que excluirlo aquí
                    foreach (var method in type.Methods.ToArray())
                    {
                        if (method == null || !method.HasMethodBody || method.CilMethodBody == null)
                            continue;

                        var instrs = method.CilMethodBody.Instructions;
                        if (instrs.Count == 0) continue;

                        for (int i = 0; i < instrs.Count; i++)
                        {
                            var ins = instrs[i];

                            if (ins.OpCode == CilOpCodes.Ldc_I8)
                            {
                                long v = (long)ins.Operand;
                                if (!mapI8.TryGetValue(v, out var idx))
                                {
                                    idx = ReserveEntry(KIND_I8, BitConverter.GetBytes(v));
                                    mapI8[v] = idx;
                                }
                            }
                            else if (ins.OpCode == CilOpCodes.Ldc_R8)
                            {
                                double v = (double)ins.Operand;
                                long bits = BitConverter.DoubleToInt64Bits(v);
                                if (!mapR8.TryGetValue(bits, out var idx))
                                {
                                    idx = ReserveEntry(KIND_R8, BitConverter.GetBytes(v));
                                    mapR8[bits] = idx;
                                }
                            }
                            else if (ins.OpCode == CilOpCodes.Ldc_R4)
                            {
                                float v = (float)ins.Operand;
                                int bits = BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
                                if (!mapR4.TryGetValue(bits, out var idx))
                                {
                                    idx = ReserveEntry(KIND_R4, BitConverter.GetBytes(v));
                                    mapR4[bits] = idx;
                                }
                            }
                            else if (TryGetI4Constant(ins, out int i4))
                            {
                                if (!mapI4.TryGetValue(i4, out var idx))
                                {
                                    idx = ReserveEntry(KIND_I4, BitConverter.GetBytes(i4));
                                    mapI4[i4] = idx;
                                }
                            }
                            else if (ObfuscateStrings && ins.OpCode == CilOpCodes.Ldstr)
                            {
                                string s = ins.Operand as string ?? string.Empty;
                                if (!mapStr.TryGetValue(s, out var idx))
                                {
                                    idx = ReserveEntry(KIND_STR, Encoding.Unicode.GetBytes(s));
                                    mapStr[s] = idx;
                                }
                            }
                        }
                    }
                }

                var pid = GetOrCreatePID(module);
                var poolField = CreateRvaBlob(module, poolBytes.ToArray(), "_POOL_");

                int N = entries.Count;
                var dirPlain = new byte[4 + N * ENTRY_SIZE];
                WriteInt32LE(dirPlain, 0, N);

                for (int i = 0; i < N; i++)
                {
                    var e = entries[i];
                    int p = 4 + i * ENTRY_SIZE;

                    byte kind = e.Kind;
                    if (e.IsEncrypted) kind |= KIND_ENC;

                    WriteInt32LE(dirPlain, p + 0, e.Offset);
                    WriteInt32LE(dirPlain, p + 4, e.Length);
                    dirPlain[p + 8] = kind;
                    // p+9..p+11 = padding 0
                    Array.Copy(e.IV, 0, dirPlain, p + 12, 16);
                    Array.Copy(e.Tag, 0, dirPlain, p + 28, 16);
                }

                var dirIV = new byte[16];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(dirIV);
                var dirCipher = AesCbcEnc(dirPlain, masterKey, dirIV);

                var dirField = CreateRvaBlob(module, dirCipher, "_DIR_");
                var dirIvField = CreateRvaBlob(module, dirIV, "_DIV_");

                var s1Field = CreateRvaBlob(module, shard1, "_KS1_");
                var s2Field = CreateRvaBlob(module, shard2, "_KS2_");

                var rt = BuildRuntimeHelper(
                    module, importer, initArray,
                    poolField, dirField, dirIvField,
                    s1Field, s2Field,
                    poolBytes.Count, dirCipher.Length, dirIV.Length);

                var decI4 = importer.ImportMethod(GetRuntimeMethod(rt, "DecI4ByIndex", 1));
                var decI8 = importer.ImportMethod(GetRuntimeMethod(rt, "DecI8ByIndex", 1));
                var decR4 = importer.ImportMethod(GetRuntimeMethod(rt, "DecR4ByIndex", 1));
                var decR8 = importer.ImportMethod(GetRuntimeMethod(rt, "DecR8ByIndex", 1));
                var decS = importer.ImportMethod(GetRuntimeMethod(rt, "DecStrByIndex", 1));

                foreach (var type in module.GetAllTypes().ToArray())
                {
                    if (type == rt || type.Name == "<PrivateImplementationDetails>")
                        continue;

                    foreach (var method in type.Methods.ToArray())
                    {
                        if (method == null || !method.HasMethodBody || method.CilMethodBody == null)
                            continue;

                        var instrs = method.CilMethodBody.Instructions;
                        if (instrs.Count == 0) continue;

                        for (int i = 0; i < instrs.Count; i++)
                        {
                            var ins = instrs[i];

                            if (ins.OpCode == CilOpCodes.Ldc_I8)
                            {
                                long v = (long)ins.Operand;
                                if (!mapI8.TryGetValue(v, out var idx)) continue; // literal no reservado
                                ins.ReplaceWith(CilOpCodes.Ldc_I4, idx);
                                instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, decI8));
                                continue;
                            }
                            if (ins.OpCode == CilOpCodes.Ldc_R8)
                            {
                                double v = (double)ins.Operand;
                                long bits = BitConverter.DoubleToInt64Bits(v);
                                if (!mapR8.TryGetValue(bits, out var idx)) continue;
                                ins.ReplaceWith(CilOpCodes.Ldc_I4, idx);
                                instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, decR8));
                                continue;
                            }
                            if (ins.OpCode == CilOpCodes.Ldc_R4)
                            {
                                float v = (float)ins.Operand;
                                int bits = BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
                                if (!mapR4.TryGetValue(bits, out var idx)) continue;
                                ins.ReplaceWith(CilOpCodes.Ldc_I4, idx);
                                instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, decR4));
                                continue;
                            }
                            if (TryGetI4Constant(ins, out int i4))
                            {
                                if (!mapI4.TryGetValue(i4, out var idx)) continue;
                                ins.ReplaceWith(CilOpCodes.Ldc_I4, idx);
                                instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, decI4));
                                continue;
                            }
                            if (ObfuscateStrings && ins.OpCode == CilOpCodes.Ldstr)
                            {
                                string s = ins.Operand as string ?? string.Empty;
                                if (!mapStr.TryGetValue(s, out var idx)) continue;
                                ins.ReplaceWith(CilOpCodes.Ldc_I4, idx);
                                instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, decS));
                                continue;
                            }
                        }
                    }
                }

                // ====== 5) Escribir módulo ======
                using (var ms = new MemoryStream())
                {
                    module.Write(ms);
                    TempModule = ms;
                }
                if (TempModule == null) throw new Exception("MemoryStream is null");
                return true;
            }
            catch (Exception ex)
            {
                this.Errors = ex;
                return false;
            }
        }

        private static TypeDefinition BuildRuntimeHelper(
            ModuleDefinition module,
            ReferenceImporter importer,
            IMethodDescriptor initArray,
            FieldDefinition poolRvaField,
            FieldDefinition dirCipherRvaField,
            FieldDefinition dirIvRvaField,
            FieldDefinition shard1RvaField,
            FieldDefinition shard2RvaField,
            int poolLen, int dirCipherLen, int dirIvLen)
        {
            var fac = module.CorLibTypeFactory;

            var rt = new TypeDefinition(
                string.Empty,
                "<HydraRvaRuntime>",
                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                fac.Object.Type);
            module.TopLevelTypes.Add(rt);

            // Campos estáticos:
            var byteArrSig = new SzArrayTypeSignature(fac.Byte);

            var fldP = new FieldDefinition("P", FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, new FieldSignature(byteArrSig)); // POOL
            var fldD = new FieldDefinition("D", FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, new FieldSignature(byteArrSig)); // DIR plano
            var fldK = new FieldDefinition("K", FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, new FieldSignature(byteArrSig)); // clave maestra (16)
            rt.Fields.Add(fldP);
            rt.Fields.Add(fldD);
            rt.Fields.Add(fldK);

            // ===== Métodos auxiliares =====

            // byte[] DecBlock(byte[] c, byte[] iv, byte[] key)
            var aesCtor = importer.ImportMethod(typeof(AesManaged).GetConstructor(Type.EmptyTypes));
            var symmCreateDec = importer.ImportMethod(typeof(SymmetricAlgorithm).GetMethod(nameof(SymmetricAlgorithm.CreateDecryptor), new[] { typeof(byte[]), typeof(byte[]) }));
            var ictfType = importer.ImportType(typeof(ICryptoTransform));
            var tfb = importer.ImportMethod(typeof(ICryptoTransform).GetMethod(nameof(ICryptoTransform.TransformFinalBlock), new[] { typeof(byte[]), typeof(int), typeof(int) }));

            var decBlock = new MethodDefinition("DecBlock",
                MethodAttributes.Private | MethodAttributes.Static,
                MethodSignature.CreateStatic(byteArrSig, byteArrSig, byteArrSig, byteArrSig)); // (c, iv, key)
            rt.Methods.Add(decBlock);
            {
                var body = new CilMethodBody(decBlock);
                decBlock.CilMethodBody = body;
                var il = body.Instructions;

                var trLocal = new CilLocalVariable(ictfType.ToTypeSignature());
                body.LocalVariables.Add(trLocal);

                il.Add(CilOpCodes.Newobj, aesCtor);
                il.Add(CilOpCodes.Ldarg_2);
                il.Add(CilOpCodes.Ldarg_1);
                il.Add(CilOpCodes.Callvirt, symmCreateDec);
                il.Add(CilOpCodes.Stloc, trLocal);

                il.Add(CilOpCodes.Ldloc, trLocal);
                il.Add(CilOpCodes.Ldarg_0);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldarg_0);
                il.Add(CilOpCodes.Ldlen);
                il.Add(CilOpCodes.Conv_I4);
                il.Add(CilOpCodes.Callvirt, tfb);
                il.Add(CilOpCodes.Ret);
            }

            // byte[] DeriveKi(byte[] tag) => HMACSHA256(K).ComputeHash(tag)[..16]
            var hmacCtor = importer.ImportMethod(typeof(HMACSHA256).GetConstructor(new[] { typeof(byte[]) }));
            var hmacCompute = importer.ImportMethod(typeof(HMAC).GetMethod(nameof(HMAC.ComputeHash), new[] { typeof(byte[]) }));
            var arrayCopy = importer.ImportMethod(typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) }));

            var deriveKi = new MethodDefinition("DeriveKi",
                MethodAttributes.Private | MethodAttributes.Static,
                MethodSignature.CreateStatic(byteArrSig, byteArrSig)); // (tag)
            rt.Methods.Add(deriveKi);
            {
                var body = new CilMethodBody(deriveKi);
                deriveKi.CilMethodBody = body;
                var il = body.Instructions;

                var hashLocal = new CilLocalVariable(byteArrSig);
                var kiLocal = new CilLocalVariable(byteArrSig);
                body.LocalVariables.Add(hashLocal);
                body.LocalVariables.Add(kiLocal);

                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "K"));
                il.Add(CilOpCodes.Newobj, hmacCtor);
                il.Add(CilOpCodes.Ldarg_0);
                il.Add(CilOpCodes.Callvirt, hmacCompute);
                il.Add(CilOpCodes.Stloc, hashLocal);

                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, importer.ImportType(typeof(byte)));
                il.Add(CilOpCodes.Stloc, kiLocal);

                il.Add(CilOpCodes.Ldloc, hashLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldloc, kiLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Call, arrayCopy);

                il.Add(CilOpCodes.Ldloc, kiLocal);
                il.Add(CilOpCodes.Ret);
            }

            // byte[] DecBytesByIndex(int idx)
            var bitToI32 = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt32), new[] { typeof(byte[]), typeof(int) }));

            var decBytes = new MethodDefinition("DecBytesByIndex",
                MethodAttributes.Private | MethodAttributes.Static,
                MethodSignature.CreateStatic(byteArrSig, fac.Int32)); // (idx)
            rt.Methods.Add(decBytes);
            {
                var body = new CilMethodBody(decBytes);
                decBytes.CilMethodBody = body;
                var il = body.Instructions;

                var baseLocal = new CilLocalVariable(fac.Int32);
                var offLocal = new CilLocalVariable(fac.Int32);
                var lenLocal = new CilLocalVariable(fac.Int32);
                var kindLocal = new CilLocalVariable(fac.Int32);
                var ivLocal = new CilLocalVariable(new SzArrayTypeSignature(fac.Byte));
                var tagLocal = new CilLocalVariable(new SzArrayTypeSignature(fac.Byte));
                var cLocal = new CilLocalVariable(new SzArrayTypeSignature(fac.Byte));
                var pLocal = new CilLocalVariable(new SzArrayTypeSignature(fac.Byte));
                var kiLocal = new CilLocalVariable(new SzArrayTypeSignature(fac.Byte));

                body.LocalVariables.Add(baseLocal);
                body.LocalVariables.Add(offLocal);
                body.LocalVariables.Add(lenLocal);
                body.LocalVariables.Add(kindLocal);
                body.LocalVariables.Add(ivLocal);
                body.LocalVariables.Add(tagLocal);
                body.LocalVariables.Add(cLocal);
                body.LocalVariables.Add(pLocal);
                body.LocalVariables.Add(kiLocal);

                il.Add(CilOpCodes.Ldarg_0);
                il.Add(CilOpCodes.Ldc_I4, ENTRY_SIZE);
                il.Add(CilOpCodes.Mul);
                il.Add(CilOpCodes.Ldc_I4, 4);
                il.Add(CilOpCodes.Add);
                il.Add(CilOpCodes.Stloc, baseLocal);

                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "D"));
                il.Add(CilOpCodes.Ldloc, baseLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Add);
                il.Add(CilOpCodes.Call, bitToI32);
                il.Add(CilOpCodes.Stloc, offLocal);

                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "D"));
                il.Add(CilOpCodes.Ldloc, baseLocal);
                il.Add(CilOpCodes.Ldc_I4, 4);
                il.Add(CilOpCodes.Add);
                il.Add(CilOpCodes.Call, bitToI32);
                il.Add(CilOpCodes.Stloc, lenLocal);

                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "D"));
                il.Add(CilOpCodes.Ldloc, baseLocal);
                il.Add(CilOpCodes.Ldc_I4, 8);
                il.Add(CilOpCodes.Add);
                il.Add(CilOpCodes.Ldelem_U1);
                il.Add(CilOpCodes.Stloc, kindLocal);

                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, importer.ImportType(typeof(byte)));
                il.Add(CilOpCodes.Stloc, ivLocal);

                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "D"));
                il.Add(CilOpCodes.Ldloc, baseLocal);
                il.Add(CilOpCodes.Ldc_I4, 12);
                il.Add(CilOpCodes.Add);
                il.Add(CilOpCodes.Ldloc, ivLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Call, importer.ImportMethod(typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })));

                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, importer.ImportType(typeof(byte)));
                il.Add(CilOpCodes.Stloc, tagLocal);

                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "D"));
                il.Add(CilOpCodes.Ldloc, baseLocal);
                il.Add(CilOpCodes.Ldc_I4, 28);
                il.Add(CilOpCodes.Add);
                il.Add(CilOpCodes.Ldloc, tagLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Call, importer.ImportMethod(typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })));

                il.Add(CilOpCodes.Ldloc, lenLocal);
                il.Add(CilOpCodes.Newarr, importer.ImportType(typeof(byte)));
                il.Add(CilOpCodes.Stloc, cLocal);

                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "P"));
                il.Add(CilOpCodes.Ldloc, offLocal);
                il.Add(CilOpCodes.Ldloc, cLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldloc, lenLocal);
                il.Add(CilOpCodes.Call, importer.ImportMethod(typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })));

                var lblRetInstr = new CilInstr(CilOpCodes.Nop);
                var lblRet = new CilInstructionLabel(lblRetInstr);

                il.Add(CilOpCodes.Ldloc, kindLocal);
                il.Add(CilOpCodes.Ldc_I4, KIND_ENC);
                il.Add(CilOpCodes.And);
                il.Add(CilOpCodes.Brfalse, lblRet);

                il.Add(CilOpCodes.Ldloc, tagLocal);
                il.Add(CilOpCodes.Call, GetRuntimeMethod(rt, "DeriveKi", 1));
                il.Add(CilOpCodes.Stloc, kiLocal);

                il.Add(CilOpCodes.Ldloc, cLocal);
                il.Add(CilOpCodes.Ldloc, ivLocal);
                il.Add(CilOpCodes.Ldloc, kiLocal);
                il.Add(CilOpCodes.Call, GetRuntimeMethod(rt, "DecBlock", 3));
                il.Add(CilOpCodes.Stloc, pLocal);
                il.Add(CilOpCodes.Ldloc, pLocal);
                il.Add(CilOpCodes.Ret);

                il.Add(lblRetInstr);
                il.Add(CilOpCodes.Ldloc, cLocal);
                il.Add(CilOpCodes.Ret);
            }

            // Wrappers públicos
            var bitI32 = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt32), new[] { typeof(byte[]), typeof(int) }));
            var bitI64 = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt64), new[] { typeof(byte[]), typeof(int) }));
            var bitR4 = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToSingle), new[] { typeof(byte[]), typeof(int) }));
            var bitR8 = importer.ImportMethod(typeof(BitConverter).GetMethod(nameof(BitConverter.ToDouble), new[] { typeof(byte[]), typeof(int) }));
            var encGet = importer.ImportMethod(typeof(Encoding).GetProperty(nameof(Encoding.Unicode)).GetGetMethod());
            var encStr = importer.ImportMethod(typeof(Encoding).GetMethod(nameof(Encoding.GetString), new[] { typeof(byte[]) }));

            rt.Methods.Add(BuildWrapperNumeric(rt, "DecI4ByIndex", module.CorLibTypeFactory.Int32, bitI32));
            rt.Methods.Add(BuildWrapperNumeric(rt, "DecI8ByIndex", module.CorLibTypeFactory.Int64, bitI64));
            rt.Methods.Add(BuildWrapperNumeric(rt, "DecR4ByIndex", module.CorLibTypeFactory.Single, bitR4));
            rt.Methods.Add(BuildWrapperNumeric(rt, "DecR8ByIndex", module.CorLibTypeFactory.Double, bitR8));
            rt.Methods.Add(BuildWrapperString(rt, "DecStrByIndex", module.CorLibTypeFactory.String, encGet, encStr));

            // ===== .cctor =====
            var cctor = new MethodDefinition(".cctor",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                MethodSignature.CreateStatic(module.CorLibTypeFactory.Void));
            rt.Methods.Add(cctor);
            {
                var body = new CilMethodBody(cctor);
                cctor.CilMethodBody = body;
                var il = body.Instructions;

                var byteType = importer.ImportType(typeof(byte));

                var ks1Local = new CilLocalVariable(new SzArrayTypeSignature(module.CorLibTypeFactory.Byte));
                body.LocalVariables.Add(ks1Local);

                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, byteType);
                il.Add(CilOpCodes.Dup);
                il.Add(CilOpCodes.Ldtoken, shard1RvaField);
                il.Add(CilOpCodes.Call, initArray);
                il.Add(CilOpCodes.Stloc, ks1Local);

                var ks2Local = new CilLocalVariable(new SzArrayTypeSignature(module.CorLibTypeFactory.Byte));
                body.LocalVariables.Add(ks2Local);

                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, byteType);
                il.Add(CilOpCodes.Dup);
                il.Add(CilOpCodes.Ldtoken, shard2RvaField);
                il.Add(CilOpCodes.Call, initArray);
                il.Add(CilOpCodes.Stloc, ks2Local);

                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, byteType);
                il.Add(CilOpCodes.Stsfld, rt.Fields.First(f => f.Name == "K"));
                for (int i = 0; i < 16; i++)
                {
                    il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "K"));
                    il.Add(CilOpCodes.Ldc_I4, i);

                    il.Add(CilOpCodes.Ldloc, ks1Local);
                    il.Add(CilOpCodes.Ldc_I4, i);
                    il.Add(CilOpCodes.Ldelem_U1);

                    il.Add(CilOpCodes.Ldloc, ks2Local);
                    il.Add(CilOpCodes.Ldc_I4, i);
                    il.Add(CilOpCodes.Ldelem_U1);

                    il.Add(CilOpCodes.Xor);
                    il.Add(CilOpCodes.Conv_U1);
                    il.Add(CilOpCodes.Stelem_I1);
                }

                il.Add(CilOpCodes.Ldc_I4, poolLen);
                il.Add(CilOpCodes.Newarr, byteType);
                il.Add(CilOpCodes.Dup);
                il.Add(CilOpCodes.Ldtoken, poolRvaField);
                il.Add(CilOpCodes.Call, initArray);
                il.Add(CilOpCodes.Stsfld, rt.Fields.First(f => f.Name == "P"));

                var dcLocal = new CilLocalVariable(new SzArrayTypeSignature(module.CorLibTypeFactory.Byte));
                body.LocalVariables.Add(dcLocal);

                il.Add(CilOpCodes.Ldc_I4, dirCipherLen);
                il.Add(CilOpCodes.Newarr, byteType);
                il.Add(CilOpCodes.Dup);
                il.Add(CilOpCodes.Ldtoken, dirCipherRvaField);
                il.Add(CilOpCodes.Call, initArray);
                il.Add(CilOpCodes.Stloc, dcLocal);

                var divLocal = new CilLocalVariable(new SzArrayTypeSignature(module.CorLibTypeFactory.Byte));
                body.LocalVariables.Add(divLocal);

                il.Add(CilOpCodes.Ldc_I4, dirIvLen);
                il.Add(CilOpCodes.Newarr, byteType);
                il.Add(CilOpCodes.Dup);
                il.Add(CilOpCodes.Ldtoken, dirIvRvaField);
                il.Add(CilOpCodes.Call, initArray);
                il.Add(CilOpCodes.Stloc, divLocal);

                il.Add(CilOpCodes.Ldloc, dcLocal);
                il.Add(CilOpCodes.Ldloc, divLocal);
                il.Add(CilOpCodes.Ldsfld, rt.Fields.First(f => f.Name == "K"));
                il.Add(CilOpCodes.Call, GetRuntimeMethod(rt, "DecBlock", 3));
                il.Add(CilOpCodes.Stsfld, rt.Fields.First(f => f.Name == "D"));

                il.Add(CilOpCodes.Ret);
            }

            return rt;
        }

        private static MethodDefinition BuildWrapperNumeric(
            TypeDefinition rt, string name, CorLibTypeSignature retSig, IMethodDescriptor bitConv)
        {
            var m = new MethodDefinition(name,
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(retSig, rt.Module.CorLibTypeFactory.Int32)); // (int idx)

            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;

            var bytesLocal = new CilLocalVariable(new SzArrayTypeSignature(rt.Module.CorLibTypeFactory.Byte));
            body.LocalVariables.Add(bytesLocal);

            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Call, GetRuntimeMethod(rt, "DecBytesByIndex", 1));
            il.Add(CilOpCodes.Stloc, bytesLocal);

            il.Add(CilOpCodes.Ldloc, bytesLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Call, bitConv);
            il.Add(CilOpCodes.Ret);

            return m;
        }

        private static MethodDefinition BuildWrapperString(
            TypeDefinition rt, string name, CorLibTypeSignature retSig, IMethodDescriptor encGet, IMethodDescriptor encStr)
        {
            var m = new MethodDefinition(name,
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(retSig, rt.Module.CorLibTypeFactory.Int32)); // (int idx)

            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;

            var bytesLocal = new CilLocalVariable(new SzArrayTypeSignature(rt.Module.CorLibTypeFactory.Byte));
            body.LocalVariables.Add(bytesLocal);

            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Call, GetRuntimeMethod(rt, "DecBytesByIndex", 1));
            il.Add(CilOpCodes.Stloc, bytesLocal);

            il.Add(CilOpCodes.Call, encGet);
            il.Add(CilOpCodes.Ldloc, bytesLocal);
            il.Add(CilOpCodes.Callvirt, encStr);
            il.Add(CilOpCodes.Ret);

            return m;
        }

        private static MethodDefinition GetRuntimeMethod(TypeDefinition helper, string name, int paramCount)
        {
            var md = helper?.Methods.FirstOrDefault(m =>
                m.Name == name &&
                m.Signature is MethodSignature sig &&
                sig.ParameterTypes.Count == paramCount);

            if (md == null)
                throw new InvalidOperationException($"No se encontró {name} en {helper?.Name}.");
            return md;
        }

        // ================= Utilidades de build-time =================

        private static byte[] AesCbcEnc(byte[] plain, byte[] key16, byte[] iv16)
        {
            using (var aes = new AesManaged { KeySize = 128, BlockSize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            {
                aes.Key = key16;
                aes.IV = iv16;
                using (var tr = aes.CreateEncryptor())
                {
                    var src = plain ?? Array.Empty<byte>();
                    return tr.TransformFinalBlock(src, 0, src.Length);
                }
            }
        }

        private static byte[] HmacSha256(byte[] key, byte[] data)
        {
            using (var h = new HMACSHA256(key))
            {
                return h.ComputeHash(data);
            }
        }

        private static byte[] Hmac16(byte[] key, byte[] tag16)
        {
            var full = HmacSha256(key, tag16 ?? Array.Empty<byte>());
            var ki = new byte[16];
            Buffer.BlockCopy(full, 0, ki, 0, 16);
            return ki;
        }

        private static byte[] Xor16(byte[] a, byte[] b)
        {
            var r = new byte[16];
            for (int i = 0; i < 16; i++) r[i] = (byte)(a[i] ^ b[i]);
            return r;
        }

        private static void WriteInt32LE(byte[] buf, int offset, int value)
        {
            var b = BitConverter.GetBytes(value);
            Buffer.BlockCopy(b, 0, buf, offset, 4);
        }

        private static bool TryGetI4Constant(CilInstruction ins, out int value)
        {
            switch (ins.OpCode.Code)
            {
                case CilCode.Ldc_I4: value = (int)ins.Operand; return true;
                case CilCode.Ldc_I4_S: value = (sbyte)ins.Operand; return true;
                case CilCode.Ldc_I4_M1: value = -1; return true;
                case CilCode.Ldc_I4_0: value = 0; return true;
                case CilCode.Ldc_I4_1: value = 1; return true;
                case CilCode.Ldc_I4_2: value = 2; return true;
                case CilCode.Ldc_I4_3: value = 3; return true;
                case CilCode.Ldc_I4_4: value = 4; return true;
                case CilCode.Ldc_I4_5: value = 5; return true;
                case CilCode.Ldc_I4_6: value = 6; return true;
                case CilCode.Ldc_I4_7: value = 7; return true;
                case CilCode.Ldc_I4_8: value = 8; return true;
                default: value = 0; return false;
            }
        }

        private static FieldDefinition CreateRvaBlob(ModuleDefinition module, byte[] data, string prefix)
        {
            if (data == null) data = Array.Empty<byte>();
            var name = prefix + Guid.NewGuid().ToString("N");
            var field = new FieldDefinition(
                name,
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(GetStaticArrayType(module, data.Length).ToTypeSignature()));
            field.FieldRva = new DataSegment(data);
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        private static TypeDefinition GetOrCreatePID(ModuleDefinition module)
        {
            var pid = module.TopLevelTypes.FirstOrDefault(t => t.Name == "<PrivateImplementationDetails>");
            if (pid != null) return pid;

            pid = new TypeDefinition(
                string.Empty,
                "<PrivateImplementationDetails>",
                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                module.CorLibTypeFactory.Object.Type);
            module.TopLevelTypes.Add(pid);
            return pid;
        }

        private static TypeDefinition GetOrCreateStaticArrayType(ModuleDefinition module, int size, Dictionary<int, TypeDefinition> cache)
        {
            if (cache.TryGetValue(size, out var t)) return t;

            var pid = GetOrCreatePID(module);
            var name = "__StaticArrayInitTypeSize=" + size;

            t = pid.NestedTypes.FirstOrDefault(x => x.Name == name);
            if (t != null) { cache[size] = t; return t; }

            var valueTypeRef = new TypeReference(module, module.CorLibTypeFactory.CorLibScope, "System", "ValueType");

            t = new TypeDefinition(
                string.Empty,
                name,
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.ExplicitLayout | TypeAttributes.BeforeFieldInit,
                valueTypeRef);
            t.ClassLayout = new ClassLayout(1, (uint)size);

            pid.NestedTypes.Add(t);
            cache[size] = t;
            return t;
        }

        private static TypeDefinition GetStaticArrayType(ModuleDefinition module, int size)
        {
            var pid = GetOrCreatePID(module);
            var name = "__StaticArrayInitTypeSize=" + size;
            var existing = pid.NestedTypes.FirstOrDefault(x => x.Name == name);
            if (existing != null) return existing;
            return GetOrCreateStaticArrayType(module, size, new Dictionary<int, TypeDefinition>());
        }

        // dnlib overload (puente)
        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            string temp = Path.Combine(Path.GetTempPath(), module.Name);
            try { module.Write(temp); } catch (Exception ex) { this.Errors = ex; }
            return Execute(temp);
        }
    }
}