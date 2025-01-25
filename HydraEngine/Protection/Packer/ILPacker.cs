using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using HydraEngine.Core;
using HydraEngine.Protection.Misc;
using HydraEngine.Protection.Packer.NetBuilderInjection;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace HydraEngine.Protection.Packer
{
     public class ILPacker : Models.Pack
    {
        public ILPacker() : base("Protection.Pack.ILPacker", "Renamer Phase", "Description for Renamer Phase") { }


       public string BaseChars = "日本書紀العالمحالعجلة林氏家族การดำน้ำดูปะการังसंस्कृतम्संस्कृतावाक्" +
              "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
              "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ" +
              "αβγδεζηθικλµνξοπρστυϕχψω" +
              "れづれなるまゝに日暮らし硯にむかひて心にうりゆくよな事を、こはかとなく書きつくればあやうこそものぐるほけれ。";


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
            //try
            //{
                //return Execute(ModuleDefMD.Load(FilePath), Ouput).GetAwaiter().GetResult();
                ModuleDefMD originModule = ModuleDefMD.Load(FilePath);
                ModuleDefMD ILoaderModule = ModuleDefMD.Load(HydraEngine.Properties.Resources.ILoader);

                TypeDef loaderType = ILoaderModule.Types.FirstOrDefault(type => type.Name == "Loader");

                if (loaderType == null)
                {
                    throw new Exception("Unknown Loader");
                }

                MethodDef mainMethod = loaderType.Methods.FirstOrDefault(method => method.Name == "Main");

                if (mainMethod == null)
                {
                    throw new Exception("Unknown Main");
                }

                ILoaderModule.EntryPoint = mainMethod;
                ILoaderModule.Kind = assemblyMap.Kind;
                ILoaderModule.Is32BitPreferred = assemblyMap.Is32BitPreferred;
                ILoaderModule.Characteristics = originModule.Characteristics;
            ILoaderModule.Cor20HeaderFlags = originModule.Cor20HeaderFlags;
            ILoaderModule.Cor20HeaderRuntimeVersion = originModule.Cor20HeaderRuntimeVersion;
            ILoaderModule.DllCharacteristics = originModule.DllCharacteristics;
            ILoaderModule.EncBaseId = originModule.EncBaseId;
            ILoaderModule.EncId = originModule.EncId;
            ILoaderModule.Generation = originModule.Generation;
            ILoaderModule.Machine = originModule.Machine;
            ILoaderModule.RuntimeVersion = originModule.RuntimeVersion;
            ILoaderModule.TablesHeaderVersion = originModule.TablesHeaderVersion;
            //ILoaderModule.Win32Resources = originModule.Win32Resources;

            originModule.Dispose();

                MethodDef EntryPoint = assemblyMap.EntryPoint;
                byte[] Payload = FilePath.ToShellCode(EntryPoint, string.Format("Hydra_{0}", Randomizer.GenerateRandomString(BaseChars, 10)));
                if (Payload != null)
                {
                    string ResName = string.Format("Hydra_{0}", Randomizer.GenerateRandomString());

                    byte[] compressedPayload = HydraEngine._7zip.QuickLZ.CompressBytes(Payload);
                    var res = new EmbeddedResource(ResName, compressedPayload, ManifestResourceAttributes.Private);
                    ILoaderModule.Resources.Add(res);

                    foreach (Instruction Instruction in mainMethod.Body.Instructions.Where((Instruction I) => I.OpCode == OpCodes.Ldstr))
                    {
                        if (Instruction.Operand.ToString() == "ShellName")
                            Instruction.Operand = ResName;

                        if (Instruction.Operand.ToString() == "Key")
                            Instruction.Operand = Convert.ToString(new Random().Next(1, 9));
                    }

                    var renamer = new HydraEngine.Protection.Renamer.RenamerPhase
                    {
                        tag = "HailHydra",
                        Mode = Renamer.RenamerPhase.RenameMode.Ascii,
                        BaseChars = BaseChars,
                        Length = 50
                    };

                    renamer.Namespace = true;
                    renamer.NamespaceEmpty = true;
                    renamer.ClassName = true;
                    renamer.Methods = true;
                    renamer.Properties = true;
                    renamer.Fields = true;
                    renamer.Events = true;
                    renamer.ModuleRenaming = true;
                    renamer.ModuleInvisible = true;

                    JunkCode Junk = new Protection.Misc.JunkCode();
                    Junk.BaseChars = BaseChars;
                    Junk.tag = ".";
                    Junk.number = 100;

                    await Junk.Execute(ILoaderModule);

                    await renamer.Execute(ILoaderModule);

                    await new Protection.CodeOptimizer.OptimizeCode().Execute(ILoaderModule);
                    await new Protection.Mutations.Mutator().Execute(ILoaderModule);
                    await new Protection.Mutations.Mutatorv2().Execute(ILoaderModule);
                    await new Protection.Proxy.ProxyReferences().Execute(ILoaderModule);
                    await new Protection.String.StringsHider().Execute(ILoaderModule);
                    await new Protection.Method.HideMethods().Execute(ILoaderModule);
                    //await new Runtimes.Anti.ErasePEHeader().Execute(ILoaderModule);

                    ModuleWriterOptions writerOptions = new ModuleWriterOptions(ILoaderModule);
                    writerOptions.Logger = DummyLogger.NoThrowInstance;
                    writerOptions.PEHeadersOptions.Subsystem = dnlib.PE.Subsystem.WindowsCui;
                    writerOptions.Cor20HeaderOptions.Flags = ComImageFlags.ILOnly | ComImageFlags.Bit32Preferred;
                    writerOptions.Cor20HeaderOptions.EntryPoint = mainMethod.MDToken.ToUInt32();
                    writerOptions.WritePdb = false;

                    ILoaderModule.Write(Ouput);

                    return true;
                }
                else { throw new Exception("Unknown error."); }
            //}
            //catch (Exception Ex)
            //{
            //    this.Errors = Ex;
            //    return false;
            //}
}

    }
}
