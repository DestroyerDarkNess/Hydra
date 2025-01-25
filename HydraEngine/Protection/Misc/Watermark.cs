using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HydraEngine.Protection.Misc
{
     public class Watermark : Models.Protection
    {
        public Watermark() : base("Protection.Misc.Watermark", "Renamer Phase", "Description for Renamer Phase") { }

        public string Mark { get; set; } = "Hail Hydra";

        public string Coment { get; set; } = "« If one head is cut off, two more will take its place »";

        public override async Task<bool> Execute(ModuleDefMD md)
        {
            try
            {

                foreach (var moduleDef in md.Assembly.Modules)
                {
                    TypeRef attrRef = moduleDef.CorLibTypes.GetTypeRef("System", "Attribute");
                    var attrType = new TypeDefUser("", Mark, attrRef);
                    moduleDef.Types.Add(attrType);
                    var ctor = new MethodDefUser(
                        ".ctor",
                        MethodSig.CreateInstance(moduleDef.CorLibTypes.Void, moduleDef.CorLibTypes.String),
                        dnlib.DotNet.MethodImplAttributes.Managed,
                        dnlib.DotNet.MethodAttributes.HideBySig | dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.SpecialName | dnlib.DotNet.MethodAttributes.RTSpecialName);
                    ctor.Body = new CilBody();
                    ctor.Body.MaxStack = 1;
                    ctor.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
                    ctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(new MemberRefUser(moduleDef, ".ctor", MethodSig.CreateInstance(moduleDef.CorLibTypes.Void), attrRef)));
                    ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                    attrType.Methods.Add(ctor);
                    var attr = new CustomAttribute(ctor);
                    attr.ConstructorArguments.Add(new CAArgument(moduleDef.CorLibTypes.String, Coment));
                    moduleDef.CustomAttributes.Add(attr);
                }

                byte[] ImageLegacy = ImageToByte(Properties.Resources.Hydra_Ex_Legacy);
                EmbeddedResource resource = new EmbeddedResource("Hydra_Ex_Legacy", ImageLegacy, ManifestResourceAttributes.Public);

                md.Resources.Add(resource);

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

        public  byte[] ImageToByte(System.Drawing.Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

    }
}
