using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.AntiDLLInjection.Runtime
{
    internal static class LoadLibraryDetour
    {
        // Define the 32-bit function hook class (previously provided).
        public class FunctionHookX86 : IDisposable
        {
            public const int RequiredBytesCount = 5;
            private IntPtr targetFunctionAddress;
            private Protection originalProtection;
            private byte[] targetFunctionCode = new byte[RequiredBytesCount];
            private byte[] replacementFunctionCode = new byte[RequiredBytesCount];
            private Delegate destinationDelegate;

            public FunctionHookX86(IntPtr source, IntPtr destination)
            {
                VirtualProtect(source, RequiredBytesCount, Protection.PAGE_EXECUTE_READWRITE, out originalProtection);
                Marshal.Copy(source, targetFunctionCode, 0, RequiredBytesCount);
                replacementFunctionCode[0] = 0xE9;
                int offset = (int)destination - (int)source - 5;
                Array.Copy(BitConverter.GetBytes(offset), 0, replacementFunctionCode, 1, 4);
                targetFunctionAddress = source;
            }

            public FunctionHookX86(IntPtr source, Delegate destination) :
                this(source, Marshal.GetFunctionPointerForDelegate(destination))
            {
                destinationDelegate = destination;
            }

            public void Install()
            {
                Marshal.Copy(replacementFunctionCode, 0, targetFunctionAddress, RequiredBytesCount);
            }

            public void Uninstall()
            {
                Marshal.Copy(targetFunctionCode, 0, targetFunctionAddress, RequiredBytesCount);
            }

            public void Dispose()
            {
                Uninstall();
                destinationDelegate = null;
                VirtualProtect(targetFunctionAddress, RequiredBytesCount, originalProtection, out _);
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out Protection lpflOldProtect);

            public enum Protection
            {
                PAGE_NOACCESS = 0x01,
                PAGE_READONLY = 0x02,
                PAGE_READWRITE = 0x04,
                PAGE_WRITECOPY = 0x08,
                PAGE_EXECUTE = 0x10,
                PAGE_EXECUTE_READ = 0x20,
                PAGE_EXECUTE_READWRITE = 0x40,
                PAGE_EXECUTE_WRITECOPY = 0x80,
                PAGE_GUARD = 0x100,
                PAGE_NOCACHE = 0x200,
                PAGE_WRITECOMBINE = 0x400
            }
        }

        // Define the 64-bit function hook class (previously provided).
        public class FunctionHookX64 : IDisposable
        {
            public const int RequiredBytesCount = 12;
            private IntPtr targetFunctionAddress;
            private Protection originalProtection;
            private byte[] targetFunctionCode = new byte[RequiredBytesCount];
            private byte[] replacementFunctionCode = new byte[RequiredBytesCount];
            private Delegate destinationDelegate;

            public FunctionHookX64(IntPtr source, IntPtr destination)
            {
                VirtualProtect(source, RequiredBytesCount, Protection.PAGE_EXECUTE_READWRITE, out originalProtection);
                Marshal.Copy(source, targetFunctionCode, 0, RequiredBytesCount);
                replacementFunctionCode[0] = 0x48;
                replacementFunctionCode[1] = 0xB8;
                Array.Copy(BitConverter.GetBytes((long)destination), 0, replacementFunctionCode, 2, 8);
                replacementFunctionCode[10] = 0xFF;
                replacementFunctionCode[11] = 0xE0;
                targetFunctionAddress = source;
            }

            public FunctionHookX64(IntPtr source, Delegate destination) :
                this(source, Marshal.GetFunctionPointerForDelegate(destination))
            {
                destinationDelegate = destination;
            }

            public void Install()
            {
                Marshal.Copy(replacementFunctionCode, 0, targetFunctionAddress, RequiredBytesCount);
            }

            public void Uninstall()
            {
                Marshal.Copy(targetFunctionCode, 0, targetFunctionAddress, RequiredBytesCount);
            }

            public void Dispose()
            {
                Uninstall();
                destinationDelegate = null;
                VirtualProtect(targetFunctionAddress, RequiredBytesCount, originalProtection, out _);
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, Protection flNewProtect, out Protection lpflOldProtect);

            public enum Protection
            {
                PAGE_NOACCESS = 0x01,
                PAGE_READONLY = 0x02,
                PAGE_READWRITE = 0x04,
                PAGE_WRITECOPY = 0x08,
                PAGE_EXECUTE = 0x10,
                PAGE_EXECUTE_READ = 0x20,
                PAGE_EXECUTE_READWRITE = 0x40,
                PAGE_EXECUTE_WRITECOPY = 0x80,
                PAGE_GUARD = 0x100,
                PAGE_NOCACHE = 0x200,
                PAGE_WRITECOMBINE = 0x400
            }
        }

        // Define delegates for the LoadLibrary functions.
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        private delegate IntPtr LoadLibraryA_Delegate(string lpFileName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate IntPtr LoadLibraryW_Delegate(string lpFileName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        private delegate IntPtr LoadLibraryExA_Delegate(string lpFileName, IntPtr hFile, uint dwFlags);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate IntPtr LoadLibraryExW_Delegate(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr InModule, string InProcName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string InPath);

      

        private static void Initialize()
        {
            try {

                bool is64BitProcess = IntPtr.Size == 8;
                IntPtr moduleHandle = GetModuleHandle("kernel32.dll");
                IntPtr loadLibraryAHandle = GetProcAddress(moduleHandle, "LoadLibraryA");

                if (is64BitProcess)
                {
                    FunctionHookX64 loadLibAHook = null;
                    loadLibAHook = new FunctionHookX64(loadLibraryAHandle, new LoadLibraryA_Delegate((fileName) =>
                    {
                        loadLibAHook.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryA(fileName);
                        loadLibAHook.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }
                else
                {
                    FunctionHookX86 loadLibAHook = null;
                    loadLibAHook = new FunctionHookX86(loadLibraryAHandle, new LoadLibraryA_Delegate((fileName) =>
                    {
                        loadLibAHook.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryA(fileName);
                        loadLibAHook.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }


                // Hook LoadLibraryW
                IntPtr loadLibraryWHandle = GetProcAddress(moduleHandle, "LoadLibraryW");
                if (is64BitProcess)
                {
                    FunctionHookX64 LoadLibraryW = null;
                    LoadLibraryW = new FunctionHookX64(loadLibraryWHandle, new LoadLibraryW_Delegate((fileName) =>
                    {
                        LoadLibraryW.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryW(fileName);
                        LoadLibraryW.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }
                else
                {
                    FunctionHookX86 LoadLibraryW = null;
                    LoadLibraryW = new FunctionHookX86(loadLibraryWHandle, new LoadLibraryW_Delegate((fileName) =>
                    {
                        LoadLibraryW.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryW(fileName);
                        LoadLibraryW.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }

                // Hook LoadLibraryExA
                IntPtr loadLibraryExAHandle = GetProcAddress(moduleHandle, "LoadLibraryExA");
                if (is64BitProcess)
                {
                    FunctionHookX64 LoadLibraryExA = null;
                    LoadLibraryExA = new FunctionHookX64(loadLibraryExAHandle, new LoadLibraryExA_Delegate((fileName, hFile, dwFlags) =>
                    {
                        LoadLibraryExA.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryExA(fileName, hFile, dwFlags);
                        LoadLibraryExA.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }
                else
                {
                    FunctionHookX86 LoadLibraryExA = null;
                    LoadLibraryExA = new FunctionHookX86(loadLibraryExAHandle, new LoadLibraryExA_Delegate((fileName, hFile, dwFlags) =>
                    {
                        LoadLibraryExA.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryExA(fileName, hFile, dwFlags);
                        LoadLibraryExA.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }

                // Hook LoadLibraryExW
                IntPtr loadLibraryExWHandle = GetProcAddress(moduleHandle, "LoadLibraryExW");
                if (is64BitProcess)
                {
                    FunctionHookX64 LoadLibraryExW = null;
                    LoadLibraryExW = new FunctionHookX64(loadLibraryExWHandle, new LoadLibraryExW_Delegate((fileName, hFile, dwFlags) =>
                    {
                        LoadLibraryExW.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryExW(fileName, hFile, dwFlags);
                        LoadLibraryExW.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }
                else
                {
                    FunctionHookX86 LoadLibraryExW = null;
                    LoadLibraryExW = new FunctionHookX86(loadLibraryExWHandle, new LoadLibraryExW_Delegate((fileName, hFile, dwFlags) =>
                    {
                        LoadLibraryExW.Uninstall(); // Uninstall the hook to avoid loader lock
                        var libraryPointer = NativeMethods.LoadLibraryExW(fileName, hFile, dwFlags);
                        LoadLibraryExW.Install(); // Reinstall the hook
                        return libraryPointer; // Return either the library handle or IntPtr.Zero if the library is not trusted
                    }));
                }

            }
            catch { }
        }


        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern IntPtr LoadLibraryA(string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibraryW(string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern IntPtr LoadLibraryExA(string lpFileName, IntPtr hFile, uint dwFlags);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);
        }


    }
}
