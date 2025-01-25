using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using HydraEngine.Core;
using HydraEngine.Protection.CodeEncryption.Stuffs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MethodBody = dnlib.DotNet.Writer.MethodBody;

namespace HydraEngine.Protection.CodeEncryption
{
    public class AntiTamperNormal : Models.Protection
    {
        public AntiTamperNormal() : base("Protection.Method.CodeEncryption", "Renamer Phase", "Description for Renamer Phase") { this.CompatibleWithMap = false; }

        public override async Task<bool> Execute(ModuleDefMD Module)
        {
            try
            {
                AntiTamper(Module);
                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }


        static string GenerateRandomAsciiString(int length)
        {
            Random random = new Random();
            char[] randomAsciiChars = new char[length];

            for (int i = 0; i < length; i++)
            {
                randomAsciiChars[i] = (char)random.Next(32, 127);
            }

            return new string(randomAsciiChars);
        }
        string name = GenerateRandomAsciiString(1);
        static IKeyDeriver deriver;
        static List<MethodDef> methods;
        static uint name1;
        static uint name2;
        static RandomGenerator random;
        static uint[] c = new uint[] { 0 };
        static uint[] v = new uint[] { 0 };
        static uint[] x = new uint[] { 0 };
        static uint[] z = new uint[] { 0 };
        private static Random random2 = new Random();

        //private string RuntimeDLL = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CE.dll");
        public void AntiTamper(ModuleDefMD Module)
        {
            random = new Stuffs.RandomGenerator(name, Guid.NewGuid().ToString());
            z[0] = random.NextUInt32();
            x[0] = random.NextUInt32();
            c[0] = random.NextUInt32();
            v[0] = random.NextUInt32();
            name1 = random.NextUInt32() & 0x7f7f7f7f;
            name2 = random.NextUInt32() & 0x7f7f7f7f;
            deriver = new NormalDeriver();
            deriver.Init();
            methods = Module.GetTypes().SelectMany(sd => sd.Methods).ToList().Where(x => x.HasBody && x.DeclaringType != x.Module.GlobalType).ToList();


            var rt =  AssemblyDef.Load(HydraEngine.Properties.Resources.CodeEncryption); 
            TypeDef initType = rt.ManifestModule.Find("Runtime.AntiTamperNormal", false);
           
            IEnumerable<IDnlibDef> defs = Core.InjectHelper.Inject(initType, Module.GlobalType, Module);
            var initMethod = defs.OfType<MethodDef>().Single(method => method.Name == "Initialize");
            initMethod.Body.SimplifyMacros(initMethod.Parameters);
            List<Instruction> instrs = initMethod.Body.Instructions.ToList();
            for (int i = 0; i < instrs.Count; i++)
            {
                Instruction instr = instrs[i];
                if (instr.OpCode == OpCodes.Ldtoken)
                {
                    instr.Operand = Module.GlobalType;
                }
                else if (instr.OpCode == OpCodes.Call)
                {
                    var method = (IMethod)instr.Operand;
                    if (method.DeclaringType.Name == "Mutation" && method.Name == "Crypt")
                    {
                        Instruction ldDst = instrs[i - 2];
                        Instruction ldSrc = instrs[i - 1];
                        Debug.Assert(ldDst.OpCode == OpCodes.Ldloc && ldSrc.OpCode == OpCodes.Ldloc);
                        instrs.RemoveAt(i);
                        instrs.RemoveAt(i - 1);
                        instrs.RemoveAt(i - 2);
                        instrs.InsertRange(i - 2, deriver.EmitDerivation(initMethod, (Local)ldDst.Operand, (Local)ldSrc.Operand));
                    }
                }
            }
            initMethod.Body.Instructions.Clear();
            IMethod DebugAssert = Module.Import(typeof(Debug).GetMethod("Assert", new[] { typeof(bool) }));
            initMethod.Body.Instructions.Insert(0, new Instruction(OpCodes.Call, DebugAssert));
            initMethod.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldc_I4_1));
            foreach (Instruction instr in instrs)
            {
                initMethod.Body.Instructions.Add(instr);
            }
            var mutation = new MutationHelper("Mutation");
          
            mutation.InjectKeys<int>(initMethod, new[] { 1, 2, 3, 4, 5 }, new[] { (int)(name1 * name2), (int)z[0], (int)x[0], (int)c[0], (int)v[0] });
            var cctor = Module.GlobalType.FindOrCreateStaticConstructor();
            cctor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(initMethod));
            EnsureNoInlining(initMethod);
            EnsureNoInlining(cctor);
            foreach (var m in defs)
                m.Name = Randomizer.GenerateRandomString();
        }

        public  void EnsureNoInlining(MethodDef method)
        {
            method.ImplAttributes &= ~MethodImplAttributes.AggressiveInlining;
            method.ImplAttributes |= MethodImplAttributes.NoInlining;
        }

        public void CreateSections(ModuleWriterBase writer)
        {
            if (methods == null) return;
            var nameBuffer = new byte[8];
            nameBuffer[0] = (byte)(name1 >> 0);
            nameBuffer[1] = (byte)(name1 >> 8);
            nameBuffer[2] = (byte)(name1 >> 16);
            nameBuffer[3] = (byte)(name1 >> 24);
            nameBuffer[4] = (byte)(name2 >> 0);
            nameBuffer[5] = (byte)(name2 >> 8);
            nameBuffer[6] = (byte)(name2 >> 16);
            nameBuffer[7] = (byte)(name2 >> 24);
            var newSection = new PESection(Encoding.ASCII.GetString(nameBuffer), 0xE0000040);
            var newSection2 = new PESection(Encoding.ASCII.GetString(nameBuffer), 0xE0000040);//////////
            writer.Sections.Insert(0, newSection); // insert first to ensure proper RVA
            writer.Sections.Insert(1, newSection2); // insert first to ensure proper RVA//////
            uint alignment;

            alignment = writer.TextSection.Remove(writer.Metadata).Value;
            writer.TextSection.Add(writer.Metadata, alignment);

            alignment = writer.TextSection.Remove(writer.NetResources).Value;
            writer.TextSection.Add(writer.NetResources, alignment);

            alignment = writer.TextSection.Remove(writer.Constants).Value;
            newSection.Add(writer.Constants, alignment);
            newSection2.Add(writer.Constants, alignment);

            // move some PE parts to separate section to prevent it from being hashed
            var peSection = new PESection(name, 0x60000020);
            var peSection2 = new PESection(name, 0x60000020);////////////
            bool moved = false;
            if (writer.StrongNameSignature != null)
            {
                alignment = writer.TextSection.Remove(writer.StrongNameSignature).Value;
                peSection.Add(writer.StrongNameSignature, alignment);
                peSection2.Add(writer.StrongNameSignature, alignment);
                moved = true;
            }
            if (writer is ModuleWriter managedWriter)
            {
                if (managedWriter.ImportAddressTable != null)
                {
                    alignment = writer.TextSection.Remove(managedWriter.ImportAddressTable).Value;
                    peSection.Add(managedWriter.ImportAddressTable, alignment);
                    peSection2.Add(managedWriter.ImportAddressTable, alignment);
                    moved = true;
                }
                if (managedWriter.StartupStub != null)
                {
                    alignment = writer.TextSection.Remove(managedWriter.StartupStub).Value;
                    peSection.Add(managedWriter.StartupStub, alignment);
                    peSection2.Add(managedWriter.StartupStub, alignment);
                    moved = true;
                }
            }
            if (moved)
            {
                writer.Sections.AddBeforeReloc(peSection);
               // writer.Sections.AddBeforeRsrc(peSection);
            }
            else
            {
                writer.Sections.AddBeforeReloc(peSection);
               // writer.Sections.AddBeforeRsrc(peSection);
            }
            // move encrypted methods
            var encryptedChunk = new MethodBodyChunks(writer.TheOptions.ShareMethodBodies);
            newSection.Add(encryptedChunk, 4);
            newSection2.Add(encryptedChunk, 4);
      
            foreach (MethodDef method in methods)
            {
                if (!method.HasBody)
                    continue;
                MethodBody body = writer.Metadata.GetMethodBody(method);
                if (body != null) { _ = writer.MethodBodies.Remove(body); encryptedChunk.Add(body); }
               
            }
            // padding to prevent bad size due to shift division
            newSection.Add(new ByteArrayChunk(new byte[4]), 4);
            newSection2.Add(new ByteArrayChunk(new byte[2]), 2);
        }
        public void EncryptSection(ModuleWriterBase writer)
        {
            Stream stream = writer.DestinationStream;
            var reader = new BinaryReader(writer.DestinationStream);
            stream.Position = 0x3C;
            stream.Position = reader.ReadUInt32();
            stream.Position += 6;
            ushort sections = reader.ReadUInt16();
            stream.Position += 0xc;
            ushort optSize = reader.ReadUInt16();
            stream.Position += 2 + optSize;
            uint encLoc = 0, encSize = 0;
            int origSects = -1;
            if (writer is NativeModuleWriter && writer.Module is ModuleDefMD mD)
                origSects = mD.Metadata.PEImage.ImageSectionHeaders.Count;
            for (int i = 0; i < sections; i++)
            {
                uint nameHash;
                if (origSects > 0)
                {
                    origSects--;
                    stream.Write(new byte[8], 0, 8);
                    nameHash = 0;
                }
                else
                    nameHash = reader.ReadUInt32() * reader.ReadUInt32();
                stream.Position += 8;
                if (nameHash == name1 * name2)
                {
                    encSize = reader.ReadUInt32();
                    encLoc = reader.ReadUInt32();
                }
                else if (nameHash != 0)
                {
                    uint sectSize = reader.ReadUInt32();
                    uint sectLoc = reader.ReadUInt32();
                    Hash(stream, reader, sectLoc, sectSize);
                }
                else
                    stream.Position += 8;
                stream.Position += 16;
            }
            uint[] key = DeriveKey();
            encSize >>= 2;
            stream.Position = encLoc;
            var result = new uint[encSize];
            for (uint i = 0; i < encSize; i++)
            {
                uint data = reader.ReadUInt32();
                result[i] = data ^ key[i & 0xf];
                key[i & 0xf] = (key[i & 0xf] ^ data) + 0x3dbb2819;
            }
            var byteResult = new byte[encSize << 2];
            Buffer.BlockCopy(result, 0, byteResult, 0, byteResult.Length);
            stream.Position = encLoc;
            stream.Write(byteResult, 0, byteResult.Length);
        }
        static void Hash(Stream stream, BinaryReader reader, uint offset, uint size)
        {
            long original = stream.Position;
            stream.Position = offset;
            size >>= 2;
            for (uint i = 0; i < size; i++)
            {
                uint data = reader.ReadUInt32();
                uint tmp = (z[0] ^ data) + x[0] + c[0] * v[0];
                z[0] = x[0];
                x[0] = c[0];
                x[0] = v[0];
                v[0] = tmp;
            }
            stream.Position = original;
        }
        static uint[] DeriveKey()
        {
            uint[] dst = new uint[0x10], src = new uint[0x10];
            for (int i = 0; i < 0x10; i++)
            {
                dst[i] = v[0];
                src[i] = x[0];
                z[0] = (x[0] >> 5) | (x[0] << 27);
                x[0] = (c[0] >> 3) | (c[0] << 29);
                c[0] = (v[0] >> 7) | (v[0] << 25);
                v[0] = (z[0] >> 11) | (z[0] << 21);
            }
            return deriver?.DeriveKey(dst, src);
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
