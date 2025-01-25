using dnlib.DotNet;
using dnlib.DotNet.Emit;
using VM.Core.Helper;
using System;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Data;
using VM.Runtime.Handler.Impl;
using dnlib.DotNet.Writer;
using HydraEngine.Protection.VM;

namespace VM.Core.Protections.Impl.UStrings
{
    public class VStrings : IProtection
    {
        private static MethodDef StringRecover = null;

        public static string Process(string str, int MDToken, int MaxStack)
        {
            byte[] data = Encoding.UTF8.GetBytes(str);

            /*for (var i = 0; i < data.Length; i++)
                data[i] = (byte)((byte)((BitConverter.GetBytes(MDToken ^ MaxStack)[i % sizeof(int)] ^ (data[i]))));*/

            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                dstream.Write(data, 0, data.Length);

            return string.Join("|", output.ToArray().Select(b => b.ToString()));
        }

        private static int Processed = 0;

        public override string Name() => "Strings";
        public override void Execute(Virtualizer context)
        {
            #region "Inject Runtime"
            var Self = ModuleDefMD.Load(typeof(VStrings).Module);

            TypeDef StrType = Self.ResolveTypeDef(MDToken.ToRID(typeof(global::Runtime).MetadataToken));

            StringRecover = (MethodDef)InjectHelper.Inject(StrType, context.Module.GlobalType, context.Module).First(M => M.Name == "Recover");

            StringRecover.Name = Generator.RandomName();
            #endregion

            #region "Process Module"
            foreach (var Type in context.Module.Types.Where(t => t.Methods.Count > 0).ToArray())
            {
                if (!Analyzer.CanRename(Type)) continue;
                foreach (var Method in Type.Methods.Where(M => M.HasBody && M.Body.HasInstructions && M.Body.Instructions.Count() > 1).ToArray())
                {
                    if (!Analyzer.CanRename(Method)) continue;
                    Instruction[] Instructions = Method.Body.Instructions.ToArray();
                    for (int i = 0; i < Instructions.Count(); i++)
                    {
                        Instruction Instruction = Instructions[i];
                        if (Instruction.OpCode != OpCodes.Ldstr || Instruction.Operand == null)
                            continue;

                        if (Instruction.Operand.ToString().Length > 1)
                        {
                            Instruction.Operand = Process(Instruction.Operand.ToString(), Method.MDToken.ToInt32(), Method.Body.MaxStack);

                            Method.Body.Instructions.Insert(Method.Body.Instructions.IndexOf(Instruction) + 1, new Instruction(OpCodes.Call, context.Module.Import(StringRecover)));

                            Processed++;
                            continue;
                        }

                    }

                    Method.Body.Instructions.OptimizeMacros();
                    Method.Body.Instructions.SimplifyBranches();
                }
            }
            #endregion

            //context.Log.Information($"{Processed} Virtualized Strings");

            MemoryStream MS = new MemoryStream();
            ModuleWriterOptions ModuleWriterOptions = new ModuleWriterOptions(context.Module) { Logger = DummyLogger.NoThrowInstance };
            ModuleWriterOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;
            context.Module.Write(MS, ModuleWriterOptions);
            context.Module = ModuleDefMD.Load(MS);
            MS.Dispose();
        }
    }
}