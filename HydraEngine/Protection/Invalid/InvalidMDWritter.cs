using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using HydraEngine.Core;
using HydraEngine.Protection.CodeEncryption.Stuffs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Invalid
{
    public class InvalidMDWritter
    {
        internal class RawHeap : HeapBase
        {
            public RawHeap(string name, byte[] content)
            {
                Name = name;
                this.content = content;
            }
            public override string Name { get; }
            public override uint GetRawLength()
            {
                return (uint)content.Length;
            }
            protected override void WriteToImpl(DataWriter writer)
            {
                writer.WriteBytes(this.content);
            }
            private readonly byte[] content;
        }

        private static RandomGenerator random;
        private static void Randomize<T>(MDTable<T> table) where T : struct
        {
            random.Shuffle<T>(table);
        }

        public static void MDEndCreateTables(ModuleWriterBase writer, ModuleWriterEventArgs e)
        {
            random = new RandomGenerator(32);
            PESection pESection = new PESection(".????", 1073741888u);
            writer.AddSection(pESection);
            pESection.Add(new ByteArrayChunk(new byte[123]), 4u);
            pESection.Add(new ByteArrayChunk(new byte[10]), 4u);
            writer.Metadata.TablesHeap.ModuleTable.Add(new RawModuleRow(0, 0x7fff7fff, 0, 0, 0));
            writer.Metadata.TablesHeap.AssemblyTable.Add(new RawAssemblyRow(0, 0, 0, 0, 0, 0, 0, 0x7fff7fff, 0));
            int r = random.NextInt32(8, 16);
            for (int i = 0; i < r; i++)
                writer.Metadata.TablesHeap.ENCLogTable.Add(new RawENCLogRow(random.NextUInt32(), random.NextUInt32()));
            r = random.NextInt32(8, 16);
            for (int i = 0; i < r; i++)
                writer.Metadata.TablesHeap.ENCMapTable.Add(new RawENCMapRow(random.NextUInt32()));
            Randomize(writer.Metadata.TablesHeap.ManifestResourceTable);
            writer.TheOptions.MetadataOptions.TablesHeapOptions.ExtraData = random.NextUInt32();
            writer.TheOptions.MetadataOptions.TablesHeapOptions.UseENC = false;
            writer.TheOptions.MetadataOptions.MetadataHeaderOptions.VersionString += "\0\0\0\0";
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#GUID", Guid.NewGuid().ToByteArray()));
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#Strings", new byte[1]));
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#Blob", new byte[1]));
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#Schema", new byte[1]));
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#GUID", Guid.NewGuid().ToByteArray()));
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#<Module>", new byte[1]));
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#" + Randomizer.GenerateRandomString(4), new byte[1]));
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#<Module>", new byte[1]));
            //writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#<Module>", new byte[5]));
            //writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#" + Safe.GenerateRandomLetters(4), new byte[21]));
            //writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#<Module>", new byte[82]));
            string text = ".????";
            string s = null;
            for (int i = 0; i < 10; i++)
            {
                text += GetRandomString();
            }
            for (int j = 0; j < 10; j++)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                s = EncodeString(bytes, asciiCharset);
            }
            byte[] bytes2 = Encoding.ASCII.GetBytes(s);
            writer.TheOptions.MetadataOptions.CustomHeaps.Add(new RawHeap("#null", bytes2));
            pESection.Add(new ByteArrayChunk(bytes2), 4u);
            uint signature = (uint)(writer.Metadata.TablesHeap.TypeSpecTable.Rows + 1);
            writer.Metadata.TablesHeap.TypeSpecTable.Add(new RawTypeSpecRow(signature));
        }

        public static void MDOnAllTablesSorted(ModuleWriterBase writer)
        {
            writer.Metadata.TablesHeap.DeclSecurityTable.Add(new RawDeclSecurityRow(unchecked(0x7fff), 0xffff7fff, 0xffff7fff));
        }
    


        private static readonly System.Random R = new System.Random();
        public static string GetRandomString()
        {
            string randomFileName = System.IO.Path.GetRandomFileName();
            return randomFileName.Replace(".", "");
        }
        private static readonly char[] asciiCharset = (from ord in Enumerable.Range(32, 95)
                                                       select (char)ord).Except(new char[]
        {
                '.'
        }).ToArray<char>();
        public static string EncodeString(byte[] buff, char[] charset)
        {
            int current = buff[0];
            var ret = new StringBuilder();
            for (int i = 1; i < buff.Length; i++)
            {
                current = (current << 8) + buff[i];
                while (current >= charset.Length)
                {
                    ret.Append(charset[current % charset.Length]);
                    current /= charset.Length;
                }
            }
            if (current != 0)
                ret.Append(charset[current % charset.Length]);
            return ret.ToString();
        }


        public static void PESectionsCreated(ModuleWriterEventArgs e)
        {
            var sect1 = new PESection(Randomizer.GenerateRandomString(4) + Randomizer.GenerateRandomString(4), 0xE0000040);
            e.Writer.AddSection(sect1);
            sect1.Add(new ByteArrayChunk(new byte[10]), 4);
            sect1.Add(new ByteArrayChunk(new byte[10]), 4);
        }
    }

    internal class RandomGenerator
    {
        internal RandomGenerator()
        {
            byte[] seed = new byte[32];
            _RNG.GetBytes(seed);
            state = _SHA256((byte[])seed.Clone());
            seedLen = seed.Length;
            stateFilled = 32;
            mixIndex = 0;
        }
        internal RandomGenerator(int length)
        {
            byte[] seed = new byte[(length == 0) ? 32 : length];
            _RNG.GetBytes(seed);
            state = _SHA256((byte[])seed.Clone());
            seedLen = seed.Length;
            stateFilled = 32;
            mixIndex = 0;
        }
        internal RandomGenerator(string seed)
        {
            byte[] ret = _SHA256((byte[])((!string.IsNullOrEmpty(seed)) ? Encoding.UTF8.GetBytes(seed) : Guid.NewGuid().ToByteArray()).Clone());
            for (int i = 0; i < 32; i++)
            {
                byte[] array = ret;
                int num = i;
                array[num] *= primes[i % primes.Length];
                ret = _SHA256(ret);
            }
            state = ret;
            seedLen = ret.Length;
            stateFilled = 32;
            mixIndex = 0;
        }
        internal RandomGenerator(byte[] seed)
        {
            state = (byte[])seed.Clone();
            seedLen = seed.Length;
            stateFilled = 32;
            mixIndex = 0;
        }
        public static byte[] _SHA256(byte[] buffer)
        {
            SHA256Managed sha = new SHA256Managed();
            return sha.ComputeHash(buffer);
        }
        private void NextState()
        {
            for (int i = 0; i < 32; i++)
            {
                byte[] array = state;
                int num = i;
                array[num] ^= primes[mixIndex = (mixIndex + 1) % RandomGenerator.primes.Length];
            }
            state = sha256.ComputeHash(state);
            stateFilled = 32;
        }
        public void NextBytes(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (buffer.Length - offset < length)
            {
                throw new ArgumentException("Invalid offset or length.");
            }
            while (length > 0)
            {
                if (length >= stateFilled)
                {
                    Buffer.BlockCopy(state, 32 - stateFilled, buffer, offset, stateFilled);
                    offset += stateFilled;
                    length -= stateFilled;
                    stateFilled = 0;
                }
                else
                {
                    Buffer.BlockCopy(state, 32 - stateFilled, buffer, offset, length);
                    stateFilled -= length;
                    length = 0;
                }
                if (stateFilled == 0)
                {
                    NextState();
                }
            }
        }
        public byte NextByte()
        {
            byte ret = state[32 - stateFilled];
            stateFilled--;
            if (stateFilled == 0)
            {
                NextState();
            }
            return ret;
        }
        public string NextString(int length)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < length; i++)
                {
                    char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(32m + NextInt32(122) - 32m)));
                    builder.Append(ch);
                }
                return builder.ToString();
            }
            catch
            {
            }
            return string.Empty;
        }
        public string NextHexString(int length, bool large = false)
        {
            if (length.ToString().Contains("5"))
            {
                throw new Exception("5 is an unacceptable number!");
            }
            try
            {
                string chars = "qwertyuıopğüasdfghjklşizxcvbnmöçQWERTYUIOPĞÜASDFGHJKLŞİZXCVBNMÖÇ0123456789/*-.:,;!'^+%&/()=?_~|\\}][{½$#£>";
                string rnd = new string((from s in Enumerable.Repeat<string>(chars, length / 2)
                                         select s[NextInt32(s.Length)]).ToArray<char>());
                if (!large)
                {
                    return BitConverter.ToString(Encoding.Default.GetBytes(rnd)).Replace("-", string.Empty).ToLower();
                }
                if (large)
                {
                    return BitConverter.ToString(Encoding.Default.GetBytes(rnd)).Replace("-", string.Empty);
                }
            }
            catch
            {
            }
            return string.Empty;
        }
        public string NextHexString(bool large = false)
        {
            return NextHexString(8, large);
        }
        public string NextString()
        {
            return NextString(seedLen);
        }
        public byte[] NextBytes(int length)
        {
            byte[] ret = new byte[length];
            NextBytes(ret, 0, length);
            return ret;
        }
        public byte[] NextBytes()
        {
            byte[] ret = new byte[seedLen];
            NextBytes(ret, 0, seedLen);
            return ret;
        }
        public int NextInt32()
        {
            return BitConverter.ToInt32(NextBytes(4), 0);
        }
        public int NextInt32(int max)
        {
            return (int)((ulong)NextUInt32() % (ulong)((long)max));
        }
        public int NextInt32(int min, int max)
        {
            if (max <= min)
            {
                return min;
            }
            return min + (int)((ulong)NextUInt32() % (ulong)((long)(max - min)));
        }
        public uint NextUInt32()
        {
            return BitConverter.ToUInt32(NextBytes(4), 0);
        }

        // Token: 0x06000593 RID: 1427 RVA: 0x0001CBBE File Offset: 0x0001ADBE
        public uint NextUInt32(uint max)
        {
            return NextUInt32() % max;
        }
        public double NextDouble()
        {
            return NextUInt32() / 4294967296.0;
        }
        public double NextDouble(double min, double max)
        {
            return NextDouble() * (max - min) + min;
        }
        public bool NextBoolean()
        {
            byte s = state[32 - stateFilled];
            stateFilled--;
            if (stateFilled == 0)
            {
                NextState();
            }
            return s % 2 == 0;
        }
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 1; i--)
            {
                int j = NextInt32(i + 1);
                T tmp = list[j];
                list[j] = list[i];
                list[i] = tmp;
            }
        }
        public void Shuffle<T>(MDTable<T> table) where T : struct
        {
            if (table.IsEmpty)
            {
                return;
            }
            for (uint i = (uint)table.Rows; i > 2U; i -= 1U)
            {
                uint j = NextUInt32(i - 1U) + 1U;
                T tmp = table[j];
                table[j] = table[i];
                table[i] = tmp;
            }
        }
        private static readonly byte[] primes = new byte[] { 7, 11, 23, 37, 43, 59, 71 };
        private static readonly RNGCryptoServiceProvider _RNG = new RNGCryptoServiceProvider();
        private readonly SHA256Managed sha256 = new SHA256Managed();
        private int mixIndex;
        private byte[] state;
        private int stateFilled;
        private readonly int seedLen;
    }
}
