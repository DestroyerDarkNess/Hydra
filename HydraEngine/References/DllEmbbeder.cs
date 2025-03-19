using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HydraEngine.References
{
    public class DllEmbbeder
    {
        public string[] MergeClassNamespace = {
            "SevenZip"
        };

        public void InjectDependencyClasses(ModuleDefMD fromModule, ModuleDefMD toModule)
        {

            //First attach our ModuleLoader class

            var modLoaderType = fromModule.GetTypes().Where(t => t.Name == "ModuleLoader").FirstOrDefault();
            fromModule.Types.Remove(modLoaderType);
            toModule.Types.Add(modLoaderType);

            //Now create a static module constructor that will be responsible
            //for registering AssemblyResolve event and loading our bundled assembiles
            var ctor = toModule.GlobalType.FindOrCreateStaticConstructor();
            var attachDef = modLoaderType.Methods.Where(m => m.Name == "Attach").FirstOrDefault();
            if (ctor.HasBody)
            {
                ctor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(attachDef));
            }
            else
            {
                ctor.Body = new CilBody();
                ctor.Body.Instructions.Add(OpCodes.Call.ToInstruction(attachDef));
                ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            }


            //Finally merge all other generic class dependencies (mainly SevenZip stuff)
            MergeClasses(fromModule, toModule);
        }

        public bool ProcessAssembly(ModuleDefMD module, List<string> ReferencesPath)
        {
            try
            {

                foreach (var referenceCopyLocalFile in ReferencesPath)
                {

                    try
                    {
                        var referenceAssemblyData = System.IO.File.ReadAllBytes(referenceCopyLocalFile);
                        var refModule = ModuleDefMD.Load(referenceAssemblyData);
                        module.Resources.Add(new EmbeddedResource(refModule.Assembly.Name.ToLower(), SevenZip.Compression.LZMA.SevenZipHelper.Compress(referenceAssemblyData)));
                        Console.WriteLine($"Merged assembly {referenceCopyLocalFile}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to merge assembly {referenceCopyLocalFile} with error {e.Message}");
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public void MergeClasses(ModuleDefMD fromModule, ModuleDefMD toModule)
        {

            foreach (var ns in MergeClassNamespace)
            {
                var mergeTypes = fromModule.GetTypes().Where(t => t.Namespace.StartsWith(ns) || t.Namespace.StartsWith($"{ns}.")).ToList();
                foreach (var mergeType in mergeTypes)
                {
                    fromModule.Types.Remove(mergeType);
                    toModule.Types.Add(mergeType);
                }
            }

            //Pull in static initializers that SevenZip relies on
            var privateInit = fromModule.GetTypes().Where(t => t.Name == "<PrivateImplementationDetails>").FirstOrDefault();
            fromModule.Types.Remove(privateInit);

            var targetPrivateInit = toModule.GetTypes().Where(t => t.Name == "<PrivateImplementationDetails>").FirstOrDefault();
            if (targetPrivateInit == null)
            {
                //If our target module doesn't have the <PriviateImplementationDetails> then
                //simply copy the entire type accross
                toModule.Types.Add(privateInit);
            }
            else
            {

                //<PrivateImplementationDetails> already exists so just copy the fields
                //across into the existing type instead.
                var fieldsToCopy = new List<FieldDef>();
                foreach (var field in privateInit.Fields)
                {
                    fieldsToCopy.Add(field);
                }
                fieldsToCopy.ForEach(field =>
                {
                    field.DeclaringType = null;
                    targetPrivateInit.Fields.Add(field);
                });

                //Also copy nested types across
                var typesToCopy = new List<TypeDef>();
                foreach (var type in privateInit.NestedTypes)
                {
                    typesToCopy.Add(type);
                }
                typesToCopy.ForEach(type =>
                {
                    type.DeclaringType = null;
                    targetPrivateInit.NestedTypes.Add(type);
                });
            }
        }

    }
}
