using System;
using System.IO;
using System.Linq;
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
using AsmResolver.PE.DotNet.Cil;            // CilOpCodes, CilInstruction
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;
using CilInstr = AsmResolver.PE.DotNet.Cil.CilInstruction;
using EXGuard.Core;

namespace HydraEngine.Protection.Integer
{
    public class StrIntToRva : Models.Protection
    {
        public bool ObfuscateStrings { get; set; } = true;

        public bool Encrypt { get; set; } = true;

        public StrIntToRva()
            : base("Protection.Renamer.StrIntToRva", "Renamer Phase",
                   "Integers & Strings → RVA indirection (IL-only)")
        {
            ManualReload = true;
        }

        public override async Task<bool> Execute(string modulePath)
        {
            try
            {
                var module = ModuleDefinition.FromFile(modulePath);
                var importer = new ReferenceImporter(module);
                var fac = module.CorLibTypeFactory;

                var initArray = importer.ImportMethod(
                    typeof(System.Runtime.CompilerServices.RuntimeHelpers)
                        .GetMethod(nameof(System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray),
                                   new[] { typeof(Array), typeof(RuntimeFieldHandle) })
                );
                var stringCtorCharArray = importer.ImportMethod(typeof(string).GetConstructor(new[] { typeof(char[]) }));
                var stringEmptyField = importer.ImportField(typeof(string).GetField(nameof(string.Empty)));
                var charTypeRef = importer.ImportType(typeof(char));
                var byteTypeRef = importer.ImportType(typeof(byte));

                TypeDefinition runtimeHelper = null;
                List<FieldDefinition> keyShards = null;
                byte[] masterKey = null;
                if (Encrypt)
                {
                    keyShards = new List<FieldDefinition>();
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        for (int s = 0; s < 4; s++)
                        {
                            var shard = new byte[16];
                            rng.GetBytes(shard);
                            keyShards.Add(CreateRvaBlob(module, shard, "_K" + s + "_"));
                        }
                    }
                    masterKey = ComputeMasterKeyFromShards(keyShards);
                    runtimeHelper = BuildRuntimeHelper(module, importer, initArray, keyShards);
                }

                bool IsInfra(TypeDefinition t)
                    => t?.Name == "<HydraRvaRuntime>" || t?.Name == "<PrivateImplementationDetails>";

                // Caches
                var i4Cache = new Dictionary<int, FieldDefinition>();
                var i8Cache = new Dictionary<long, FieldDefinition>();
                var r4Cache = new Dictionary<int, FieldDefinition>();   // bits de float
                var r8Cache = new Dictionary<long, FieldDefinition>();  // bits de double
                var strCache = new Dictionary<string, (FieldDefinition field, int length)>();
                var sizedStructCache = new Dictionary<int, TypeDefinition>();

                // ========= STRINGS =========
                if (ObfuscateStrings)
                {
                    foreach (var type in module.GetAllTypes().ToArray())
                    {
                        if (IsInfra(type)) continue;

                        foreach (var method in type.Methods.ToArray())
                        {
                            if (method is null || !method.HasMethodBody || method.CilMethodBody is null)
                                continue;

                            var instrs = method.CilMethodBody.Instructions;
                            if (instrs.Count == 0) continue;

                            for (int i = 0; i < instrs.Count; i++)
                            {
                                var ins = instrs[i];
                                if (ins.OpCode != CilOpCodes.Ldstr) continue;

                                var content = ins.Operand as string ?? string.Empty;

                                // "" -> String.Empty (evita blobs de tamaño 0)
                                if (content.Length == 0)
                                {
                                    ins.ReplaceWith(CilOpCodes.Ldsfld, stringEmptyField);
                                    continue;
                                }

                                if (!Encrypt)
                                {
                                    if (!strCache.TryGetValue(content, out var info))
                                    {
                                        var field = CreateRvaStringBlob(module, content, sizedStructCache);
                                        info = (field, content.Length);
                                        strCache[content] = info;
                                    }

                                    ins.ReplaceWith(CilOpCodes.Ldc_I4, info.length);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, charTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, info.field));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newobj, stringCtorCharArray));
                                }
                                else
                                {
                                    var plain = System.Text.Encoding.Unicode.GetBytes(content);
                                    var iv = new byte[16];
                                    var tag = new byte[16];
                                    using (var rng = RandomNumberGenerator.Create())
                                    {
                                        rng.GetBytes(iv);
                                        rng.GetBytes(tag);
                                    }

                                    var ki = DerivePerLiteralKey(masterKey, tag);
                                    var cipher = EncryptAes(plain, ki, iv);

                                    var cField = CreateRvaBlob(module, cipher, "_CSTR_");
                                    var iField = CreateRvaBlob(module, iv, "_IV_");
                                    var tField = CreateRvaBlob(module, tag, "_TAG_");

                                    // byte[] cipher
                                    ins.ReplaceWith(CilOpCodes.Ldc_I4, cipher.Length);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, cField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));
                                    // byte[] iv
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, iv.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, iField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));
                                    // byte[] tag
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, tag.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, tField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    var decStr = importer.ImportMethod(GetRuntimeMethod(runtimeHelper, "DecStr", 3));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, decStr));
                                }
                            }
                        }
                    }
                }

                // ========= NUMÉRICOS =========
                foreach (var type in module.GetAllTypes().ToArray())
                {
                    if (IsInfra(type)) continue;

                    foreach (var method in type.Methods.ToArray())
                    {
                        if (method is null || !method.HasMethodBody || method.CilMethodBody is null)
                            continue;

                        var instrs = method.CilMethodBody.Instructions;
                        if (instrs.Count == 0) continue;

                        for (int i = 0; i < instrs.Count; i++)
                        {
                            var ins = instrs[i];

                            // Int64
                            if (ins.OpCode == CilOpCodes.Ldc_I8)
                            {
                                long v = (long)ins.Operand;

                                if (!Encrypt)
                                {
                                    if (!i8Cache.TryGetValue(v, out var f))
                                        i8Cache[v] = f = CreateRvaI8Field(module, v);

                                    ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldind_I8));
                                }
                                else
                                {
                                    var iv = new byte[16];
                                    var tag = new byte[16];
                                    using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(iv); rng.GetBytes(tag); }
                                    var ki = DerivePerLiteralKey(masterKey, tag);
                                    var cipher = EncryptAes(BitConverter.GetBytes(v), ki, iv);

                                    var cField = CreateRvaBlob(module, cipher, "_CI8_");
                                    var iField = CreateRvaBlob(module, iv, "_IV_");
                                    var tField = CreateRvaBlob(module, tag, "_TAG_");

                                    ins.ReplaceWith(CilOpCodes.Ldc_I4, cipher.Length);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, cField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, iv.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, iField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, tag.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, tField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    var dec = importer.ImportMethod(GetRuntimeMethod(runtimeHelper, "DecI8", 3));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, dec));
                                }
                                continue;
                            }

                            if (TryGetI4Constant(ins, out int i4))
                            {
                                if (!Encrypt)
                                {
                                    if (!i4Cache.TryGetValue(i4, out var f))
                                        i4Cache[i4] = f = CreateRvaI4Field(module, i4);

                                    ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldind_I4));
                                }
                                else
                                {
                                    var iv = new byte[16];
                                    var tag = new byte[16];
                                    using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(iv); rng.GetBytes(tag); }
                                    var ki = DerivePerLiteralKey(masterKey, tag);
                                    var cipher = EncryptAes(BitConverter.GetBytes(i4), ki, iv);

                                    var cField = CreateRvaBlob(module, cipher, "_CI4_");
                                    var iField = CreateRvaBlob(module, iv, "_IV_");
                                    var tField = CreateRvaBlob(module, tag, "_TAG_");

                                    ins.ReplaceWith(CilOpCodes.Ldc_I4, cipher.Length);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, cField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, iv.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, iField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, tag.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, tField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    var dec = importer.ImportMethod(GetRuntimeMethod(runtimeHelper, "DecI4", 3));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, dec));
                                }
                                continue;
                            }

                            // Double
                            if (ins.OpCode == CilOpCodes.Ldc_R8)
                            {
                                double v = (double)ins.Operand;

                                if (!Encrypt)
                                {
                                    long bits = BitConverter.DoubleToInt64Bits(v);
                                    if (!r8Cache.TryGetValue(bits, out var f))
                                        r8Cache[bits] = f = CreateRvaR8Field(module, v);

                                    ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldind_R8));
                                }
                                else
                                {
                                    var iv = new byte[16];
                                    var tag = new byte[16];
                                    using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(iv); rng.GetBytes(tag); }
                                    var ki = DerivePerLiteralKey(masterKey, tag);
                                    var cipher = EncryptAes(BitConverter.GetBytes(v), ki, iv);

                                    var cField = CreateRvaBlob(module, cipher, "_CR8_");
                                    var iField = CreateRvaBlob(module, iv, "_IV_");
                                    var tField = CreateRvaBlob(module, tag, "_TAG_");

                                    ins.ReplaceWith(CilOpCodes.Ldc_I4, cipher.Length);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, cField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, iv.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, iField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, tag.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, tField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    var dec = importer.ImportMethod(GetRuntimeMethod(runtimeHelper, "DecR8", 3));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, dec));
                                }
                                continue;
                            }

                            // Float
                            if (ins.OpCode == CilOpCodes.Ldc_R4)
                            {
                                float v = (float)ins.Operand;

                                if (!Encrypt)
                                {
                                    int bits = BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
                                    if (!r4Cache.TryGetValue(bits, out var f))
                                        r4Cache[bits] = f = CreateRvaR4Field(module, v);

                                    ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldind_R4));
                                }
                                else
                                {
                                    var iv = new byte[16];
                                    var tag = new byte[16];
                                    using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(iv); rng.GetBytes(tag); }
                                    var ki = DerivePerLiteralKey(masterKey, tag);
                                    var cipher = EncryptAes(BitConverter.GetBytes(v), ki, iv);

                                    var cField = CreateRvaBlob(module, cipher, "_CR4_");
                                    var iField = CreateRvaBlob(module, iv, "_IV_");
                                    var tField = CreateRvaBlob(module, tag, "_TAG_");

                                    ins.ReplaceWith(CilOpCodes.Ldc_I4, cipher.Length);
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, cField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, iv.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, iField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldc_I4, tag.Length));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Newarr, byteTypeRef));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Dup));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Ldtoken, tField));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, initArray));

                                    var dec = importer.ImportMethod(GetRuntimeMethod(runtimeHelper, "DecR4", 3));
                                    instrs.Insert(++i, new CilInstr(0, CilOpCodes.Call, dec));
                                }
                                continue;
                            }
                        }
                    }
                }

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

        private static byte[] EncryptAes(byte[] plain, byte[] key, byte[] iv)
        {
            using (var aes = new AesManaged { KeySize = 128, BlockSize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            {
                aes.Key = key;
                aes.IV = iv;
                using (var tr = aes.CreateEncryptor())
                    return tr.TransformFinalBlock(plain, 0, plain.Length);
            }
        }

        private static byte[] DerivePerLiteralKey(byte[] masterKey, byte[] tag)
        {
            using (var h = new HMACSHA256(masterKey))
            {
                var full = h.ComputeHash(tag);       // 32B
                var ki = new byte[16];
                Buffer.BlockCopy(full, 0, ki, 0, 16);
                return ki;
            }
        }

        private static byte[] ComputeMasterKeyFromShards(IReadOnlyList<FieldDefinition> shards)
        {
            var concat = new byte[16 * shards.Count];
            int off = 0;
            foreach (var f in shards)
            {
                var b = ReadRvaFieldBytes(f);
                Buffer.BlockCopy(b, 0, concat, off, 16);
                off += 16;
            }
            using (var sha = SHA256.Create())
            {
                var full = sha.ComputeHash(concat); // 32B
                var k = new byte[16];
                Buffer.BlockCopy(full, 0, k, 0, 16);
                return k;
            }
        }

        private static byte[] ReadRvaFieldBytes(FieldDefinition fieldWithRva)
        {
            if (fieldWithRva?.FieldRva is DataSegment seg)
                return seg.Data.ToArray();
            return Array.Empty<byte>();
        }

        private static FieldDefinition CreateRvaBlob(ModuleDefinition module, byte[] data, string prefix)
        {
            if (data == null || data.Length == 0)
                throw new InvalidOperationException("No se permiten blobs RVA vacíos.");

            var name = prefix + Guid.NewGuid().ToString("N");
            var field = new FieldDefinition(
                name,
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(GetStaticArrayType(module, data.Length).ToTypeSignature()));
            field.FieldRva = new DataSegment(data);
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        private static FieldDefinition CreateRvaI4Field(ModuleDefinition module, int value)
        {
            var fac = module.CorLibTypeFactory;
            var field = new FieldDefinition(
                "_I4_" + Guid.NewGuid().ToString("N"),
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(fac.Int32));
            field.FieldRva = new DataSegment(BitConverter.GetBytes(value));
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        private static FieldDefinition CreateRvaI8Field(ModuleDefinition module, long value)
        {
            var fac = module.CorLibTypeFactory;
            var field = new FieldDefinition(
                "_I8_" + Guid.NewGuid().ToString("N"),
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(fac.Int64));
            field.FieldRva = new DataSegment(BitConverter.GetBytes(value));
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        private static FieldDefinition CreateRvaR4Field(ModuleDefinition module, float value)
        {
            var fac = module.CorLibTypeFactory;
            var field = new FieldDefinition(
                "_R4_" + Guid.NewGuid().ToString("N"),
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(fac.Single));
            field.FieldRva = new DataSegment(BitConverter.GetBytes(value));
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        private static FieldDefinition CreateRvaR8Field(ModuleDefinition module, double value)
        {
            var fac = module.CorLibTypeFactory;
            var field = new FieldDefinition(
                "_R8_" + Guid.NewGuid().ToString("N"),
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(fac.Double));
            field.FieldRva = new DataSegment(BitConverter.GetBytes(value));
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        // char[] (UTF-16) → blob RVA + __StaticArrayInitTypeSize
        private static FieldDefinition CreateRvaStringBlob(ModuleDefinition module, string content, Dictionary<int, TypeDefinition> sizedStructCache)
        {
            var bytes = System.Text.Encoding.Unicode.GetBytes(content ?? string.Empty);
            if (bytes.Length == 0) throw new InvalidOperationException("String vacío no debería llegar aquí.");

            var blobType = GetOrCreateStaticArrayType(module, bytes.Length, sizedStructCache);
            var field = new FieldDefinition(
                "_STR_" + Guid.NewGuid().ToString("N"),
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(blobType.ToTypeSignature()));
            field.FieldRva = new DataSegment(bytes);
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        // __StaticArrayInitTypeSize=N (value-type, explicit layout)
        private static TypeDefinition GetOrCreateStaticArrayType(ModuleDefinition module, int size, Dictionary<int, TypeDefinition> cache)
        {
            if (cache.TryGetValue(size, out var t)) return t;

            var name = "__StaticArrayInitTypeSize=" + size;
            var pid = GetOrCreatePID(module);

            t = pid.NestedTypes.FirstOrDefault(x => x.Name == name);
            if (t != null) { cache[size] = t; return t; }

            // System.ValueType del mismo corlib:
            var sysValueTypeRef = new TypeReference(
                module,
                module.CorLibTypeFactory.CorLibScope,
                "System",
                "ValueType");

            t = new TypeDefinition(
                string.Empty,
                name,
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.ExplicitLayout | TypeAttributes.BeforeFieldInit,
                sysValueTypeRef);
            t.ClassLayout = new ClassLayout(1, (uint)size);

            pid.NestedTypes.Add(t);
            cache[size] = t;
            return t;
        }

        private static TypeDefinition GetStaticArrayType(ModuleDefinition module, int size)
        {
            var pid = GetOrCreatePID(module);
            var existing = pid.NestedTypes.FirstOrDefault(x => x.Name == "__StaticArrayInitTypeSize=" + size);
            return existing ?? GetOrCreateStaticArrayType(module, size, new Dictionary<int, TypeDefinition>());
        }

        private static TypeDefinition GetOrCreatePID(ModuleDefinition module)
        {
            var pid = module.TopLevelTypes.FirstOrDefault(t => t.Name == "<PrivateImplementationDetails>");
            if (pid != null)
                return pid;

            pid = new TypeDefinition(
                string.Empty,
                "<PrivateImplementationDetails>",
                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                module.CorLibTypeFactory.Object.Type);

            module.TopLevelTypes.Add(pid);
            return pid;
        }

        private static TypeDefinition BuildRuntimeHelper(
            ModuleDefinition module,
            ReferenceImporter importer,
            IMethodDescriptor initArray,
            IReadOnlyList<FieldDefinition> keyShards)
        {
            // internal sealed class <HydraRvaRuntime>
            var rt = new TypeDefinition(
                string.Empty,
                "<HydraRvaRuntime>",
                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                module.CorLibTypeFactory.Object.Type);
            module.TopLevelTypes.Add(rt);

            rt.Methods.Add(BuildDecStr(module, importer, initArray, keyShards));
            rt.Methods.Add(BuildDecNum(module, importer, initArray, keyShards, "DecI4", module.CorLibTypeFactory.Int32,
                typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt32), new[] { typeof(byte[]), typeof(int) })));
            rt.Methods.Add(BuildDecNum(module, importer, initArray, keyShards, "DecI8", module.CorLibTypeFactory.Int64,
                typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt64), new[] { typeof(byte[]), typeof(int) })));
            rt.Methods.Add(BuildDecNum(module, importer, initArray, keyShards, "DecR4", module.CorLibTypeFactory.Single,
                typeof(BitConverter).GetMethod(nameof(BitConverter.ToSingle), new[] { typeof(byte[]), typeof(int) })));
            rt.Methods.Add(BuildDecNum(module, importer, initArray, keyShards, "DecR8", module.CorLibTypeFactory.Double,
                typeof(BitConverter).GetMethod(nameof(BitConverter.ToDouble), new[] { typeof(byte[]), typeof(int) })));

            return rt;
        }

        private static MethodDefinition BuildDecStr(
            ModuleDefinition m,
            ReferenceImporter imp,
            IMethodDescriptor initArray,
            IReadOnlyList<FieldDefinition> shards)
        {
            var byteTypeRef = imp.ImportType(typeof(byte));
            var byteArraySig = new SzArrayTypeSignature(m.CorLibTypeFactory.Byte);

            var sha256TypeRef = imp.ImportType(typeof(SHA256));
            var shaCreate = imp.ImportMethod(typeof(SHA256).GetMethod(nameof(SHA256.Create), Type.EmptyTypes));
            var shaCompute = imp.ImportMethod(typeof(SHA256).GetMethod(nameof(SHA256.ComputeHash), new[] { typeof(byte[]) }));

            var hmacCtor = imp.ImportMethod(typeof(HMACSHA256).GetConstructor(new[] { typeof(byte[]) }));
            var hmacCompute = imp.ImportMethod(typeof(HMACSHA256).GetMethod(nameof(HMACSHA256.ComputeHash), new[] { typeof(byte[]) }));

            var blockCopy = imp.ImportMethod(typeof(Buffer).GetMethod(nameof(Buffer.BlockCopy),
                                 new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) }));

            var aesManagedCtor = imp.ImportMethod(typeof(AesManaged).GetConstructor(Type.EmptyTypes));
            var symmCreateDec = imp.ImportMethod(typeof(SymmetricAlgorithm).GetMethod(nameof(SymmetricAlgorithm.CreateDecryptor), new[] { typeof(byte[]), typeof(byte[]) }));
            var icTransform = imp.ImportType(typeof(ICryptoTransform));
            var tfb = imp.ImportMethod(typeof(ICryptoTransform).GetMethod(nameof(ICryptoTransform.TransformFinalBlock), new[] { typeof(byte[]), typeof(int), typeof(int) }));

            var encGetUnicode = imp.ImportMethod(typeof(System.Text.Encoding).GetProperty(nameof(System.Text.Encoding.Unicode)).GetGetMethod());
            var encGetString = imp.ImportMethod(typeof(System.Text.Encoding).GetMethod(nameof(System.Text.Encoding.GetString), new[] { typeof(byte[]) }));

            var md = new MethodDefinition("DecStr",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(m.CorLibTypeFactory.String, byteArraySig, byteArraySig, byteArraySig)); // (cipher, iv, tag)

            var body = new CilMethodBody(md);
            md.CilMethodBody = body;
            var il = body.Instructions;

            // locals
            var sLocal = new CilLocalVariable(byteArraySig);
            var tmpLocal = new CilLocalVariable(byteArraySig);
            var shaLocal = new CilLocalVariable(sha256TypeRef.ToTypeSignature());
            var fullLocal = new CilLocalVariable(byteArraySig);
            var kLocal = new CilLocalVariable(byteArraySig);
            var hmacLocal = new CilLocalVariable(imp.ImportType(typeof(HMACSHA256)).ToTypeSignature());
            var full2 = new CilLocalVariable(byteArraySig);
            var kiLocal = new CilLocalVariable(byteArraySig);
            var trLocal = new CilLocalVariable(icTransform.ToTypeSignature());
            var plainLocal = new CilLocalVariable(byteArraySig);
            body.LocalVariables.AddRange(new[] { sLocal, tmpLocal, shaLocal, fullLocal, kLocal, hmacLocal, full2, kiLocal, trLocal, plainLocal });

            int totalLen = shards.Count * 16;

            // S = new byte[totalLen]
            il.Add(CilOpCodes.Ldc_I4, totalLen);
            il.Add(CilOpCodes.Newarr, byteTypeRef);
            il.Add(CilOpCodes.Stloc, sLocal);

            // Rellenar S con shards
            for (int i = 0; i < shards.Count; i++)
            {
                var shard = shards[i];
                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, byteTypeRef);
                il.Add(CilOpCodes.Dup);
                il.Add(CilOpCodes.Ldtoken, shard);
                il.Add(CilOpCodes.Call, initArray);
                il.Add(CilOpCodes.Stloc, tmpLocal);

                il.Add(CilOpCodes.Ldloc, tmpLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldloc, sLocal);
                il.Add(CilOpCodes.Ldc_I4, i * 16);
                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Call, blockCopy);
            }

            // sha = SHA256.Create(); full = sha.ComputeHash(S)
            il.Add(CilOpCodes.Call, shaCreate);
            il.Add(CilOpCodes.Stloc, shaLocal);
            il.Add(CilOpCodes.Ldloc, shaLocal);
            il.Add(CilOpCodes.Ldloc, sLocal);
            il.Add(CilOpCodes.Callvirt, shaCompute);
            il.Add(CilOpCodes.Stloc, fullLocal);

            // K = new byte[16]; BlockCopy(full,0,K,0,16)
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Newarr, byteTypeRef);
            il.Add(CilOpCodes.Stloc, kLocal);

            il.Add(CilOpCodes.Ldloc, fullLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldloc, kLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Call, blockCopy);

            // h = new HMACSHA256(K); full2 = h.ComputeHash(tag)
            il.Add(CilOpCodes.Ldloc, kLocal);
            il.Add(CilOpCodes.Newobj, hmacCtor);
            il.Add(CilOpCodes.Stloc, hmacLocal);

            il.Add(CilOpCodes.Ldloc, hmacLocal);
            il.Add(CilOpCodes.Ldarg_2); // tag
            il.Add(CilOpCodes.Callvirt, hmacCompute);
            il.Add(CilOpCodes.Stloc, full2);

            // Ki = new byte[16]; BlockCopy(full2,0,Ki,0,16)
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Newarr, byteTypeRef);
            il.Add(CilOpCodes.Stloc, kiLocal);

            il.Add(CilOpCodes.Ldloc, full2);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldloc, kiLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Call, blockCopy);

            // tr = (new AesManaged()).CreateDecryptor(Ki, iv)
            il.Add(CilOpCodes.Newobj, aesManagedCtor);
            il.Add(CilOpCodes.Ldloc, kiLocal);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Callvirt, symmCreateDec);
            il.Add(CilOpCodes.Stloc, trLocal);

            // plain = tr.TransformFinalBlock(cipher, 0, cipher.Length)
            il.Add(CilOpCodes.Ldloc, trLocal);
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldlen);
            il.Add(CilOpCodes.Conv_I4);
            il.Add(CilOpCodes.Callvirt, tfb);
            il.Add(CilOpCodes.Stloc, plainLocal);

            // return Encoding.Unicode.GetString(plain)
            il.Add(CilOpCodes.Call, encGetUnicode);
            il.Add(CilOpCodes.Ldloc, plainLocal);
            il.Add(CilOpCodes.Callvirt, encGetString);
            il.Add(CilOpCodes.Ret);

            return md;
        }

        private static MethodDefinition BuildDecNum(
            ModuleDefinition m,
            ReferenceImporter imp,
            IMethodDescriptor initArray,
            IReadOnlyList<FieldDefinition> shards,
            string name,
            CorLibTypeSignature retSig,
            System.Reflection.MethodInfo bitConv)
        {
            var byteTypeRef = imp.ImportType(typeof(byte));
            var byteArraySig = new SzArrayTypeSignature(m.CorLibTypeFactory.Byte);

            var sha256TypeRef = imp.ImportType(typeof(SHA256));
            var shaCreate = imp.ImportMethod(typeof(SHA256).GetMethod(nameof(SHA256.Create), Type.EmptyTypes));
            var shaCompute = imp.ImportMethod(typeof(SHA256).GetMethod(nameof(SHA256.ComputeHash), new[] { typeof(byte[]) }));

            var hmacCtor = imp.ImportMethod(typeof(HMACSHA256).GetConstructor(new[] { typeof(byte[]) }));
            var hmacCompute = imp.ImportMethod(typeof(HMACSHA256).GetMethod(nameof(HMACSHA256.ComputeHash), new[] { typeof(byte[]) }));

            var blockCopy = imp.ImportMethod(typeof(Buffer).GetMethod(nameof(Buffer.BlockCopy),
                                 new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) }));

            var aesManagedCtor = imp.ImportMethod(typeof(AesManaged).GetConstructor(Type.EmptyTypes));
            var symmCreateDec = imp.ImportMethod(typeof(SymmetricAlgorithm).GetMethod(nameof(SymmetricAlgorithm.CreateDecryptor), new[] { typeof(byte[]), typeof(byte[]) }));
            var icTransform = imp.ImportType(typeof(ICryptoTransform));
            var tfb = imp.ImportMethod(typeof(ICryptoTransform).GetMethod(nameof(ICryptoTransform.TransformFinalBlock), new[] { typeof(byte[]), typeof(int), typeof(int) }));

            var toX = imp.ImportMethod(bitConv);

            var md = new MethodDefinition(name,
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(retSig, byteArraySig, byteArraySig, byteArraySig)); // (cipher, iv, tag)

            var body = new CilMethodBody(md);
            md.CilMethodBody = body;
            var il = body.Instructions;

            // locals
            var sLocal = new CilLocalVariable(byteArraySig);
            var tmpLocal = new CilLocalVariable(byteArraySig);
            var shaLocal = new CilLocalVariable(sha256TypeRef.ToTypeSignature());
            var fullLocal = new CilLocalVariable(byteArraySig);
            var kLocal = new CilLocalVariable(byteArraySig);
            var hmacLocal = new CilLocalVariable(imp.ImportType(typeof(HMACSHA256)).ToTypeSignature());
            var full2 = new CilLocalVariable(byteArraySig);
            var kiLocal = new CilLocalVariable(byteArraySig);
            var trLocal = new CilLocalVariable(icTransform.ToTypeSignature());
            var plainLocal = new CilLocalVariable(byteArraySig);
            body.LocalVariables.AddRange(new[] { sLocal, tmpLocal, shaLocal, fullLocal, kLocal, hmacLocal, full2, kiLocal, trLocal, plainLocal });

            int totalLen = shards.Count * 16;

            // S = new byte[totalLen]
            il.Add(CilOpCodes.Ldc_I4, totalLen);
            il.Add(CilOpCodes.Newarr, byteTypeRef);
            il.Add(CilOpCodes.Stloc, sLocal);

            // Rellenar S con shards
            for (int i = 0; i < shards.Count; i++)
            {
                var shard = shards[i];
                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Newarr, byteTypeRef);
                il.Add(CilOpCodes.Dup);
                il.Add(CilOpCodes.Ldtoken, shard);
                il.Add(CilOpCodes.Call, initArray);
                il.Add(CilOpCodes.Stloc, tmpLocal);

                il.Add(CilOpCodes.Ldloc, tmpLocal);
                il.Add(CilOpCodes.Ldc_I4, 0);
                il.Add(CilOpCodes.Ldloc, sLocal);
                il.Add(CilOpCodes.Ldc_I4, i * 16);
                il.Add(CilOpCodes.Ldc_I4, 16);
                il.Add(CilOpCodes.Call, blockCopy);
            }

            // sha = SHA256.Create(); full = sha.ComputeHash(S)
            il.Add(CilOpCodes.Call, shaCreate);
            il.Add(CilOpCodes.Stloc, shaLocal);
            il.Add(CilOpCodes.Ldloc, shaLocal);
            il.Add(CilOpCodes.Ldloc, sLocal);
            il.Add(CilOpCodes.Callvirt, shaCompute);
            il.Add(CilOpCodes.Stloc, fullLocal);

            // K = new byte[16]; BlockCopy(full,0,K,0,16)
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Newarr, byteTypeRef);
            il.Add(CilOpCodes.Stloc, kLocal);

            il.Add(CilOpCodes.Ldloc, fullLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldloc, kLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Call, blockCopy);

            // h = new HMACSHA256(K); full2 = h.ComputeHash(tag)
            il.Add(CilOpCodes.Ldloc, kLocal);
            il.Add(CilOpCodes.Newobj, hmacCtor);
            il.Add(CilOpCodes.Stloc, hmacLocal);

            il.Add(CilOpCodes.Ldloc, hmacLocal);
            il.Add(CilOpCodes.Ldarg_2); // tag
            il.Add(CilOpCodes.Callvirt, hmacCompute);
            il.Add(CilOpCodes.Stloc, full2);

            // Ki = new byte[16]; BlockCopy(full2,0,Ki,0,16)
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Newarr, byteTypeRef);
            il.Add(CilOpCodes.Stloc, kiLocal);

            il.Add(CilOpCodes.Ldloc, full2);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldloc, kiLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldc_I4, 16);
            il.Add(CilOpCodes.Call, blockCopy);

            // tr = (new AesManaged()).CreateDecryptor(Ki, iv)
            il.Add(CilOpCodes.Newobj, aesManagedCtor);
            il.Add(CilOpCodes.Ldloc, kiLocal);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Callvirt, symmCreateDec);
            il.Add(CilOpCodes.Stloc, trLocal);

            // plain = tr.TransformFinalBlock(cipher, 0, cipher.Length)
            il.Add(CilOpCodes.Ldloc, trLocal);
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldlen);
            il.Add(CilOpCodes.Conv_I4);
            il.Add(CilOpCodes.Callvirt, tfb);
            il.Add(CilOpCodes.Stloc, plainLocal);

            // return BitConverter.ToX(plain, 0)
            il.Add(CilOpCodes.Ldloc, plainLocal);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Call, toX);
            il.Add(CilOpCodes.Ret);

            return md;
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

        private static MethodDefinition GetRuntimeMethod(TypeDefinition helper, string name, int paramCount = 2)
        {
            var md = helper?.Methods.FirstOrDefault(m =>
                m.Name == name &&
                m.Signature is MethodSignature sig &&
                sig.ParameterTypes.Count == paramCount);

            if (md == null)
                throw new InvalidOperationException($"No se encontró {name} en {helper?.Name}.");
            return md;
        }

        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            string TempRenamer = Path.Combine(Path.GetTempPath(), module.Name);
            try { module.Write(TempRenamer); } catch (Exception Ex) { this.Errors = Ex; }
            return Execute(TempRenamer);
        }
    }
}