using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HydraEngine.Protection.Header
{
    public class SectionObfuscation
    {
        public string tag { get; set; } = "";
        public int Length { get; set; } = 8;
        public string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        public RenameMode Mode { get; set; } = RenameMode.Ascii;

        private Dictionary<string, string> Names = new Dictionary<string, string>();

        private Random Random = new Random();


        public enum RenameMode
        {
            Ascii,
            Key,
            Normal,
            Invisible
        }

        private static readonly char[] InvisibleChars = new char[]
   {
        '\u200B', // Zero Width Space
        '\u200C', // Zero Width Non-Joiner
        '\u200D', // Zero Width Joiner
        '\u2060', // Word Joiner
        '\uFEFF'  // Zero Width No-Break Space
   };

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private string GenerateInvisibleString(int length)
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = InvisibleChars[Random.Next(InvisibleChars.Length)];
            }
            return new string(result);
        }

        private string RandomString(int length, string chars)
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }


        public static readonly char[] ProblematicChars = new char[]
  {
        '\u202A', // LEFT-TO-RIGHT EMBEDDING
        '\u202B', // RIGHT-TO-LEFT EMBEDDING
        '\u202C', // POP DIRECTIONAL FORMATTING
        '\u202D', // LEFT-TO-RIGHT OVERRIDE
        '\u202E'  // RIGHT-TO-LEFT OVERRIDE
  };

        private string GenerateProblematicString(int length)
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = ProblematicChars[Random.Next(ProblematicChars.Length)];
            }
            return new string(result);
        }

        private const int MaxChunkSize = 10000; // Tamaño de segmento razonable

        private string GenerateProblematicLongString(long length)
        {
            var stringBuilder = new StringBuilder((int)length);

            for (long i = 0; i < length; i += MaxChunkSize)
            {
                int chunkSize = (int)Math.Min(MaxChunkSize, length - i);
                char[] chunk = new char[chunkSize];

                for (int j = 0; j < chunkSize; j++)
                {
                    chunk[j] = ProblematicChars[Random.Next(ProblematicChars.Length)];
                }

                stringBuilder.Append(chunk);
            }

            return stringBuilder.ToString();
        }

        public string GenerateString(RenameMode mode)
        {
            switch (mode)
            {
                case RenameMode.Ascii:
                    return RandomString(Random.Next(4, Length), BaseChars);
                case RenameMode.Key:
                    return GenerateProblematicString(Random.Next(4, Length));
                case RenameMode.Normal:
                    return RandomString(Length);
                case RenameMode.Invisible:
                    return GenerateInvisibleString(Random.Next(4, Length));
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }


        //public static string[] blacklist = {
        //    "INIT",
        //    ".pdata",
        //    ".rdata",
        //    ".data",
        //    ".reloc",
        //    ".text"
        //};

        public  List<string> blacklist = new List<string>();

        private bool IsBlacklist(string arg)
        {
            return blacklist.Any(elem => arg.Contains(elem));
        }

        public void Protect(string filePath)
        {
            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    var begin = DateTime.Now;

                    Console.WriteLine("[!] Working file : " + filePath);
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        byte[] fileBytes = new byte[fs.Length];
                        fs.Read(fileBytes, 0, fileBytes.Length);

                        GCHandle handle = GCHandle.Alloc(fileBytes, GCHandleType.Pinned);
                        IntPtr ptr = handle.AddrOfPinnedObject();

                        IMAGE_DOS_HEADER dosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>(ptr);
                        if (dosHeader.e_magic != 0x5A4D) // IMAGE_DOS_SIGNATURE
                            return;

                        IntPtr ntHeadersOffset = IntPtr.Add(ptr, dosHeader.e_lfanew);
                        IntPtr fileHeaderOffset = IntPtr.Add(ntHeadersOffset, 4);
                        IMAGE_FILE_HEADER fileHeader = Marshal.PtrToStructure<IMAGE_FILE_HEADER>(fileHeaderOffset);

                        bool is64Bit = fileHeader.Machine == 0x8664; // IMAGE_FILE_MACHINE_AMD64
                        IntPtr optionalHeaderOffset = IntPtr.Add(fileHeaderOffset, Marshal.SizeOf<IMAGE_FILE_HEADER>());
                        IntPtr sectionHeaderOffset;

                        if (is64Bit)
                        {
                            sectionHeaderOffset = IntPtr.Add(optionalHeaderOffset, Marshal.SizeOf<IMAGE_OPTIONAL_HEADER64>());
                        }
                        else
                        {
                            sectionHeaderOffset = IntPtr.Add(optionalHeaderOffset, Marshal.SizeOf<IMAGE_OPTIONAL_HEADER32>());
                        }

                        IMAGE_SECTION_HEADER[] sectionHeaders = new IMAGE_SECTION_HEADER[fileHeader.NumberOfSections];

                        for (int i = 0; i < fileHeader.NumberOfSections; i++)
                        {
                            sectionHeaders[i] = Marshal.PtrToStructure<IMAGE_SECTION_HEADER>(sectionHeaderOffset);

                            string sectionName = new string(sectionHeaders[i].Name).TrimEnd('\0');

                            if (IsBlacklist(sectionName))
                            {
                                Console.WriteLine("[+] '{0}'\t : Section Text(Blacklist)", sectionName);
                            }
                            else
                            {
                                string randomName = RandomString(8);

                                if (string.IsNullOrWhiteSpace(tag))
                                {
                                    randomName = GenerateString(Mode);
                                } else { randomName = tag; }


                                Console.WriteLine("[+] '{0}'\t : Section Renamed with {1}", sectionName, randomName);

                                try
                                {
                                    byte[] randomNameBytes = System.Text.Encoding.ASCII.GetBytes(randomName);
                                    Marshal.Copy(randomNameBytes, 0, sectionHeaderOffset, randomNameBytes.Length);
                                }
                                catch
                                {
                                    Console.WriteLine("[+] '{0}'\t : Section Renamed Failed!", sectionName);
                                }
                            }

                            sectionHeaderOffset = IntPtr.Add(sectionHeaderOffset, Marshal.SizeOf<IMAGE_SECTION_HEADER>());
                        }

                        fs.Seek(0, SeekOrigin.Begin);
                        fs.Write(fileBytes, 0, fileBytes.Length);
                    }

                    var end = DateTime.Now;
                    Console.WriteLine("[-] Finished operation in {0}ms", (end - begin).TotalMilliseconds);
                    Thread.Sleep(3000);
                }
                else
                {
                    Console.WriteLine("[-] Failed to open specified file.");
                }
            }
            else
            {
                Console.WriteLine("[-] File is not specified, please use this program.exe any.exe/.dll");
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DOS_HEADER
        {
            public ushort e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res1;
            public ushort e_oemid;
            public ushort e_oeminfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;
            public int e_lfanew;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_OPTIONAL_HEADER32
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public uint BaseOfData;
            public uint ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public uint SizeOfStackReserve;
            public uint SizeOfStackCommit;
            public uint SizeOfHeapReserve;
            public uint SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_OPTIONAL_HEADER64
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
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
            public uint NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_SECTION_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public char[] Name;
            public uint PhysicalAddressOrVirtualSize;
            public uint VirtualAddress;
            public uint SizeOfRawData;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint PointerToLinenumbers;
            public ushort NumberOfRelocations;
            public ushort NumberOfLinenumbers;
            public uint Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DATA_DIRECTORY
        {
            public uint VirtualAddress;
            public uint Size;
        }
    }
}
