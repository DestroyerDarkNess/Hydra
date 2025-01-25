using dnlib.DotNet.Emit;
using dnlib.DotNet;
using HydraEngine.Core;
using HydraEngine.Runtimes.Anti.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.PE;
using System.Configuration.Assemblies;
using AsmResolver.DotNet.Signatures.Types;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using System.IO;
using AsmResolver.PE.File.Headers;
using HydraEngine.Protection.VM;

namespace HydraEngine.Runtimes.Anti
{
   public class ExtremeAD : Models.Protection
    {
        public bool Protect = false;

        public string BaseChars = "日本書紀العالمحالعجلة林氏家族การดำน้ำดูปะการังसंस्कृतम्संस्कृतावाक्" +
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
        "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ" +
        "αβγδεζηθικλµνξοπρστυϕχψω" +
        "れづれなるまゝに日暮らし硯にむかひて心にうりゆくよな事を、こはかとなく書きつくればあやうこそものぐるほけれ。";

        public ExtremeAD() : base("Runtimes.Anti.ExtremeAntidump", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {

                byte[] ExtremeAntiDump = null;

                if (IsNetCoreApp) { ExtremeAntiDump = HydraEngine.Properties.Resources.ExtremeAntiDump_HailHydra; } else { ExtremeAntiDump = HydraEngine.Properties.Resources.Antidump_HailHydra; }

                ModuleDefMD AntiDump = ModuleDefMD.Load(ExtremeAntiDump);
                var options = new ModuleWriterOptions(AntiDump);
                options.Logger = DummyLogger.NoThrowInstance;
                options.WritePdb = false;

                var peImage = module.Metadata.PEImage;
                var machineType = peImage.ImageNTHeaders.FileHeader.Machine;
                bool Isx64 = machineType == Machine.AMD64 || machineType == Machine.IA64;

                if (Isx64) {

                    AntiDump.Machine = module.Machine;
                    AntiDump.Is32BitPreferred = module.Is32BitPreferred;

                    options.PEHeadersOptions = new PEHeadersOptions()
                    {
                        NumberOfRvaAndSizes = 16,
                        Machine = Machine.AMD64
                    };
                    
                }


                using (MemoryStream ms = new MemoryStream())
                {
                    if (Protect)
                    {
                        var renamer = new HydraEngine.Protection.Renamer.RenamerPhase
                        {
                            tag = "HydraAD",
                            Mode = HydraEngine.Protection.Renamer.RenamerPhase.RenameMode.Key,
                            BaseChars = BaseChars,
                            Length = 10
                        };

                        renamer.Namespace = true;
                        renamer.NamespaceEmpty = true;
                        renamer.ClassName = true;
                        renamer.ModuleRenaming = true;
                        renamer.ModuleInvisible = true;

                        await renamer.Execute(AntiDump);
                    }
                   
                    AntiDump.Write(ms, options);
                    ExtremeAntiDump = ms.ToArray();
                }

                string ResName = AntiDump.Assembly.Name; // string.Format("Hydra_{0}", Randomizer.GenerateRandomString());

                AntiDump.Dispose();

                byte[] compressedPayload = HydraEngine._7zip.QuickLZ.CompressBytes(ExtremeAntiDump);
                var res = new EmbeddedResource(ResName, compressedPayload, ManifestResourceAttributes.Private);
                module.Resources.Add(res);


                var typeModule = ModuleDefMD.Load(typeof(ExtremeAntidump).Module);
                var cctor = module.GlobalType.FindOrCreateStaticConstructor();
                var typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(ExtremeAntidump).MetadataToken));
                var members = InjectHelper.Inject(typeDef, module.GlobalType, module);
                var init = (MethodDef)members.Single(method => method.Name == "Extreme");
                cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
                init.Name = "<" + Core.Randomizer.GenerateRandomString2() + ">";

                foreach (Instruction Instruction in init.Body.Instructions.Where((Instruction I) => I.OpCode == OpCodes.Ldstr))
                {
                    if (Instruction.Operand.ToString() == "ShellName")
                        Instruction.Operand = ResName;

                    if (Instruction.Operand.ToString() == "Key")
                        Instruction.Operand = Convert.ToString(new Random().Next(1, 9));
                }

                foreach (var md in module.GlobalType.Methods)
                {
                    if (md.Name != ".ctor") continue;
                    module.GlobalType.Remove(md);
                    break;
                }

                return true;
            }
            catch (Exception Ex)
            {
                this.Errors = Ex;
                return false;
            }
        }

        public override Task<bool> Execute(string assembly)
        {
            throw new NotImplementedException();
        }
    }
}
