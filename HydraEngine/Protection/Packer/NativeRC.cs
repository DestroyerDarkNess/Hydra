using dnlib.DotNet;
using dnlib.PE;
using HydraEngine.Protection.Packer.NetBuilderInjection;
using Ressy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ResourceType = Ressy.ResourceType;

namespace HydraEngine.Protection.Packer
{
     public class NativeRC : Models.Pack
    {
        public NativeRC() : base("Protection.Pack.NativeV2", "Renamer Phase", "Description for Renamer Phase") { UpdateResurces = true; }

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

                //if (PackPE == false) { System.IO.File.Copy(FilePath, TempAssembly, true); }

                // Generate shellcode from the .NET assembly
                byte[] shellcodeBytes = TempAssembly.ToShellCode(assemblyMap.EntryPoint);

                if (shellcodeBytes == null) { throw new Exception("Shellcode generation failed!"); }

                // Build C code for the loader
                StringBuilder cCodeBuilder = new StringBuilder();
                cCodeBuilder.AppendLine("#include <stdio.h>");
                cCodeBuilder.AppendLine("#include <stdlib.h>");
                cCodeBuilder.AppendLine("#include <windows.h>");
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine(Protections);
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine("void* loadEmbeddedResource(int resourceId, DWORD* size) {");
                cCodeBuilder.AppendLine("    HRSRC hResource = FindResource(NULL, MAKEINTRESOURCE(resourceId), RT_RCDATA);");
                cCodeBuilder.AppendLine("    if (hResource == NULL) { return NULL; }");
                cCodeBuilder.AppendLine("    HGLOBAL hMemory = LoadResource(NULL, hResource);");
                cCodeBuilder.AppendLine("    if (hMemory == NULL) { return NULL; }");
                cCodeBuilder.AppendLine("    *size = SizeofResource(NULL, hResource);");
                cCodeBuilder.AppendLine("    return LockResource(hMemory);");
                cCodeBuilder.AppendLine("}");
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine("int main() {");
                cCodeBuilder.AppendLine("    DWORD shellcodeSize;");
                cCodeBuilder.AppendLine("    unsigned char* shellcode = (unsigned char*)loadEmbeddedResource(1, &shellcodeSize);");
                cCodeBuilder.AppendLine("    if (shellcode == NULL) {");
                cCodeBuilder.AppendLine("        fprintf(stderr, \"Failed to load embedded resource.\\n\");");
                cCodeBuilder.AppendLine("        return 1;");
                cCodeBuilder.AppendLine("    }");
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine("    void* exec = VirtualAlloc(0, shellcodeSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE);");
                cCodeBuilder.AppendLine("    if (exec == NULL) {");
                cCodeBuilder.AppendLine("        fprintf(stderr, \"VirtualAlloc failed.\\n\");");
                cCodeBuilder.AppendLine("        return 1;");
                cCodeBuilder.AppendLine("    }");
                cCodeBuilder.AppendLine();
                cCodeBuilder.AppendLine("    memcpy(exec, shellcode, shellcodeSize);");
                cCodeBuilder.AppendLine("    detectDebugger();");
                cCodeBuilder.AppendLine("    ((void(*)())exec)();");
                cCodeBuilder.AppendLine("    return 0;");
                cCodeBuilder.AppendLine("}");
                cCodeBuilder.AppendLine();
                if (Ouput.ToLower().EndsWith(".dll"))  cCodeBuilder.AppendLine(DllLoader).Replace("$AsyncThread$", AsyncMain.ToString().ToLower());
                cCodeBuilder.AppendLine();

                string cCode = cCodeBuilder.ToString();

                string StubTempFile = Path.Combine(Path.GetTempPath(), "Temp.c");
                File.WriteAllText(StubTempFile, cCode);

                // Compile loader with embedded resources
                string tccArguments = $"\"{StubTempFile}\" -o \"{Ouput}\" -luser32 -lkernel32 -mwindows";
                if (Ouput.ToLower().EndsWith(".dll")) tccArguments += " -shared";

                string tccResult = Core.Utils.RunRemoteHost(Compiler, tccArguments);
                if (File.Exists(StubTempFile) == true) { File.Delete(StubTempFile); }

                if (string.IsNullOrEmpty(tccResult) == false) { tccResult = "Successful compilation."; }
                Console.WriteLine("Compiler Result: " + tccResult);

                var portableExecutable = new PortableExecutable(Ouput);
                ResourceIdentifier RI = new ResourceIdentifier(ResourceType.FromCode(10), ResourceName.FromCode(1));
                portableExecutable.SetResource(RI, shellcodeBytes);

                return File.Exists(Ouput);
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        //Thank to https://github.com/0x59R11/BitDotNet
        private bool pack(string Path)
        {
            try {
                using (var stream = File.Open(Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (var reader = new BinaryReader(stream))
                using (var writer = new BinaryWriter(stream))
                {
                    //var numberOfRvaAndSizes = 0xF4;
                    //stream.Position = numberOfRvaAndSizes;
                    //writer.Write(0xD);
                    //writer.Write(0x1);

                    //var dotnetSize = 0x16C;
                    //stream.Position = dotnetSize;
                    //writer.Write(0);

                    var debugVirtualAddress = 0x128;
                    stream.Position = debugVirtualAddress;
                    writer.Write(0);

                    var debugSize = 0x12C;
                    stream.Position = debugSize;
                    writer.Write(0);

                    //var importSize = 0x104;
                    //stream.Position = importSize;
                    //writer.Write(0);

                    stream.Position = 0x3C;
                    var peHeader = reader.ReadUInt32();
                    stream.Position = peHeader;

                    //const int PEHeaderWithExtraByteHex = 0x00014550;
                    //writer.Write(PEHeaderWithExtraByteHex);

                    //stream.Position += 0x2;
                    //var numberOfSections = reader.ReadUInt16();

                    //stream.Position += 0x10;
                    //var is64PEOptionsHeader = reader.ReadUInt16() == 0x20B;

                    //stream.Position += is64PEOptionsHeader ? 0x38 : 0x28 + 0xA6;
                    //var dotNetVirtualAddress = reader.ReadUInt32();

                    //uint dotNetPointerRaw = 0;
                    //stream.Position += 0xC;
                    //for (int i = 0; i < numberOfSections; i++)
                    //{
                    //    stream.Position += 0xC;
                    //    var virtualAddress = reader.ReadUInt32();
                    //    var sizeOfRawData = reader.ReadUInt32();
                    //    var pointerToRawData = reader.ReadUInt32();
                    //    stream.Position += 0x10;

                    //    if (dotNetVirtualAddress >= virtualAddress && dotNetVirtualAddress < virtualAddress + sizeOfRawData && dotNetPointerRaw == 0)
                    //    {
                    //        dotNetPointerRaw = dotNetVirtualAddress + pointerToRawData - virtualAddress;
                    //    }
                    //}

                    //stream.Position = dotNetPointerRaw;
                    //writer.Write(0);
                    //writer.Write(0);
                    //stream.Position += 0x4;
                    //writer.Write(0);
                }
                return true;
            } catch { return false; }
         
        }

    }
}
