using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti.Runtime
{
    internal unsafe static class AntiAttachRuntime
    {
        // P/Invoke to get the module handle
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // P/Invoke to get the address of a function in a module
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        // P/Invoke to write memory to the current process
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, int lpNumberOfBytesWritten);

        // P/Invoke to query information about a process
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern NtStatus NtQueryInformationProcess(
            [In] IntPtr processHandle,
            [In] ProcessInfoClass processInformationClass,
            out IntPtr processInformation,
            [In] int processInformationLength,
            [Optional] out int returnLength);

        // P/Invoke to close a handle
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern NtStatus NtClose([In] IntPtr handle);

        // P/Invoke to remove the debug object from a process
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern NtStatus NtRemoveProcessDebug(IntPtr processHandle, IntPtr debugObjectHandle);

        // P/Invoke to set information for a debug object
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern NtStatus NtSetInformationDebugObject(
            IntPtr debugObjectHandle,
            DebugObjectInformationClass debugObjectInformationClass,
            IntPtr debugObjectInformation,
            int debugObjectInformationLength,
            out int returnLength);

        // P/Invoke to set thread information
        [DllImport("ntdll.dll")]
        internal static extern NtStatus NtSetInformationThread(
            IntPtr threadHandle,
            ThreadInformationClass threadInformationClass,
            IntPtr threadInformation,
            int threadInformationLength);

        // P/Invoke to check if a remote debugger is present
        [DllImport("Kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CheckRemoteDebuggerPresent(SafeHandle hProcess, [MarshalAs(UnmanagedType.Bool)] ref bool isDebuggerPresent);

        // P/Invoke to close a handle
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        // Define enums for process and thread information classes
        internal enum ProcessInfoClass : int
        {
            ProcessBasicInformation= 0,
            ProcessDebugPort = 7,
            ProcessDebugObjectHandle = 30,
        }

        internal enum NtStatus : uint
        {
            Success = 0x00000000,
        }

        internal enum DebugObjectInformationClass : int
        {
            DebugObjectFlags = 1,
        }

        internal enum ThreadInformationClass : int
        {
            ThreadHideFromDebugger = 0x11,
        }

        private static void AntiAttach()
        {
            try
            {
                IntPtr NtdllModule = GetModuleHandle("ntdll.dll");
                IntPtr DbgUiRemoteBreakinAddress = GetProcAddress(NtdllModule, "DbgUiRemoteBreakin");
                IntPtr DbgUiConnectToDbgAddress = GetProcAddress(NtdllModule, "DbgUiConnectToDbg");
                byte[] Int3InvaildCode = new byte[]
                {
                204
                };
                WriteProcessMemory(Process.GetCurrentProcess().Handle, DbgUiRemoteBreakinAddress, Int3InvaildCode, 6U, 0);
                WriteProcessMemory(Process.GetCurrentProcess().Handle, DbgUiConnectToDbgAddress, Int3InvaildCode, 6U, 0);
            }
            catch { }

            // Crea el hilo y apunta al método
            Thread myThread = new Thread(ThreadMethod);

            // Inicia el hilo
            myThread.Start();
        }

       public static void ThreadMethod()
        {
            try
            {
                while (true)
                {
                    //Console.WriteLine("CheckRemoteDebugger: " + CheckRemoteDebugger());
                    //Console.WriteLine("CheckDebugPort: " + CheckDebugPort());

                    DetachFromDebuggerProcess();

                    CloseHandleAntiDebug();

                    //Console.WriteLine("CloseDebuggers");

                    Thread.Sleep(1000);
                }
            }
            catch { }
        }

        public unsafe static bool DetachFromDebuggerProcess()
        {
            uint flags = 0U;
            IntPtr debugObjectHandle;
            int returnLength;

            NtStatus queryStatus = NtQueryInformationProcess(Process.GetCurrentProcess().Handle, ProcessInfoClass.ProcessDebugObjectHandle, out debugObjectHandle, IntPtr.Size, out returnLength);
            if (queryStatus > NtStatus.Success)
            {
                return false;
            }

            NtStatus setStatus = NtSetInformationDebugObject(debugObjectHandle, DebugObjectInformationClass.DebugObjectFlags, new IntPtr((void*)(&flags)), Marshal.SizeOf<uint>(flags), out returnLength);
            if (setStatus > NtStatus.Success)
            {
                return false;
            }

            NtStatus removeStatus = NtRemoveProcessDebug(Process.GetCurrentProcess().Handle, debugObjectHandle);
            if (removeStatus > NtStatus.Success)
            {
                return false;
            }

            NtStatus closeStatus = NtClose(debugObjectHandle);
            return closeStatus <= NtStatus.Success;
        }

        public static bool CheckRemoteDebugger()
        {
            bool isDebuggerPresent = false;
            return CheckRemoteDebuggerPresent(Process.GetCurrentProcess().SafeHandle, ref isDebuggerPresent) && isDebuggerPresent;
        }

        public static bool CheckDebugPort()
        {
            IntPtr processInformation = new IntPtr(0);
            int num;
            return NtQueryInformationProcess(Process.GetCurrentProcess().Handle, ProcessInfoClass.ProcessDebugPort, out processInformation, Marshal.SizeOf<IntPtr>(processInformation), out num) == NtStatus.Success && processInformation == new IntPtr(-1);
        }

        public static bool CloseHandleAntiDebug()
        {
            try
            {
                CloseHandle((IntPtr)14258465L);
                CloseHandle((IntPtr)19075618L);
                return false;
            }
            catch (Exception ex)
            {
                bool flag = ex.Message == "External component has thrown an exception.";
                if (flag)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
