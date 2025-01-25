using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HydraEngine.Core;
using HydraEngine.Runtimes.AntiMemoryEditing.Runtime;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Runtime.CompilerServices;

namespace HydraEngine.Runtimes.AntiMemoryEditing
{
      public class AntiMemoryEdit : Models.Protection
    {
        public AntiMemoryEdit() : base("Protection.Rubtunes.AntiMemoryEditing", "Renamer Phase", "Description for Renamer Phase") { }

        public override async Task<bool> Execute(ModuleDefMD md)
        {
            try
            {
                //foreach (var type in md.Types)
                //{
                //    if (!Analyzer.CanRename(type)) continue;
                //    if (type == md.GlobalType) continue;
                //    foreach (MethodDef method in type.Methods)
                //    {
                //        if (!method.HasBody) continue;
                //        if (!method.Body.HasInstructions) continue;

                    
                //    }
                //}

                //if (!parameters.Targets.Any(a => a is FieldDef fd && fd.Module == context.CurrentModule))
                //    return;

                //var m = md;

                ////get services
                //var service = context.Registry.GetService<IMemoryEditService>();
                //var marker = context.Registry.GetService<IMarkerService>();
                //var name = context.Registry.GetService<INameService>();

                ////import type
                //var obfType = RuntimeHelper.GetType(typeof(ObfuscatedValue<>));
                //var newType = new TypeDefUser(obfType.Namespace, obfType.Name, new Importer(m).Import(typeof(object)));
                //newType.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
                //m.Types.Add(newType);
                //var injected = InjectHelper.Inject(obfType, newType, m);
                //service.SetWrapperType(m, newType);

                ////find read/write methods
                //var methods = newType.FindMethods("op_Implicit").ToArray();
                //service.SetReadMethod(m, methods[0]);
                //service.SetWriteMethod(m, methods[1]);

                ////mark type for renaming
                //name.MarkHelper(newType, marker, Parent);

                ////workaround for issue below
                //foreach (IDnlibDef def in injected)
                //    marker.Mark(def, Parent);

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
