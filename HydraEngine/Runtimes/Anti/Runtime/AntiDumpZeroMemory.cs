using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace HydraEngine.Runtimes.Anti.Runtime
{
    internal class JitFuck
    {
        [DllImport("kernel32")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string name);

        [DllImport("kernel32")]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        public static void Init_Jit()
        {
            try
            {
                if (IntPtr.Size == 8)
                {
                    PatchEtw(new List<byte> { 0x48, 0x33, 0xC0, 0xC3 });
                }
                else if (IntPtr.Size == 4)
                {
                    PatchEtw(new List<byte> { 0x33, 0xc0, 0xc2, 0x14, 0x00 });
                }

                Process currentProcess = Process.GetCurrentProcess();
                List<IntPtr> addresses = new List<IntPtr>();

                string[] clrNames = new string[] { "clr.dll", "coreclr.dll"};
                string[] clrjitNames = new string[] { "clrjit.dll", "coreclrjit.dll" };

                foreach (var clrName in clrNames)
                {
                    IntPtr clrAddress  = GetAddress(currentProcess, clrName);
                    if (clrAddress != IntPtr.Zero)
                    {
                        addresses.Add(clrAddress);
                    }
                }

                foreach (var clrjitName in clrjitNames)
                {
                    IntPtr clrjitAddress  = GetAddress(currentProcess, clrjitName);
                    if (clrjitAddress != IntPtr.Zero)
                    {
                        addresses.Add(clrjitAddress);
                    }
                }

                foreach (IntPtr address in addresses)
                {
                    try
                    {
                        EraseHeader(address);
                    }
                    catch { }
                }
            }
            catch { }
        }



        #region "Erase PE Header"

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr ZeroMemory(IntPtr addr, IntPtr size);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr VirtualProtect(IntPtr lpAddress, IntPtr dwSize, IntPtr flNewProtect, ref IntPtr lpflOldProtect);

        public static bool EraseHeader(IntPtr base_address)
        {
            try
            {
                if (base_address == IntPtr.Zero || (long)base_address == -1) { return false; }

                List<int> sectiontabledwords = new List<int>() { 0x8, 0xC, 0x10, 0x14, 0x18, 0x1C, 0x24 };
                List<int> peheaderbytes = new List<int>() { 0x1A, 0x1B };
                List<int> peheaderwords = new List<int>() { 0x4, 0x16, 0x18, 0x40, 0x42, 0x44, 0x46, 0x48, 0x4A, 0x4C, 0x5C, 0x5E };
                List<int> peheaderdwords = new List<int>() { 0x0, 0x8, 0xC, 0x10, 0x16, 0x1C, 0x20, 0x28, 0x2C, 0x34, 0x3C, 0x4C, 0x50, 0x54, 0x58, 0x60, 0x64, 0x68, 0x6C, 0x70, 0x74, 0x104, 0x108, 0x10C, 0x110, 0x114, 0x11C };

                long baseAddr = base_address.ToInt64();
                int dwpeheader = 0;
                int wnumberofsections = 0;

                try
                {
                    dwpeheader = System.Runtime.InteropServices.Marshal.ReadInt32((IntPtr)(baseAddr + 0x3C));
                    wnumberofsections = System.Runtime.InteropServices.Marshal.ReadInt16((IntPtr)(baseAddr + dwpeheader + 0x6));
                }
                catch { }

                for (int i = 0; i < peheaderwords.Count; i++)
                {
                    EraseSection((IntPtr)(baseAddr + dwpeheader + peheaderwords[i]), 2);
                }
                for (int i = 0; i < peheaderbytes.Count; i++)
                {
                    EraseSection((IntPtr)(baseAddr + dwpeheader + peheaderbytes[i]), 1);
                }

                int x = 0;
                int y = 0;

                while (x <= wnumberofsections)
                {
                    if (y == 0)
                    {
                        EraseSection((IntPtr)((baseAddr + dwpeheader + 0xFA + (0x28 * x)) + 0x20), 2);
                    }

                    y++;

                    if (y == sectiontabledwords.Count)
                    {
                        x++;
                        y = 0;
                    }
                }
                return true;
            }
            catch { return false; }
        }

        private static void EraseSection(IntPtr address, int size)
        {
            try
            {
                IntPtr sz = (IntPtr)size;
                IntPtr dwOld = default(IntPtr);
                VirtualProtect(address, sz, (IntPtr)0x40, ref dwOld);
                ZeroMemory(address, sz);
                IntPtr temp = default(IntPtr);
                VirtualProtect(address, sz, dwOld, ref temp);
            }
            catch { }
        }

        #endregion

        #region " Methods "

        private static IntPtr GetAddress(Process process, string moduleName)
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
            return IntPtr.Zero;
        }

        private static bool PatchEtw(List<byte> patch)
        {
            try
            {

                uint oldProtect;

                var ntdll = LoadLibrary("ntdll.dll");
                var etwEventSend = GetProcAddress(ntdll, "EtwEventWrite");

                VirtualProtect(etwEventSend, (UIntPtr)patch.ToArray().Length, 0x40, out oldProtect);
                Marshal.Copy(patch.ToArray(), 0, etwEventSend, patch.ToArray().Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

    }
}
