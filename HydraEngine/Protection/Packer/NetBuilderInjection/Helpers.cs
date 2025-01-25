using HydraEngine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Packer.NetBuilderInjection
{
    internal class Helpers
    {
        public static string UnzipTCC_Compiler(string DirToExtract)
        {

            bool ExtractTCC = false;

            string CCompilerDir = Path.Combine(Path.GetDirectoryName(DirToExtract), "TCC");

            if (Directory.Exists(CCompilerDir) == false) { Directory.CreateDirectory(CCompilerDir); ExtractTCC = true; }

            string CCompilerExe = Path.Combine(CCompilerDir, "tcc.exe");

            if (File.Exists(CCompilerExe) == false) { ExtractTCC = true; }

            if (ExtractTCC == true)
            {
                string TempWriteZip = Path.Combine(Path.GetTempPath(), "Tcc.zip");

                if (File.Exists(TempWriteZip) == true) { File.Delete(TempWriteZip); }

                File.WriteAllBytes(TempWriteZip, HydraEngine.Properties.Resources.Bin);

                System.IO.Compression.ZipFile.ExtractToDirectory(TempWriteZip, CCompilerDir);
            }

            return CCompilerExe;

        }
    }
}
