using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VM.Core.Protections;
using VM.Core.Protections.Impl.UStrings;
using VM.Core.Protections.Impl.Virtualization;

namespace HydraEngine.Protection.VM
{
    public class Virtualizer : Models.Protection
    {
        public static Virtualizer Instance { get; private set; }

        public Virtualizer() : base("Protection.Renamer.Virtualizer", "Renamer Phase", "Description for Renamer Phase") { Instance = this; ManualReload = true; TempModule = new MemoryStream(); }

        public string BaseChars = "日本書紀العالمحالعجلة林氏家族การดำน้ำดูปะการังसंस्कृतम्संस्कृतावाक्" +
          "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
          "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ" +
          "αβγδεζηθικλµνξοπρστυϕχψω" +
          "れづれなるまゝに日暮らし硯にむかひて心にうりゆくよな事を、こはかとなく書きつくればあやうこそものぐるほけれ。";

        public bool InjectRuntime { get; set; } = true;
        public bool ProtectRuntime { get; set; } = false;
        public ModuleDefMD Module { get; set; }
        public ModuleDefMD RTModule { get; set; }
        public Importer Importer { get; set; }

        public List<IProtection> Protections { get; set; }
        public List<string> VirtualizedMethods = new List<string>();
        public TypeDef theType = null;
        public MethodDef theMethod = null;

        //Need opts.MetadataOptions.Flags = MetadataFlags.PreserveAll;

        public override async Task<bool> Execute(ModuleDefMD module)
        {
            try
            {
                Module = module;
                RTModule = ModuleDefMD.Load("VM.Runtime.dll");
                Importer = new Importer(module);
                Protections = new List<IProtection>()
            {
                //new VStrings(),
                new Virtualization()
            };

                this.theType = RTModule.Types.Where(t => t.FullName.Contains("VirtualMachine")).First(); //VirtualMachine
                this.theMethod = theType.Methods.Where(m => m.ReturnType.ToString().Contains("Object")).First(); //RunVM, in case other methods are added.

                foreach (IProtection Protection in Protections)
                {
                    Protection.Execute(this);
                }

                var opts = new ModuleWriterOptions(Module) { Logger = DummyLogger.NoThrowInstance };

            opts.MetadataOptions.Flags = MetadataFlags.PreserveMethodRids | MetadataFlags.PreserveMemberRefRids;  //MetadataFlags.PreserveAllMethodRids;     // MetadataFlags.PreserveRids;

            // PreserveRids | PreserveStringsOffsets | PreserveUSOffsets |
            // PreserveBlobOffsets | PreserveExtraSignatureData,

            Module.Write(this.TempModule, opts);
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
