using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydraEngine.Core;

namespace HydraEngine.Certificate
{
    public class Cert
    {
        public  byte[] Content {  get; set; } = new byte[0];
    }

    public class Sigthief
    {
        public PEFileInfo flItms = null;

        public Sigthief(string FilePath) {
            flItms = PEFileInfo.GatherFileInfoWin(FilePath);
        }

        public byte[] ExtractCert(string FilePath)
        {
            var cert = GetCert(FilePath);
            return cert.Content;
        }

        public  bool HaveSig()
        {
            return (flItms?.CertLoc != 0 || flItms?.CertSize != 0);
        }

        public  bool Removesig(string original, string output = "")
        {
            if (!HaveSig()) return false;

            if (string.IsNullOrEmpty(output)) output = Path.Combine(Path.GetDirectoryName(original), Path.GetFileNameWithoutExtension(original) + "_nosig" + Path.GetExtension(original));

            File.Copy(original, output, true);

            using (var binary = new BinaryWriter(File.Open(output, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                binary.BaseStream.SetLength(flItms.CertLoc);
            }
            return true;
        }

        public  bool InjectCert(Cert cert, string original, string output = "")
        {

            if (string.IsNullOrEmpty(output)) output = Path.Combine(Path.GetDirectoryName(original), Path.GetFileNameWithoutExtension(original) + "_signed" + Path.GetExtension(original));

            File.Copy(original, output, true);

            using (var g = new BinaryReader(File.Open(original, FileMode.Open, FileAccess.Read)))
            {
                using (var f = new BinaryWriter(File.Open(output, FileMode.OpenOrCreate, FileAccess.Write)))
                {
                    f.Write(g.ReadBytes((int)g.BaseStream.Length));
                    f.BaseStream.Seek(flItms.CertTableLoc, SeekOrigin.Begin);
                    f.Write(BitConverter.GetBytes((int)new FileInfo(original).Length));
                    f.Write(BitConverter.GetBytes(cert.Content.Length));
                    f.Seek(0, SeekOrigin.End);
                    f.Write(cert.Content);
                }
            }

           return true;
        }

        public Cert GetCert(string Target)
        {
            Cert cert = new Cert();
            bool HaveCert = HaveSig();

            if (HaveCert)
            {
                using (var f = new BinaryReader(File.Open(Target, FileMode.Open, FileAccess.Read)))
                {
                    f.BaseStream.Seek(flItms.CertLoc, SeekOrigin.Begin);
                    cert.Content = f.ReadBytes((int)flItms.CertSize);
                }
            }

            return cert;
        }
        
        public static bool CloneCert(string Original, string Target, string ouput)
        {
            try {
                Sigthief SigManager = new Sigthief(Original); 
                Cert cert = SigManager.GetCert(Original);
                return SigManager.InjectCert(cert, Target, ouput); ;
            } catch { return false; } 
        }
    }

}
