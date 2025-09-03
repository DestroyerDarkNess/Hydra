using AsmResolver;
using AsmResolver.IO;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE;
using AsmResolver.PE.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.File.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;

namespace HydraEngine.Protection.Integer
{
    public class StrIntToRva : Models.Protection
    {
        public bool ObfuscateStrings { get; set; } = true;

        public StrIntToRva()
            : base("Protection.Renamer.StrIntToRva", "Renamer Phase",
                   "Integers & Strings → RVA indirection")
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

                // Importar referencias necesarias:
                var initArray = importer.ImportMethod(
                    typeof(System.Runtime.CompilerServices.RuntimeHelpers)
                        .GetMethod("InitializeArray", new[] { typeof(Array), typeof(RuntimeFieldHandle) })
                );
                var stringCtorCharArray = importer.ImportMethod(
                    typeof(string).GetConstructor(new[] { typeof(char[]) })
                );
                var stringEmptyField = importer.ImportField(
                    typeof(string).GetField("Empty")
                );

                // Caches
                var i4Cache = new Dictionary<int, FieldDefinition>();
                var i8Cache = new Dictionary<long, FieldDefinition>();
                var r4Cache = new Dictionary<int, FieldDefinition>(); // bits de float
                var r8Cache = new Dictionary<long, FieldDefinition>(); // bits de double
                var strCache = new Dictionary<string, (FieldDefinition field, int length)>();
                var sizedStructCache = new Dictionary<int, TypeDefinition>();

                // ========= PASO 1: Strings → RVA =========
                if (ObfuscateStrings)
                {
                    foreach (var type in module.GetAllTypes().ToArray())
                        foreach (var method in type.Methods.ToArray())
                        {
                            if (method is null || !method.HasMethodBody || method.CilMethodBody is null)
                                continue;

                            var instrs = method.CilMethodBody.Instructions;
                            if (instrs.Count == 0) continue;

                            for (int i = 0; i < instrs.Count; i++)
                            {
                                var ins = instrs[i];
                                if (ins.OpCode != CilOpCodes.Ldstr)
                                    continue;

                                var content = ins.Operand as string ?? string.Empty;

                                // ⚠️ Caso especial: string vacío => no metemos RVA de tamaño 0
                                if (content.Length == 0)
                                {
                                    ins.ReplaceWith(CilOpCodes.Ldsfld, stringEmptyField);
                                    continue;
                                }

                                if (!strCache.TryGetValue(content, out var info))
                                {
                                    var field = CreateRvaStringBlob(module, content, sizedStructCache);
                                    info = (field, content.Length);
                                    strCache[content] = info;
                                }

                                // Reemplazo:
                                // ldc.i4 <len>
                                // newarr char
                                // dup
                                // ldtoken <field>
                                // call RuntimeHelpers.InitializeArray(Array, RuntimeFieldHandle)
                                // newobj string(char[])
                                ins.ReplaceWith(CilOpCodes.Ldc_I4, info.length);
                                var charTypeRef = importer.ImportType(typeof(char));
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Newarr, charTypeRef));
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Dup));
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Ldtoken, info.field));
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Call, initArray));
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Newobj, stringCtorCharArray));
                            }
                        }
                }

                // ========= PASO 2: Numéricos → RVA =========
                foreach (var type in module.GetAllTypes().ToArray())
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
                                if (!i8Cache.TryGetValue(v, out var f))
                                    i8Cache[v] = f = CreateRvaI8Field(module, v);

                                ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Ldind_I8));
                                continue;
                            }

                            // Int32 (incluye -1..8, i4.s, etc.)
                            if (TryGetI4Constant(ins, out int i4))
                            {
                                if (!i4Cache.TryGetValue(i4, out var f))
                                    i4Cache[i4] = f = CreateRvaI4Field(module, i4);

                                ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Ldind_I4));
                                continue;
                            }

                            // Double
                            if (ins.OpCode == CilOpCodes.Ldc_R8)
                            {
                                double v = (double)ins.Operand;
                                long bits = BitConverter.DoubleToInt64Bits(v);
                                if (!r8Cache.TryGetValue(bits, out var f))
                                    r8Cache[bits] = f = CreateRvaR8Field(module, v);

                                ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Ldind_R8));
                                continue;
                            }

                            // Float
                            if (ins.OpCode == CilOpCodes.Ldc_R4)
                            {
                                float v = (float)ins.Operand;
                                int bits = BitConverter.ToInt32(BitConverter.GetBytes(v), 0);
                                if (!r4Cache.TryGetValue(bits, out var f))
                                    r4Cache[bits] = f = CreateRvaR4Field(module, v);

                                ins.ReplaceWith(CilOpCodes.Ldsflda, f);
                                instrs.Insert(++i, new CilInstruction(CilOpCodes.Ldind_R4));
                                continue;
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

        // ================= Helpers =================

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

        // ===== Campos RVA para números (en <PrivateImplementationDetails>) =====

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

        // ===== Strings: blob RVA + InitializeArray =====

        private static FieldDefinition CreateRvaStringBlob(
            ModuleDefinition module,
            string content,
            Dictionary<int, TypeDefinition> sizedStructCache)
        {
            // UTF-16 (LE) => 2 * length bytes. Nunca creamos blobs de tamaño 0.
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(content ?? string.Empty);
            int size = bytes.Length;
            if (size <= 0)
                throw new InvalidOperationException("CreateRvaStringBlob llamado con tamaño 0.");

            var blobType = GetOrCreateStaticArrayType(module, size, sizedStructCache);

            var field = new FieldDefinition(
                "_STR_" + Guid.NewGuid().ToString("N"),
                FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.HasFieldRva | FieldAttributes.InitOnly,
                new FieldSignature(blobType.ToTypeSignature()));

            field.FieldRva = new DataSegment(bytes);
            GetOrCreatePID(module).Fields.Add(field);
            return field;
        }

        private static TypeDefinition GetOrCreateStaticArrayType(
            ModuleDefinition module,
            int size,
            Dictionary<int, TypeDefinition> sizedStructCache)
        {
            if (sizedStructCache.TryGetValue(size, out var t))
                return t;

            var name = "__StaticArrayInitTypeSize=" + size;
            var pid = GetOrCreatePID(module);

            // ¿Existe ya?
            t = pid.NestedTypes.FirstOrDefault(x => x.Name == name);
            if (t != null)
            {
                sizedStructCache[size] = t;
                return t;
            }

            var valueTypeRef = GetSystemValueTypeRef(module);

            t = new TypeDefinition(
                string.Empty,
                name,
                TypeAttributes.NestedPrivate
                | TypeAttributes.Sealed
                | TypeAttributes.ExplicitLayout
                | TypeAttributes.BeforeFieldInit,
                valueTypeRef);

            // packing=1, size>0
            t.ClassLayout = new ClassLayout(1, (uint)size);

            pid.NestedTypes.Add(t);
            sizedStructCache[size] = t;
            return t;
        }

        private static ITypeDefOrRef GetSystemValueTypeRef(ModuleDefinition module)
        {
            // System.ValueType en el MISMO corlib que usa el módulo:
            return new TypeReference(
                module,                                   // módulo dueño
                module.CorLibTypeFactory.CorLibScope,     // scope del corlib
                "System",                                  // namespace
                "ValueType");                              // nombre
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

        public override Task<bool> Execute(dnlib.DotNet.ModuleDefMD module)
        {
            string temp = Path.Combine(Path.GetTempPath(), module.Name);
            try { module.Write(temp); } catch (Exception ex) { this.Errors = ex; }
            return Execute(temp);
        }
    }
}