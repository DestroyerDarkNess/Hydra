using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using HydraEngine.Protection.VM;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VM.Core.Helper;

namespace VM.Core.Protections.Impl.Virtualization
{
    public class Virtualization : IProtection
    {

        public HashSet<MethodDef> Methods = null;

        public override string Name() => "Virtualization";
        public override void Execute(Virtualizer context)
        {
            protectRuntime();
            IMethod runVm = context.Module.Import(Virtualizer.Instance.theMethod);
            //foreach (var type in context.Module.GetTypes().ToArray())
            //{
            //    if (!Analyzer.CanRename(type)) continue;
            //    if (type.FullName.StartsWith(Virtualizer.Instance.RTModule.Assembly.Name)) continue; 
            //}

            foreach (var method in Methods)
            {
                string methodName = method.MDToken.ToInt32().ToString();

                if (method.IsRuntime) continue;

                var name = Generator.RandomName();

                var conv = new Converter(method, name);
                conv.Save();

                if (!conv.Compatible) continue;

                method.Body = new CilBody();

                if (method.Parameters.Count() == 0)
                {
                    method.Body.Instructions.Add(new Instruction(OpCodes.Ldnull));
                }
                else
                {
                    method.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, method.Parameters.Count));
                    method.Body.Instructions.Add(OpCodes.Newarr.ToInstruction(context.Module.CorLibTypes.Object));
                    for (var i = 0; i < method.Parameters.Count; i++)
                    {
                        method.Body.Instructions.Add(new Instruction(OpCodes.Dup));
                        method.Body.Instructions.Add(new Instruction(OpCodes.Ldc_I4, i));
                        method.Body.Instructions.Add(new Instruction(OpCodes.Ldarg, method.Parameters[i]));
                        method.Body.Instructions.Add(new Instruction(OpCodes.Box, method.Parameters[i].Type.ToTypeDefOrRef()));
                        method.Body.Instructions.Add(new Instruction(OpCodes.Stelem_Ref));
                    }
                }
                method.Body.Instructions.Add(new Instruction(OpCodes.Ldstr, methodName));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, runVm));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Calli, runVm.MethodSig));

                if (method.HasReturnType)
                    method.Body.Instructions.Add(new Instruction(OpCodes.Unbox_Any, method.ReturnType.ToTypeDefOrRef()));
                else
                    method.Body.Instructions.Add(OpCodes.Pop.ToInstruction());

                method.Body.Instructions.Add(new Instruction(OpCodes.Ret));

                method.Body.UpdateInstructionOffsets();

                Virtualizer.Instance.VirtualizedMethods.Add(method.FullName);
            }

            if (Virtualizer.Instance.InjectRuntime) injectRuntime();
        }

        private void injectRuntime()
        {
            var runtimeName = $"{Virtualizer.Instance.RTModule.Assembly.Name}";

            #region Inject Runtime DLL
            var opts = new ModuleWriterOptions(Virtualizer.Instance.RTModule) { Logger = DummyLogger.NoThrowInstance };

            opts.MetadataOptions.Flags = MetadataFlags.PreserveAll;

            MemoryStream ms = new MemoryStream();
            //...
            Virtualizer.Instance.RTModule.Write(ms, opts);

            byte[] array = ms.ToArray();
            Virtualizer.Instance.Module.Resources.Add(new EmbeddedResource(runtimeName, Compression.Compress(array), ManifestResourceAttributes.Private));
            #endregion

            #region Inject Runtime DLL Loader
            ModuleDefMD typeModule = ModuleDefMD.Load(typeof(LoadDLLRuntime.VMInitialize).Module);
            TypeDef typeDef = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(LoadDLLRuntime.VMInitialize).MetadataToken));
            IEnumerable<IDnlibDef> members = InjectHelper.Inject(typeDef, Virtualizer.Instance.Module.GlobalType, Virtualizer.Instance.Module);
            MethodDef init = (MethodDef)members.Single(method => method.Name == "InitializeRuntime");
            init.Name = Generator.RandomName();

            Virtualizer.Instance.Module.GlobalType.FindOrCreateStaticConstructor().Body.Instructions.Insert(0, new Instruction(OpCodes.Calli, init.MethodSig));
            Virtualizer.Instance.Module.GlobalType.FindOrCreateStaticConstructor().Body.Instructions.Insert(0, new Instruction(OpCodes.Ldftn, init));
            #endregion

            #region Protect Virtualized Methods
            //ProxyInteger.Execute(Context.Instance.Module);
            //Strings.Execute(Context.Instance.Module);
            //CallToCalli.Execute(Context.Instance.Module);
            #endregion
        }

        private async void protectRuntime()
        {
            /*Patch RunVM GetCallingAssembly*/
            foreach (var x in Virtualizer.Instance.theMethod.Body.Instructions)
                if (x.OpCode == OpCodes.Ldstr && x.Operand.ToString().Contains("{{TExecutingHAssemblyT}}"))
                    x.Operand = Virtualizer.Instance.Module.Assembly.FullName;
            /*Patch RunVM GetCallingAssembly*/

            if (Virtualizer.Instance.ProtectRuntime)
            {

                #region Protect Runtime
                //Strings.Execute(Context.Instance.RTModule);
                // CFlow.Execute(Context.Instance.RTModule);
                //  ProxyInteger.Execute(Context.Instance.RTModule);
                // CallToCalli.Execute(Context.Instance.RTModule);
                //Renamer.Execute(Context.Instance.RTModule); Context.Instance.theMethod.Name = Generator.RandomName();

                var renamer = new HydraEngine.Protection.Renamer.RenamerPhase
                {
                    tag = "HydraVM",
                    Mode = HydraEngine.Protection.Renamer.RenamerPhase.RenameMode.Key,
                    BaseChars = Virtualizer.Instance.BaseChars,
                    Length = 10
                };

                renamer.Resources = false;
                renamer.Namespace = true;
                renamer.NamespaceEmpty = true;
                renamer.ClassName = true;
                renamer.Methods = true;
                renamer.Properties = true;
                renamer.Fields = true;
                renamer.Events = true;
                renamer.ModuleRenaming = true;
                renamer.ModuleInvisible = true;

                await renamer.Execute(Virtualizer.Instance.RTModule);

                #endregion

            }
        }
    }
}