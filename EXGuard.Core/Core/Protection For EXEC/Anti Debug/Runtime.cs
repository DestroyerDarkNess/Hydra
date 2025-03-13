using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EXGuard.Core.EXECProtections
{
    internal static class AntiDebug_Runtime
    {
		static void Initialize()
		{
			string x = "COR";
			var env = typeof(Environment);
			var method = env.GetMethod("GetEnvironmentVariable", new[] { typeof(string) });
			if (method != null && "1".Equals(method.Invoke(null, new object[] { x + "_ENABLE_PROFILING" })))
				Environment.FailFast(null);

			if (Environment.GetEnvironmentVariable(x + "_PROFILER") != null || Environment.GetEnvironmentVariable(x + "_ENABLE_PROFILING") != null)
				Environment.FailFast(null);

			var thread = new Thread(Worker);
			thread.IsBackground = true;
			thread.Start(null);
		}

		[DllImport("kernel32.dll")]
		static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll")]
		static extern bool IsDebuggerPresent();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		static extern int OutputDebugString(string str);

		[DllImport("Kernel32.dll", SetLastError = true)]
		static extern IntPtr GetCurrentThread();

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] ref bool isDebuggerPresent);
		
		[DllImport("Ntdll.dll", SetLastError = true)]
	    static extern uint NtSetInformationThread(IntPtr hThread, int ThreadInformationClass, IntPtr ThreadInformation, uint ThreadInformationLength);

		[DllImport("ntdll.dll", SetLastError = true)]
		static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, IntPtr processInformation, uint processInformationLength, IntPtr returnLength);

		static void Worker(object thread)
		{
			var th = thread as Thread;
			if (th == null)
			{
				th = new Thread(Worker);
				th.IsBackground = true;
				th.Start(Thread.CurrentThread);

				Thread.Sleep(500);
			}

			while (true)
			{
				//Managed
				if (Debugger.IsAttached || Debugger.IsLogging())
					Process.GetCurrentProcess().Kill();

				//CheckRemoteDebuggerPresent
				bool present = false;
				CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref present);
				if (present)
					Process.GetCurrentProcess().Kill();

				//NtSetInformationThread
				uint Status = NtSetInformationThread(GetCurrentThread(), 17, IntPtr.Zero, 0);
				if (Status != 0)
					Process.GetCurrentProcess().Kill();

				//NtQueryInformationProcess
				IntPtr NoDebugInherit = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt32)));
				var status2 = NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 0x1f, NoDebugInherit, 4, IntPtr.Zero);
				if (((uint)Marshal.PtrToStructure(NoDebugInherit, typeof(uint))) == 0)
					Process.GetCurrentProcess().Kill();

				IntPtr hDebugObject = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));
				var status3 = NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 0x1e, hDebugObject, 4, IntPtr.Zero);
				if (status3 == 0)
					Process.GetCurrentProcess().Kill();

				// IsDebuggerPresent
				if (IsDebuggerPresent())
					Process.GetCurrentProcess().Kill();

				// OpenProcess
				Process ps = Process.GetCurrentProcess();
				if (ps.Handle == IntPtr.Zero)
					Process.GetCurrentProcess().Kill();

				ps.Close();

				// OutputDebugString
				if (OutputDebugString("") > IntPtr.Size)
					Process.GetCurrentProcess().Kill();

				if (!th.IsAlive)
					Process.GetCurrentProcess().Kill();

				Thread.Sleep(5000);
			}
		}
	}
}
