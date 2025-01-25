using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static HydraEngine.Runtimes.Anti.Runtime.AntiAttachRuntime;

namespace HydraEngine.Runtimes.Anti.Runtime
{
    internal static class AntiDebugSafe
    {
        [DllImport("ntdll.dll", CharSet = CharSet.Auto)]
        private static extern int NtQueryInformationProcess(IntPtr test, int test2, int[] test3, int test4, ref int test5);

        private static void Initialize()
        {
            string mode = "message";

            if (Debugger.IsLogging())
            { Terminate(mode, "Debugger Detected!"); }
            if (Debugger.IsAttached)
            { Terminate(mode, "Debugger Detected!"); }
            if (Environment.GetEnvironmentVariable("complus_profapi_profilercompatibilitysetting") != null)
            { Terminate(mode, "Debugger Detected!"); }
            if (string.Compare(Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING"), "1", StringComparison.Ordinal) == 0)
            { Terminate(mode, "Debugger Detected!"); }

            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            var array = new int[6];
            var num = 0;
            var intPtr = Process.GetCurrentProcess().Handle;
            if (NtQueryInformationProcess(intPtr, 31, array, 4, ref num) == 0 && array[0] != 1)
            {
                Terminate(mode, "Debugger Detected!");
            }
            if (NtQueryInformationProcess(intPtr, 30, array, 4, ref num) == 0 && array[0] != 0)
            {
                Terminate(mode, "Debugger Detected!");
            }

            if (NtQueryInformationProcess(intPtr, 0, array, 24, ref num) != 0) return;
            intPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr((IntPtr)array[1], 12), 12);
            Marshal.WriteInt32(intPtr, 32, 0);
            var intPtr2 = Marshal.ReadIntPtr(intPtr, 0);
            var ptr = intPtr2;
            do
            {
                ptr = Marshal.ReadIntPtr(ptr, 0);
                if (Marshal.ReadInt32(ptr, 44) != 1572886 ||
                    Marshal.ReadInt32(Marshal.ReadIntPtr(ptr, 48), 0) != 7536749) continue;
                var intPtr3 = Marshal.ReadIntPtr(ptr, 8);
                var intPtr4 = Marshal.ReadIntPtr(ptr, 12);
                Marshal.WriteInt32(intPtr4, 0, (int)intPtr3);
                Marshal.WriteInt32(intPtr3, 4, (int)intPtr4);
            }
            while (!ptr.Equals(intPtr2));
        }

        #region " CloseApp "

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr ZeroMemory(IntPtr addr, IntPtr size);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr VirtualProtect(IntPtr lpAddress, IntPtr dwSize, IntPtr flNewProtect, ref IntPtr lpflOldProtect);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern IntPtr RtlAdjustPrivilege(int privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(IntPtr hProcess, int processInformationClass, ref int processInformation, int processInformationLength);
        [DllImport("ntdll.dll")]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        private static void Terminate(string Mode, string message = "Malicious App Detected!")
        {
            switch (Mode.ToLower())
            {
                case "crash":
                    foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
                    {
                        try
                        {
                            //IntPtr oldProtect = IntPtr.Zero;
                            //IntPtr baseAddress = IntPtr.Zero;
                            //if (module.ModuleName.ToLower().Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    baseAddress = module.BaseAddress;
                            //}
                            //else if (module.ModuleName.ToLower().Equals("mscoree.dll", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    baseAddress = module.BaseAddress;
                            //}
                            //VirtualProtect(baseAddress, (IntPtr)4096, (IntPtr)0x04, ref oldProtect);
                            //ZeroMemory(baseAddress, (IntPtr)4096);

                            Environment.FailFast("");
                        }
                        catch { }
                    }
                    break;
                case "system":
                    try
                    {
                        int isCritical = 1;
                        int BreakOnTermination = 0x1D;
                        Process.EnterDebugMode();
                        NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref isCritical, sizeof(int));
                    }
                    catch
                    {
                        int SeShutdownPrivilege = 19;
                        uint STATUS_ASSERTION_FAILURE = 0xC0000420;
                        RtlAdjustPrivilege(SeShutdownPrivilege, true, false, out bool previousValue);
                        NtRaiseHardError(STATUS_ASSERTION_FAILURE, 0, 0, IntPtr.Zero, 6, out uint Response);
                    }
                    break;
                case "message":
                    try
                    {
                        string messageFile = Path.Combine(Path.GetTempPath(), "HailHydra.txt");
                        File.WriteAllText(messageFile, message);
                        Process.Start("notepad.exe", messageFile);
                    }
                    catch
                    {
                        Process.Start("cmd.exe", string.Format("/c @echo off & title Hydra.NET & echo {0} & pause", message));
                    }
                    break;
                default:
                    Process.GetCurrentProcess().Kill();
                    break;
            }
            Environment.Exit(0);
        }

        #endregion
    }
}
