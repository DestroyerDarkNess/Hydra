using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using HydraEngine.Core;
using HydraEngine.Protection.Renamer.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;

namespace HydraEngine.Protection.JIT
{
    public class Protection
    {

        public string x86RT { get; set; } = "x86RTeX";
        public string x64RT { get; set; } = "x64RTeX";

        private readonly ModuleDefMD module;
        static List<EncryptedMethod> encryptedMethods;
        public static string x = "";
        public static string xx = "";
        public static string mname = "";
        public Protection(ModuleDefMD Module)
        {
            module = Module;
            encryptedMethods = new List<EncryptedMethod>();
            x64RT = string.Format("Hydra_{0}", Randomizer.GenerateRandomString());
            x86RT = string.Format("Hydra_{0}", Randomizer.GenerateRandomString());
        }
        public byte[] Protect()
        {
            AddToResources(module);
            InjectRuntime();
            SearchMethods();
            return EncryptModule();
        }
        private static newInjector inj = null;
        void InjectRuntime()
        {
            x = Randomizer.GenerateRandomString();
            xx = Randomizer.GenerateRandomString(5);
            var cctor = module.GlobalType.FindOrCreateStaticConstructor();
            inj = new newInjector(module, typeof(Runtime.JITRuntime));
            var initMethod = inj.FindMember("Initialize") as MethodDef;
            foreach (Instruction Instruction in initMethod.Body.Instructions.Where((Instruction I) => I.OpCode == OpCodes.Ldstr))
            {
                if (Instruction.Operand.ToString() == "Key")
                    Instruction.Operand = Convert.ToString(new Random().Next(1, 9));
            }
            var strdecode = inj.FindMember("decodeStr") as MethodDef;
            var converter = inj.FindMember("StreamToByteArray") as MethodDef;
            var DecompressBytes = inj.FindMember("DecompressBytes") as MethodDef;
            var HeaderLen = inj.FindMember("HeaderLen") as MethodDef;
            var SizeDecompressed = inj.FindMember("SizeDecompressed") as MethodDef;
            string nSapce = Randomizer.GenerateRandomString();
            MethodDef[] Utils = new MethodDef[]
           {
               converter,
               strdecode,
                DecompressBytes,
                HeaderLen,
                SizeDecompressed
           };
            cctor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(initMethod));
            var mutation = new MutationHelper();
            mutation.InjectKey<string>(initMethod, 12, toBase64(x));
            mutation.InjectKey<string>(initMethod, 13, x86RT);
            mutation.InjectKey<string>(initMethod, 14, x64RT);
            mutation.InjectKey<string>(initMethod, 15, toBase64("ISByte"));
            //Helpers.Mutations.MutationHelper.InjectString(initMethod, "j", toBase64(x));
            //Helpers.Mutations.MutationHelper.InjectString(initMethod, "n", oGlobals.x86RT);
            //Helpers.Mutations.MutationHelper.InjectString(initMethod, "o", oGlobals.x64RT);
            //Helpers.Mutations.MutationHelper.InjectString(initMethod, "u", toBase64("ISByte"));
            foreach (var m in Utils)
            {
                m.Name = string.Format("<{0}>Hydra_{1}", Randomizer.GenerateRandomString(), "Guard");
            }
            initMethod.Name = string.Format("<{0}>Hydra_{1}", Randomizer.GenerateRandomString(), "Guard");
        }
        static string toBase64(string str)
        {
            byte[] bytesToEncode = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(bytesToEncode);
        }
        void AddToResources(ModuleDefMD Module)
        {
            var x86 = HydraEngine.Properties.Resources.JLX86;
            var cX86 = HydraEngine._7zip.QuickLZ.CompressBytes(x86);
            var x64 = HydraEngine.Properties.Resources.JLX64;
            var cX64 = HydraEngine._7zip.QuickLZ.CompressBytes(x64);
            Module.Resources.Add(new EmbeddedResource(x86RT, cX86, ManifestResourceAttributes.Private));
            Module.Resources.Add(new EmbeddedResource(x64RT, cX64, ManifestResourceAttributes.Private));
        }
        void SearchMethods()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            foreach (TypeDef typeDef in module.Types.ToArray())
            {
                if (typeDef.IsGlobalModuleType)
                    continue;
                foreach (MethodDef methodDef in typeDef.Methods)
                {
                    if (methodDef.HasGenericParameters || (methodDef.HasReturnType && methodDef.ReturnType.IsGenericParameter)) continue;
                    bool isConstructor = methodDef.IsStaticConstructor;
                    if (!isConstructor)
                    {
                        bool hasBody = methodDef.HasBody;
                        if (hasBody && methodDef.Body.HasInstructions)
                        {
                            methodDef.ImplAttributes |= MethodImplAttributes.NoInlining;
                            var exceptionRef = module.CorLibTypes.GetTypeRef("System", "Exception");
                            var objectCtor = new MemberRefUser(module, ".ctor",
                            MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), exceptionRef);
                            methodDef.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Failed to load Runtime!"));
                            methodDef.Body.Instructions.Add(OpCodes.Newobj.ToInstruction(objectCtor));
                            methodDef.Body.Instructions.Add(OpCodes.Throw.ToInstruction());
                            encryptedMethods.Add(new EncryptedMethod
                            {
                                Method = methodDef,
                                OriginalBytes = module.GetOriginalRawILBytes(methodDef),
                                IsEncrypted = false
                            });
                        }
                    }
                }
            }
            writer.Write(encryptedMethods.Count);
            foreach (var method in encryptedMethods)
            {
                writer.Write(method.Method.MDToken.ToInt32());
                writer.Write(Convert.ToBase64String(method.OriginalBytes));
            }
            byte[] streamArr = stream.ToArray();
            byte[] compressedStream = HydraEngine._7zip.QuickLZ.CompressBytes(streamArr);
            var res = new EmbeddedResource(x, compressedStream, ManifestResourceAttributes.Private);
            module.Resources.Add(res);          
        }
        byte[] EncryptModule()
        {
            var writerOptions = new ModuleWriterOptions(module)
            {
                Logger = DummyLogger.NoThrowInstance,
            };
            writerOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;
            writerOptions.WriterEvent += OnWriterEvent;
            var result = new MemoryStream();
            module.Write(result, writerOptions);
            var bytes = result.ToArray();
            foreach (var method in encryptedMethods)
            {
                if (!method.IsEncrypted)
                {
                    var end = (int)method.FileOffset + method.CodeSize;
                    for (int i = (int)method.FileOffset; i < end; i++)
                    {
                        bytes[i] = 0x0;
                    }
                }
            }
            return bytes;
        }
        void OnWriterEvent(object sender, ModuleWriterEventArgs e)
        {
            if (e.Event == ModuleWriterEvent.EndWriteChunks)
            {
                foreach (var method in encryptedMethods)
                {
                    var methodBody = e.Writer.Metadata.GetMethodBody(method.Method);
                    var index = Utils.Locate(methodBody.Code, method.OriginalBytes);
                    method.FileOffset = (uint)(((uint)methodBody.FileOffset) + index);
                    method.CodeSize = method.OriginalBytes.Length;
                }
            }
        }
        public class EncryptedMethod
        {
            public MethodDef Method;
            public uint FileOffset;
            public int CodeSize;
            public byte[] OriginalBytes;
            public bool IsEncrypted;
        }
    }

    public class MutationHelper : IDisposable
    {
        string m_mtFullName;
        public string MutationType
        {
            get { return m_mtFullName; }
            set { m_mtFullName = value; }
        }
        public MutationHelper() : this(typeof(HydraEngine.Protection.JIT.Runtime.MutationClass).FullName) { }
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
