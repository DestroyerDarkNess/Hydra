using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.Packer
{
      public class OrigamiPack : Models.Pack
    {
        public OrigamiPack() : base("Protection.Pack.Origami", "Renamer Phase", "Description for Renamer Phase") { }

        public bool InDebugSection { get; set; } = false;

        public override async Task<bool> Execute(ModuleDefMD module, string Ouput)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public override async Task<bool> Execute(string FilePath, string Ouput)
        {
            try
            {
                Origami.Utils.Module = "<Hydra>";
                Origami.Utils.Section = ".Hydra";

                byte[] payloadData = System.IO.File.ReadAllBytes(FilePath);

                Origami.Packers.IPacker packer;
                if (InDebugSection == true)
                {
                    packer = new Origami.Packers.RelocPacker(Origami.Packers.Mode.DebugDataEntry, payloadData, Ouput);
                }
                else
                {
                    packer = new Origami.Packers.RelocPacker(Origami.Packers.Mode.PESection, payloadData, Ouput);
                }

                packer.Execute();

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

    }
}
