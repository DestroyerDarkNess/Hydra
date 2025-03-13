
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;
using System;
using System.Collections.Generic;


//NativeModuleWriterOptions NavwriterOptions = new NativeModuleWriterOptions(module, true)
//{
//    Logger = DummyLogger.NoThrowInstance
//};

//NativeModuleWriter writer = new NativeModuleWriter(module, NavwriterOptions);
//NativeEraser.Erase(writer, module);

//string outputFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Ouput), System.IO.Path.GetFileNameWithoutExtension(Ouput) + "_native" + System.IO.Path.GetExtension(Ouput));
//writer.Write(outputFilePath);


namespace HydraEngine.Protection.Misc
{
    internal class NativeEraser
    {
        private static void Erase(Tuple<uint, uint, byte[]> section, uint offset, uint len)
        {
            Array.Clear(section.Item3, (int)(offset - section.Item1), (int)len);
        }

        private static void Erase(List<Tuple<uint, uint, byte[]>> sections, uint beginOffset, uint size)
        {
            foreach (Tuple<uint, uint, byte[]> sect in sections)
            {
                if (beginOffset >= sect.Item1 && beginOffset + size < sect.Item2)
                {
                    Erase(sect, beginOffset, size);
                    break;
                }
            }
        }

        private static void Erase(List<Tuple<uint, uint, byte[]>> sections, IFileSection s)
        {
            foreach (Tuple<uint, uint, byte[]> sect in sections)
            {
                if ((uint)s.StartOffset >= sect.Item1 && (uint)s.EndOffset < sect.Item2)
                {
                    Erase(sect, (uint)s.StartOffset, (uint)(s.EndOffset - s.StartOffset));
                    break;
                }
            }
        }

        private static void Erase(List<Tuple<uint, uint, byte[]>> sections, uint methodOffset)
        {
            foreach (Tuple<uint, uint, byte[]> sect in sections)
            {
                if (methodOffset >= sect.Item1)
                {
                    uint f = (uint)sect.Item3[(int)((UIntPtr)(methodOffset - sect.Item1))];
                    uint size;
                    switch (f & 7u)
                    {
                        case 2u:
                        case 6u:
                            size = (f >> 2) + 1u;
                            break;
                        case 3u:
                            {
                                f |= (uint)((uint)sect.Item3[(int)((UIntPtr)(methodOffset - sect.Item1 + 1u))] << 8);
                                size = (f >> 12) * 4u;
                                uint codeSize = BitConverter.ToUInt32(sect.Item3, (int)(methodOffset - sect.Item1 + 4u));
                                size += codeSize;
                                break;
                            }
                        case 4u:
                        case 5u:
                            goto IL_98;
                        default:
                            goto IL_98;
                    }
                    Erase(sect, methodOffset, size);
                    continue;
                IL_98:
                    break;
                }
            }
        }

        public static void Erase(NativeModuleWriter writer, ModuleDefMD module)
        {

            if (writer == null || module == null)
            {
                return;
            }
            List<Tuple<uint, uint, byte[]>> sections = new List<Tuple<uint, uint, byte[]>>();
            System.IO.MemoryStream s = new System.IO.MemoryStream();

            if (writer.OrigSections != null)
            {
                foreach (NativeModuleWriter.OrigSection origSect in writer.OrigSections)
                {
                    var oldChunk = origSect.Chunk;
                    ImageSectionHeader sectHdr = origSect.PESection;
                    s.SetLength(0L);
                    oldChunk.WriteTo(new DataWriter(s));
                    byte[] buf = s.ToArray();
                    var newChunk = new DataReaderChunk(ByteArrayDataReaderFactory.CreateReader(buf), oldChunk.GetVirtualSize());
                    newChunk.SetOffset(oldChunk.FileOffset, oldChunk.RVA);
                    origSect.Chunk = newChunk;
                    sections.Add(Tuple.Create<uint, uint, byte[]>(sectHdr.PointerToRawData, sectHdr.PointerToRawData + sectHdr.SizeOfRawData, buf));
                }
            }



            var md = module.Metadata;
            uint row = md.TablesStream.MethodTable.Rows;
            for (uint i = 1u; i <= row; i += 1u)
            {
                try
                {
                    if (md.TablesStream.MethodRowReader == null) continue;

                    RawMethodRow method;
                    md.TablesStream.MethodRowReader.TryReadRow(i, out method);

                    if ((method.ImplFlags & 3) == 0)
                    {
                        Erase(sections, (uint)md.PEImage.ToFileOffset((RVA)method.RVA));
                    }
                }
                catch { continue; }
            }
            ImageDataDirectory res = md.ImageCor20Header.Resources;
            if (res.Size > 0u)
            {
                Erase(sections, (uint)res.StartOffset, res.Size);
            }
            Erase(sections, md.ImageCor20Header);
            Erase(sections, md.MetadataHeader);// md.MetadataHeader);
            foreach (DotNetStream stream in md.AllStreams)
            {
                Erase(sections, stream);
            }
        }

    }

}
