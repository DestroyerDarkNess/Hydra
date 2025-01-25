using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti.Runtime
{
    internal class TestClass
    {

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr ZeroMemory(IntPtr addr, IntPtr size);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr VirtualProtect(IntPtr lpAddress, IntPtr dwSize, IntPtr flNewProtect, ref IntPtr lpflOldProtect);

        public static void Init()
        {
          List<int> sectiontabledwords = new List<int>() { 0x8, 0xC, 0x10, 0x14, 0x18, 0x1C, 0x24 };
            Console.WriteLine("Hello World : " );
            Console.ReadKey();
        }

    }
}
