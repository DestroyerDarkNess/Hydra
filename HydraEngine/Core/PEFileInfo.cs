using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Core
{
    public class PEFileInfo 
    {
        public int PeHeaderLocation;
        public int CoffStart;
        public ushort MachineType;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
        public int OptionalHeaderStart;
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint PatchLocation;
        public uint BaseOfCode;
        public uint BaseOfData;
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public long SizeOfImageLoc;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberofRvaAndSizes;
        public uint ExportTableRva;
        public uint ExportTableSize;
        public long ImportTableLocInPeOptHdrs;
        public uint ImportTableRva;
        public uint ImportTableSize;
        public ulong ResourceTable;
        public ulong ExceptionTable;
        public long CertTableLoc;
        public uint CertLoc;
        public uint CertSize;
       
        public static PEFileInfo GatherFileInfoWin(string binaryPath)
        {
           
                if (!File.Exists(binaryPath)) throw new FileNotFoundException();


                var flItms = new PEFileInfo();
               


                using (var binary = new BinaryReader(File.Open(binaryPath, FileMode.Open, FileAccess.Read)))
                {
                    binary.BaseStream.Seek(0x3C, SeekOrigin.Begin);
                    flItms.PeHeaderLocation = binary.ReadInt32();

                    flItms.CoffStart = flItms.PeHeaderLocation + 4;
                    binary.BaseStream.Seek(flItms.CoffStart, SeekOrigin.Begin);
                    flItms.MachineType = binary.ReadUInt16();
                    flItms.NumberOfSections = binary.ReadUInt16();
                    flItms.TimeDateStamp = binary.ReadUInt32();
                    binary.BaseStream.Seek(flItms.CoffStart + 16, SeekOrigin.Begin);
                    flItms.SizeOfOptionalHeader = binary.ReadUInt16();
                    flItms.Characteristics = binary.ReadUInt16();
                    flItms.OptionalHeaderStart = flItms.CoffStart + 20;

                    binary.BaseStream.Seek(flItms.OptionalHeaderStart, SeekOrigin.Begin);
                    flItms.Magic = binary.ReadUInt16();
                    flItms.MajorLinkerVersion = binary.ReadByte();
                    flItms.MinorLinkerVersion = binary.ReadByte();
                    flItms.SizeOfCode = binary.ReadUInt32();
                    flItms.SizeOfInitializedData = binary.ReadUInt32();
                    flItms.SizeOfUninitializedData = binary.ReadUInt32();
                    flItms.AddressOfEntryPoint = binary.ReadUInt32();
                    flItms.PatchLocation = flItms.AddressOfEntryPoint;
                    flItms.BaseOfCode = binary.ReadUInt32();
                    if (flItms.Magic != 0x20B)
                    {
                        flItms.BaseOfData = binary.ReadUInt32();
                    }

                    if (flItms.Magic == 0x20B)
                    {
                        flItms.ImageBase = binary.ReadUInt64();
                    }
                    else
                    {
                        flItms.ImageBase = binary.ReadUInt32();
                    }
                    flItms.SectionAlignment = binary.ReadUInt32();
                    flItms.FileAlignment = binary.ReadUInt32();
                    flItms.MajorOperatingSystemVersion = binary.ReadUInt16();
                    flItms.MinorOperatingSystemVersion = binary.ReadUInt16();
                    flItms.MajorImageVersion = binary.ReadUInt16();
                    flItms.MinorImageVersion = binary.ReadUInt16();
                    flItms.MajorSubsystemVersion = binary.ReadUInt16();
                    flItms.MinorSubsystemVersion = binary.ReadUInt16();
                    flItms.Win32VersionValue = binary.ReadUInt32();
                    flItms.SizeOfImageLoc = binary.BaseStream.Position;
                    flItms.SizeOfImage = binary.ReadUInt32();
                    flItms.SizeOfHeaders = binary.ReadUInt32();
                    flItms.CheckSum = binary.ReadUInt32();
                    flItms.Subsystem = binary.ReadUInt16();
                    flItms.DllCharacteristics = binary.ReadUInt16();

                    if (flItms.Magic == 0x20B)
                    {
                        flItms.SizeOfStackReserve = binary.ReadUInt64();
                        flItms.SizeOfStackCommit = binary.ReadUInt64();
                        flItms.SizeOfHeapReserve = binary.ReadUInt64();
                        flItms.SizeOfHeapCommit = binary.ReadUInt64();
                    }
                    else
                    {
                        flItms.SizeOfStackReserve = binary.ReadUInt32();
                        flItms.SizeOfStackCommit = binary.ReadUInt32();
                        flItms.SizeOfHeapReserve = binary.ReadUInt32();
                        flItms.SizeOfHeapCommit = binary.ReadUInt32();
                    }
                    flItms.LoaderFlags = binary.ReadUInt32();
                    flItms.NumberofRvaAndSizes = binary.ReadUInt32();

                    flItms.ExportTableRva = binary.ReadUInt32();
                    flItms.ExportTableSize = binary.ReadUInt32();
                    flItms.ImportTableLocInPeOptHdrs = binary.BaseStream.Position;
                    flItms.ImportTableRva = binary.ReadUInt32();
                    flItms.ImportTableSize = binary.ReadUInt32();
                    flItms.ResourceTable = binary.ReadUInt64();
                    flItms.ExceptionTable = binary.ReadUInt64();
                    flItms.CertTableLoc = binary.BaseStream.Position;
                    flItms.CertLoc = binary.ReadUInt32();
                    flItms.CertSize = binary.ReadUInt32();
                }
                return flItms;
        }

    }
}
