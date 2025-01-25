using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HydraEngine.Runtimes.Anti.Runtime
{
    internal static class HideThreads
    {
        private const uint THREAD_SET_INFORMATION = 0x0020;
        private const uint ThreadHideFromDebugger = 0x11; // THREADINFOCLASS

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, int dwThreadId);

        [DllImport("ntdll.dll")]
        private static extern int NtSetInformationThread(IntPtr ThreadHandle, uint ThreadInformationClass, IntPtr ThreadInformation, int ThreadInformationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static void HideThread()
        {
            var count = 0;
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                IntPtr handle = OpenThread(THREAD_SET_INFORMATION, false, thread.Id);
                if (handle != IntPtr.Zero)
                {
                    try
                    {
                        int ntstatus = NtSetInformationThread(handle, ThreadHideFromDebugger, IntPtr.Zero, 0);
                        if (ntstatus == 0)
                        {
                            count++;
                        }
                        else
                        {
                            //Console.WriteLine("Failed to hide thread {0}. NTSTATUS {1}.", thread.Id, ntstatus);
                        }
                    }
                    finally
                    {
                        CloseHandle(handle);
                    }
                }
                else
                {
                    //Console.WriteLine("Failed to open thread {0}.", thread.Id);
                }
            }
            //Console.WriteLine("Hidden threads: {0}", count);
        }
    }
}
