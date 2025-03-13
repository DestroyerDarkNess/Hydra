using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core;
using EXGuard.Core.JIT;

using MethodAttributes = dnlib.DotNet.MethodAttributes;

namespace EXGuard.Internal
{
    public class InitializePhase
    {
        Dictionary<IMemberRef, IMemberRef> refRepl;

        public ModuleDefMD DFModule
        {
            get;
            private set;
        }

        public HashSet<MethodDef> Methods
        {
            get;
            set;
        }

        public Virtualizer VR
        {
            get;
            private set;
        }

        public string RT_OUT_Directory
        {
            get;
            set;
        }

        public string RTName
        {
            get;
            set;
        }

        public string SNK_File
        {
            get;
            set;
        }

        public string SNK_Password
        {
            get;
            set;
        }

        public InitializePhase(ModuleDefMD module)
        {
            DFModule = module;
            Methods = new HashSet<MethodDef>();
            refRepl = new Dictionary<IMemberRef, IMemberRef>();
        }

        public void Initialize()
        {
            Methods = new HashSet<MethodDef>(Methods.Distinct().ToList());
            VR = new Virtualizer(DFModule, RTName);

            var oldType = DFModule.GlobalType;
            var newType = new TypeDefUser(oldType.Name);

            oldType.Name = "EXGuarder";
            oldType.BaseType = DFModule.CorLibTypes.GetTypeRef("System", "Object");

            DFModule.Types.Insert(0, newType);

            var old_cctor = oldType.FindOrCreateStaticConstructor();
            var cctor = newType.FindOrCreateStaticConstructor();

            old_cctor.Name = "Startup";
            old_cctor.IsRuntimeSpecialName = false;
            old_cctor.IsSpecialName = false;
            old_cctor.Access = MethodAttributes.Assembly;

            cctor.Body = new CilBody(true, new List<Instruction> {
                Instruction.Create(OpCodes.Jmp, old_cctor),
                Instruction.Create(OpCodes.Ret)
            }, new List<ExceptionHandler>(), new List<Local>());

            #region Import Runtime Entry Initialize
            ////////////////////////////////////////////////////////////
            VR.Runtime.RTMutator.ImportEntryInitialize(DFModule);
            ////////////////////////////////////////////////////////////
            #endregion

            for (int i = 0; i < oldType.Methods.Count; i++)
            {
                var nativeMethod = oldType.Methods[i];
                if (nativeMethod.IsNative)
                {
                    var methodStub = new MethodDefUser(nativeMethod.Name, nativeMethod.MethodSig.Clone());

                    methodStub.Attributes = MethodAttributes.Assembly | MethodAttributes.Static;
                    methodStub.Body = new CilBody();
                    methodStub.Body.Instructions.Add(new Instruction(OpCodes.Jmp, nativeMethod));
                    methodStub.Body.Instructions.Add(new Instruction(OpCodes.Ret));

                    newType.Methods[i] = methodStub;
                    newType.Methods.Add(nativeMethod);

                    refRepl[nativeMethod] = nativeMethod;
                }
            }

            Methods.Remove(cctor);
            Methods.Add(old_cctor);

            foreach (var entry in Methods)
                VR.AddMethod(entry);

            Utils.ExecuteModuleWriterOptions = new ModuleWriterOptions((ModuleDefMD)DFModule)
            {
                Logger = DummyLogger.NoThrowInstance,

                PdbOptions = PdbWriterOptions.None,
                WritePdb = false
            };

            //Utils.ExecuteModuleWriterOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;

            if (!string.IsNullOrEmpty(SNK_File))
            {
                if (File.Exists(SNK_File))
                {
                    StrongNameKey signatureKey = Utils.LoadSNKey(SNK_File, SNK_Password);
                    Utils.ExecuteModuleWriterOptions.InitializeStrongNameSigning(DFModule, signatureKey);
                }
            }

            // Fix methods
            VR.ResolveMethods();

            VR.JIT(DFModule, Utils.ExecuteModuleWriterOptions, out var jitCtx);

            Utils.ExecuteModuleWriterOptions.WriterEvent += delegate (object sender, ModuleWriterEventArgs e)
            {
                var _writer = (ModuleWriterBase)sender;

                if (e.Event == ModuleWriterEvent.MDBeginWriteMethodBodies)
                {
                    VR.ProcessMethods(_writer);

                    foreach (var repl in refRepl)
                        VR.Runtime.Descriptor.Data.ReplaceReference(repl.Key, repl.Value);

                    VR.CommitModule(_writer.Metadata);

                    #region Configure JIT
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////
                    foreach (var jitMethod in jitCtx.Targets)
                    {
                        JITContext.RealBodies.Add(jitMethod.Body);

                        jitMethod.Body = JITWriter.NopBody(_writer.Module);
                    }
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////
                    #endregion
                }
            };
        }
        
        public void GetProtectedFile(out byte[] jitedEXEC)
        {
            #region Extract Virted EXEC
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            MemoryStream output = new MemoryStream();
            DFModule.Write(output, Utils.ExecuteModuleWriterOptions);
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            jitedEXEC = output.ToArray();
        }

        public void SaveRuntime()
        {
            var rt = new MemoryStream();
            VR.Runtime.RTModule.Write(rt, VR.Runtime.RTModuleWriterOptions);

            #region Check New Runtime Name
            ////////////////////////////////////////////////
            if (Path.GetExtension(RTName) != ".dll")
                RTName += ".dll";
            ////////////////////////////////////////////////
            #endregion

            var WriteDirectory = Path.Combine(RT_OUT_Directory, RTName);

            if (File.Exists(WriteDirectory))
                File.Delete(WriteDirectory);

            File.WriteAllBytes(WriteDirectory, rt.ToArray());
        }

        public void Dispose()
        {
            VR.Clear();
            Methods.Clear();

            DFModule = null;
        }
    }
}