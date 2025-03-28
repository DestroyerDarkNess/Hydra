﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HydraEngine.Runtimes.Anti.Runtime
{
    internal static class InvokeDetector
    {
        private static void Initialize() {

            string mode = "message";
            if (Assembly.GetExecutingAssembly() != Assembly.GetCallingAssembly())
            {
                Terminate(mode, "Dont Load it On Memory!");
            }
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
