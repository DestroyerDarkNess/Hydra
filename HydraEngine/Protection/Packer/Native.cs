using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.PE;
using HydraEngine.Core;
using HydraEngine.Protection.Packer.NetBuilderInjection;
using HydraEngine.Runtimes.Anti.Runtime;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HydraEngine.Protection.Packer
{
    public class Native : Models.Pack
    {
        public Native() : base("Protection.Pack.Native", "Renamer Phase", "Description for Renamer Phase") { UpdateResurces = true; }

        public string BaseChars = "日本書紀العالمحالعجلة林氏家族การดำน้ำดูปะการังसंस्कृतम्संस्कृतावाक्" +
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ" +
            "αβγδεζηθικλµνξοπρστυϕχψω" +
            "れづれなるまゝに日暮らし硯にむかひて心にうりゆくよな事を、こはかとなく書きつくればあやうこそものぐるほけれ。";
        public bool AsyncMain { get; set; } = true;

        public override async Task<bool> Execute(ModuleDefMD module, string Ouput)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }
        private string Protections = @"
void detectDebugger() {
    if (IsDebuggerPresent()) {
        exit(1);
    }
}
            ";

        private string DllLoader = @"
BOOL WINAPI DllMain(HINSTANCE dllHistance, DWORD callReason, void* reserved)
{
        switch (callReason)
        {
                case DLL_PROCESS_ATTACH:
                {
					bool AsyncMain = $AsyncThread$;

					if (AsyncMain == false) {
						main();
					}
					else {
						CreateThread(NULL, 0, &main, 0, 0, 0);
					}
                        break;
                }
                case DLL_PROCESS_DETACH:
                {
                        break;
                }
                default:
                {
                        break;
                }
        }
        return TRUE;
}
            ";

        private string Compiler = string.Empty;

        public override async Task<bool> Execute(string FilePath, string Ouput)
        {
            try
            {

                try { Compiler = NetBuilderInjection.Helpers.UnzipTCC_Compiler(Application.ExecutablePath); } catch { throw new Exception("Error Extracting Compiler, Please disable your antivirus."); }

                string PExtension = Path.GetExtension(FilePath).ToLower();
                string TempNameAssembly = Path.GetFileNameWithoutExtension(FilePath) + PExtension;
                string TempAssembly = System.IO.Path.Combine(Path.GetTempPath(), TempNameAssembly);
                string tccX64 = System.IO.Path.Combine(Path.GetDirectoryName(Compiler), "x86_64-win32-tcc.exe");

                System.IO.File.Copy(FilePath, TempAssembly, true);

                var module = ModuleDefMD.Load(TempAssembly);
                var peImage = module.Metadata.PEImage;
                var machineType = peImage.ImageNTHeaders.FileHeader.Machine;
                bool Isx64 = machineType == Machine.AMD64 || machineType == Machine.IA64;
                module.Dispose();

                if (Isx64) Compiler = tccX64;

                //bool PackPE = pack(TempAssembly);

                string TCC_Args = "";

                byte[] shellcodeBytes = TempAssembly.ToShellCode(assemblyMap.EntryPoint);

                if (shellcodeBytes == null) { throw new Exception("Shellcode generation failed!"); }
               
                StringBuilder cCodeBuilder = new StringBuilder();
                cCodeBuilder.AppendLine("#include <stdio.h>");
                cCodeBuilder.AppendLine("#include <stdlib.h>");
                cCodeBuilder.AppendLine("#include <windows.h>");
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine(Protections);
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine("int main() {");
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine("    unsigned char shellcode[] = {");

                for (int i = 0; i < shellcodeBytes.Length; i++)
                {
                    cCodeBuilder.AppendFormat("0x{0:X2}", shellcodeBytes[i]);
                    if (i < shellcodeBytes.Length - 1)
                    {
                        cCodeBuilder.Append(", ");
                    }
                    if ((i + 1) % 12 == 0)
                    {
                        cCodeBuilder.AppendLine();
                        cCodeBuilder.Append("    ");
                    }
                }

                cCodeBuilder.AppendLine(" };");
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine("    void* exec = VirtualAlloc(0, sizeof(shellcode), MEM_COMMIT, PAGE_EXECUTE_READWRITE);");
                cCodeBuilder.AppendLine("    memcpy(exec, shellcode, sizeof(shellcode));");
                cCodeBuilder.AppendLine("    detectDebugger(); ");
                cCodeBuilder.AppendLine("    ((void(*)())exec)();");
                cCodeBuilder.AppendLine("    return 0;");
                cCodeBuilder.AppendLine("}");
                cCodeBuilder.AppendLine();
                if (Ouput.ToLower().EndsWith(".dll")) cCodeBuilder.AppendLine(DllLoader).Replace("$AsyncThread$", AsyncMain.ToString().ToLower());
                cCodeBuilder.AppendLine();

                string cCode = cCodeBuilder.ToString();

                //    if (PExtension != ".exe") { TCC_Args = " -shared"; } else { StubStr = StubStr.Replace("Conditional = false", "Conditional = true"); }

                    string StubTempFile = Path.Combine(Path.GetTempPath(), "Temp");
                    if (File.Exists(StubTempFile) == true) { File.Delete(StubTempFile); }

                    File.WriteAllText(StubTempFile, cCode);
          
                string FullArguments = "\"" + StubTempFile + "\"" + TCC_Args + " -o " + "\"" + Ouput + "\"" + " -luser32 -lkernel32 -mwindows";
                if (Ouput.ToLower().EndsWith(".dll")) FullArguments += " -shared";

                string TccResult = Core.Utils.RunRemoteHost(Compiler, FullArguments);

                if (File.Exists(StubTempFile) == true) { File.Delete(StubTempFile); }

                if ( string.IsNullOrEmpty(TccResult) == false) { TccResult = "Successful compilation."; }

                    Console.WriteLine("Compiler Result: " + TccResult);

                return File.Exists(Ouput);
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        private bool pack(string Path)
        {
            try
            {
                using (var stream = File.Open(Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (var reader = new BinaryReader(stream))
                using (var writer = new BinaryWriter(stream))
                {
                    var numberOfRvaAndSizes = 0xF4;
                    stream.Position = numberOfRvaAndSizes;
                    writer.Write(0xD);
                    writer.Write(0x1);

                    var dotnetSize = 0x16C;
                    stream.Position = dotnetSize;
                    writer.Write(0);

                    var debugVirtualAddress = 0x128;
                    stream.Position = debugVirtualAddress;
                    writer.Write(0);

                    var debugSize = 0x12C;
                    stream.Position = debugSize;
                    writer.Write(0);

                    var importSize = 0x104;
                    stream.Position = importSize;
                    writer.Write(0);

                    stream.Position = 0x3C;
                    var peHeader = reader.ReadUInt32();
                    stream.Position = peHeader;

                    const int PEHeaderWithExtraByteHex = 0x00014550;
                    writer.Write(PEHeaderWithExtraByteHex);

                    stream.Position += 0x2;
                    var numberOfSections = reader.ReadUInt16();

                    stream.Position += 0x10;
                    var is64PEOptionsHeader = reader.ReadUInt16() == 0x20B;

                    stream.Position += is64PEOptionsHeader ? 0x38 : 0x28 + 0xA6;
                    var dotNetVirtualAddress = reader.ReadUInt32();

                    uint dotNetPointerRaw = 0;
                    stream.Position += 0xC;
                    for (int i = 0; i < numberOfSections; i++)
                    {
                        stream.Position += 0xC;
                        var virtualAddress = reader.ReadUInt32();
                        var sizeOfRawData = reader.ReadUInt32();
                        var pointerToRawData = reader.ReadUInt32();
                        stream.Position += 0x10;

                        if (dotNetVirtualAddress >= virtualAddress && dotNetVirtualAddress < virtualAddress + sizeOfRawData && dotNetPointerRaw == 0)
                        {
                            dotNetPointerRaw = dotNetVirtualAddress + pointerToRawData - virtualAddress;
                        }
                    }

                    stream.Position = dotNetPointerRaw;
                    writer.Write(0);
                    writer.Write(0);
                    stream.Position += 0x4;
                    writer.Write(0);
                }
                return true;
            }
            catch { return false; }

        }

    }
}
