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
        public NativeRC() : base("Protection.Pack.NativeV2", "Renamer Phase", "Description for Renamer Phase")
        {
            UpdateResurces = true; IsCompatibleWithDLL = true;
        }

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

        private string Headers = @"#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <windows.h>
#include <string.h>
#include <signal.h>

// Definir funciones que pueden no estar disponibles
#ifndef EXCEPTION_EXECUTE_HANDLER
#define EXCEPTION_EXECUTE_HANDLER       1
#endif

#ifndef EXCEPTION_CONTINUE_SEARCH
#define EXCEPTION_CONTINUE_SEARCH       0
#endif
";

        // Variable global para manejar crashes
        private string GlobalVars = @"

volatile int exception_occurred = 0;
volatile int shellcode_crashed = 0;

// Manejador de señales simplificado
void crash_handler(int sig) {
    printf(""[DEBUG] CRASH DETECTED: Signal %d received during shellcode execution!\n"", sig);
    shellcode_crashed = 1;
    exception_occurred = 1;
    // Salir del programa para evitar loops infinitos
    exit(1);
}";

        private string Protections = @"void detectDebugger() {
    printf(""[DEBUG] Checking for debugger...\n"");
    if (IsDebuggerPresent()) {
        printf(""[DEBUG] Debugger detected! Exiting...\n"");
        exit(1);
    }
    printf(""[DEBUG] No debugger detected.\n"");
}";

        private string ResourceLoader = @"

BOOL CALLBACK EnumNamesFunc(HMODULE hModule, LPCTSTR lpType, LPTSTR lpName, LONG_PTR lParam) {
    printf(""[DEBUG] Found resource name: %p\n"", lpName);
    return TRUE;
}

BOOL CALLBACK EnumTypesFunc(HMODULE hModule, LPTSTR lpType, LONG_PTR lParam) {
    printf(""[DEBUG] Found resource type: %p\n"", lpType);
    EnumResourceNames(hModule, lpType, EnumNamesFunc, 0);
    return TRUE;
}

void* loadEmbeddedResource(int resourceId, DWORD* size) {
    printf(""[DEBUG] Loading embedded resource with ID: %d\n"", resourceId);

    HRSRC hResource = NULL;
    HMODULE hModule = NULL;

    bool isDLL = $IsDLL$;

    printf(""[DEBUG] Module type: %s\n"", isDLL ? ""DLL"" : ""EXE"");

    if (isDLL) {
        // Para DLLs: obtener el handle de la DLL actual
        printf(""[DEBUG] Getting DLL module handle...\n"");

        // Método más confiable: obtener el handle usando la dirección de esta función
        if (!GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS,
                              (LPCTSTR)loadEmbeddedResource,
                              &hModule)) {
            printf(""[DEBUG] ERROR: GetModuleHandleEx failed. Error: %lu\n"", GetLastError());
            return NULL;
        }

        printf(""[DEBUG] DLL module handle obtained: 0x%p\n"", hModule);
        hResource = FindResource(hModule, MAKEINTRESOURCE(resourceId), RT_RCDATA);
    } else {
        // Para EXEs: usar NULL (módulo principal)
        printf(""[DEBUG] Using main module (EXE)...\n"");
        hModule = NULL;
        hResource = FindResource(NULL, MAKEINTRESOURCE(resourceId), RT_RCDATA);
    }

    if (hResource == NULL) {
        printf(""[DEBUG] CRITICAL ERROR: Resource not found with RT_RCDATA!\n"");
        printf(""[DEBUG] Last error: %lu\n"", GetLastError());

        // Debug: enumerar todos los recursos del módulo correcto
        printf(""[DEBUG] Enumerating all available resources in %s:\n"", isDLL ? ""DLL"" : ""EXE"");
        if (isDLL && hModule != NULL) {
            EnumResourceTypes(hModule, EnumTypesFunc, 0);
        } else {
            EnumResourceTypes(NULL, EnumTypesFunc, 0);
        }

        if (isDLL && hModule != NULL) {
            FreeLibrary(hModule);
        }
        return NULL;
    }

    printf(""[DEBUG] Resource found successfully!\n"");

    HGLOBAL hMemory = LoadResource(hModule, hResource);
    if (hMemory == NULL) {
        printf(""[DEBUG] ERROR: LoadResource failed. Error: %lu\n"", GetLastError());
        if (isDLL && hModule != NULL) {
            FreeLibrary(hModule);
        }
        return NULL;
    }

    *size = SizeofResource(hModule, hResource);
    printf(""[DEBUG] Resource size: %lu bytes\n"", *size);

    void* resourceData = LockResource(hMemory);
    if (resourceData == NULL) {
        printf(""[DEBUG] ERROR: LockResource failed.\n"");
        if (isDLL && hModule != NULL) {
            FreeLibrary(hModule);
        }
        return NULL;
    }

    printf(""[DEBUG] Resource loaded and locked successfully.\n"");

    return resourceData;
}

";

        private string ThreadMainFunction = @"

DWORD WINAPI ShellcodeExecutor(LPVOID lpParam) {
    printf(""[DEBUG] ShellcodeExecutor thread started.\n"");

    printf(""[DEBUG] About to call shellcode...\n"");
    fflush(stdout);

    // Ejecutar shellcode sin protección de timeout
    if (!shellcode_crashed && !exception_occurred) {
        void (*shellcode_func)() = (void(*)())lpParam;
        shellcode_func();
    }

    if (!shellcode_crashed && !exception_occurred) {
        printf(""[DEBUG] Shellcode returned normally.\n"");
    } else {
        printf(""[DEBUG] Shellcode execution was interrupted.\n"");
    }

    printf(""[DEBUG] ShellcodeExecutor completed.\n"");
    return (shellcode_crashed || exception_occurred) ? 1 : 0;
}

DWORD WINAPI ThreadMain(LPVOID lpParam) {
    printf(""[DEBUG] ThreadMain started.\n"");

    DWORD shellcodeSize;
    unsigned char* shellcode = (unsigned char*)loadEmbeddedResource(1, &shellcodeSize);
    if (shellcode == NULL) {
        printf(""[DEBUG] CRITICAL ERROR: Failed to load embedded resource.\n"");
        return 1;
    }

    printf(""[DEBUG] Shellcode loaded. Size: %lu bytes\n"", shellcodeSize);
    printf(""[DEBUG] First 16 bytes of shellcode: "");
    for (int i = 0; i < 16 && i < shellcodeSize; i++) {
        printf(""%02X "", shellcode[i]);
    }
    printf(""\n"");

    printf(""[DEBUG] Allocating executable memory...\n"");
    void* exec = VirtualAlloc(0, shellcodeSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
    if (exec == NULL) {
        printf(""[DEBUG] CRITICAL ERROR: VirtualAlloc failed. Error: %lu\n"", GetLastError());
        return 1;
    }
    printf(""[DEBUG] Memory allocated at address: 0x%p\n"", exec);

    printf(""[DEBUG] Copying shellcode to executable memory...\n"");
    memcpy(exec, shellcode, shellcodeSize);
    printf(""[DEBUG] Shellcode copied successfully.\n"");

    printf(""[DEBUG] Flushing instruction cache...\n"");
    if (!FlushInstructionCache(GetCurrentProcess(), exec, shellcodeSize)) {
        printf(""[DEBUG] WARNING: FlushInstructionCache failed. Error: %lu\n"", GetLastError());
    }

    detectDebugger();

    printf(""[DEBUG] About to execute shellcode at address: 0x%p\n"", exec);
    printf(""[DEBUG] First 16 bytes at exec address: "");
    for (int i = 0; i < 16 && i < shellcodeSize; i++) {
        printf(""%02X "", ((unsigned char*)exec)[i]);
    }
    printf(""\n"");

    printf(""[DEBUG] Executing shellcode with infinite timeout...\n"");
    fflush(stdout);

    // Ejecutar shellcode con timeout INFINITO
    HANDLE hThread = CreateThread(NULL, 0, ShellcodeExecutor, exec, 0, NULL);
    if (hThread == NULL) {
        printf(""[DEBUG] ERROR: Failed to create shellcode execution thread. Error: %lu\n"", GetLastError());
        VirtualFree(exec, 0, MEM_RELEASE);
        return 1;
    }

    // Esperar INFINITAMENTE hasta que el shellcode termine
    printf(""[DEBUG] Waiting for shellcode to complete (no timeout)...\n"");
    DWORD waitResult = WaitForSingleObject(hThread, INFINITE);
    DWORD threadExitCode = 0;
    GetExitCodeThread(hThread, &threadExitCode);

    switch(waitResult) {
        case WAIT_OBJECT_0:
            if (threadExitCode == 0 && !exception_occurred) {
                printf(""[DEBUG] Shellcode execution completed successfully.\n"");
            } else {
                printf(""[DEBUG] Shellcode execution completed with errors. Exit code: %lu\n"", threadExitCode);
            }
            break;

        case WAIT_FAILED:
            printf(""[DEBUG] WaitForSingleObject failed. Error: %lu\n"", GetLastError());
            break;

        default:
            printf(""[DEBUG] Unexpected wait result: %lu\n"", waitResult);
            break;
    }

    CloseHandle(hThread);

    printf(""[DEBUG] Cleaning up allocated memory...\n"");
    VirtualFree(exec, 0, MEM_RELEASE);

    printf(""[DEBUG] ThreadMain completed.\n"");
    return 0;
}

";

        private string MainFunction = @"int main() {
    printf(""[DEBUG] Main function started.\n"");
    return ThreadMain(NULL);
}";

        private string DllLoader = @"BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    switch (fdwReason) {
        case DLL_PROCESS_ATTACH:
        {
            // Allocate console for debugging
            //AllocConsole();
            freopen(""CONOUT$"", ""w"", stdout);
            freopen(""CONOUT$"", ""w"", stderr);
            freopen(""CONIN$"", ""r"", stdin);

            printf(""[DEBUG] DLL_PROCESS_ATTACH - DLL loaded successfully.\n"");
            printf(""[DEBUG] DLL Instance: 0x%p\n"", hinstDLL);

            DisableThreadLibraryCalls(hinstDLL);

            bool AsyncMain = $AsyncThread$;
            printf(""[DEBUG] AsyncMain setting: %s\n"", AsyncMain ? ""true"" : ""false"");

            if (AsyncMain == false) {
                printf(""[DEBUG] Executing ThreadMain synchronously...\n"");
                DWORD result = ThreadMain(NULL);
                printf(""[DEBUG] ThreadMain returned: %lu\n"", result);
            } else {
                printf(""[DEBUG] Creating thread for ThreadMain...\n"");
                HANDLE hThread = CreateThread(NULL, 0, ThreadMain, NULL, 0, NULL);
                if (hThread == NULL) {
                    printf(""[DEBUG] ERROR: CreateThread failed. Error: %lu\n"", GetLastError());
                } else {
                    printf(""[DEBUG] Thread created successfully. Handle: 0x%p\n"", hThread);
                    CloseHandle(hThread);
                }
            }
            break;
        }
        case DLL_PROCESS_DETACH:
        {
            printf(""[DEBUG] DLL_PROCESS_DETACH - DLL unloading.\n"");
            break;
        }
        case DLL_THREAD_ATTACH:
        {
            printf(""[DEBUG] DLL_THREAD_ATTACH\n"");
            break;
        }
        case DLL_THREAD_DETACH:
        {
            printf(""[DEBUG] DLL_THREAD_DETACH\n"");
            break;
        }
        default:
        {
            printf(""[DEBUG] Unknown fdwReason: %lu\n"", fdwReason);
            break;
        }
    }
    return TRUE;
}";

        private string GenerateStubCode(bool isDll, bool asyncMain)
        {
            var parts = new List<string>
    {
        Headers,
        "",
        GlobalVars,
        "",
        Protections,
        "",
        ResourceLoader.Replace("$IsDLL$", isDll.ToString().ToLower()),
        "",
        ThreadMainFunction,
        "",
        MainFunction
    };

            if (isDll)
            {
                parts.Add("");
                parts.Add(DllLoader.Replace("$AsyncThread$", asyncMain.ToString().ToLower()));
            }

            return string.Join("\n", parts);
        }

        private string Compiler = string.Empty;

        public override async Task<bool> Execute(string FilePath, string Output)
        {
            //try
            //{
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
            bool isDll = Output.ToLower().EndsWith(".dll"); // module.Kind == ModuleKind.Dll;
            module.Dispose();

            if (Isx64) Compiler = tccX64;

            //bool PackPE = pack(TempAssembly);

            //if (PackPE == false) { System.IO.File.Copy(FilePath, TempAssembly, true); }

            // Generate shellcode from the .NET assembly
            byte[] shellcodeBytes = TempAssembly.ToShellCode(assemblyMap.EntryPoint);

            if (shellcodeBytes == null) { throw new Exception("Shellcode generation failed!"); }

            // Build C code for the loader
            string cCode = GenerateStubCode(isDll, AsyncMain);

            string StubTempFile = Path.Combine(Path.GetTempPath(), "Temp.c");
            File.WriteAllText(StubTempFile, cCode);

            // Compile loader with embedded resources
            string tccArguments = $"\"{StubTempFile}\" -o \"{Output}\" -luser32 -lkernel32 -mwindows";
            if (isDll) tccArguments += " -shared";

            string tccResult = Core.Utils.RunRemoteHost(Compiler, tccArguments);
            Console.WriteLine("TCC Compilation failed: " + tccResult);
            if (File.Exists(StubTempFile) == true) { File.Delete(StubTempFile); }

            if (string.IsNullOrEmpty(tccResult) == false) { tccResult = "Successful compilation."; }
            Console.WriteLine("Compiler Result: " + tccResult);

            try
            {
                var portableExecutable = new PortableExecutable(Output);

                ResourceIdentifier RI;
                RI = new ResourceIdentifier(ResourceType.FromCode(10), ResourceName.FromCode(1));

                portableExecutable.SetResource(RI, shellcodeBytes);
                Console.WriteLine($"Resource embedded successfully in {(isDll ? "DLL" : "EXE")}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to embed resource: {ex.Message}");
            }

            return File.Exists(Output);
            //}
            //catch (Exception Ex)
            //{
            //    this.Errors = Ex;
            //    return false;
            //}
        }

        //Thank to https://github.com/0x59R11/BitDotNet
        private bool pack(string Path)
        {
            try
            {
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
            }
            catch { return false; }
        }
    }
}