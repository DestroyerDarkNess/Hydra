using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;

using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.RT;
using EXGuard.Core.Helpers;
using EXGuard.Core.Services;

namespace EXGuard.Core.JIT
{
    public class JITWriter
    {
        JITContext ctx;
        ModuleDef module;
        JITDynamicDeriver deriver;

        public JITWriter(JITContext ctx, ModuleDef module)
        {
            this.ctx = ctx;
            this.module = module;
            this.deriver = new JITDynamicDeriver();

            JITContext.RealBodies = new List<CilBody>();
        }

        public void HandleRun(ModuleWriterOptions options)
        {
            if (ctx.Targets.Count != 0)
            {
                #region Write Mutation.Crypt (For Encrypted And Compressed JIT Data)
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ctx.Runtime.RTSearch.JITRuntime_Initialize.Body.SimplifyMacros(ctx.Runtime.RTSearch.JITRuntime_Initialize.Parameters);

                List<Instruction> instrs = ctx.Runtime.RTSearch.JITRuntime_Initialize.Body.Instructions.ToList();
                for (int i = 0; i < instrs.Count; i++)
                {
                    Instruction instr = instrs[i];
                    var method = instr.Operand as IMethod;
                    if (instr.OpCode == OpCodes.Call)
                    {
                        if (method.DeclaringType.Name == RTMap.Mutation &&
                            method.Name == RTMap.Mutation_Crypt)
                        {
                            Instruction ldBlock = instrs[i - 2];
                            Instruction ldKey = instrs[i - 1];

                            instrs.RemoveAt(i);
                            instrs.RemoveAt(i - 1);
                            instrs.RemoveAt(i - 2);

                            instrs.InsertRange(i - 2, deriver.EmitDecrypt(ctx.Runtime.RTSearch.JITRuntime_Initialize, ctx.Runtime, (Local)ldBlock.Operand, (Local)ldKey.Operand));
                        }
                    }
                }

                ctx.Runtime.RTSearch.JITRuntime_Initialize.Body.Instructions.Clear();

                foreach (Instruction instr in instrs)
                    ctx.Runtime.RTSearch.JITRuntime_Initialize.Body.Instructions.Add(instr);
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                #region HandleMD
                ///////////////////////////////////////////////
                options.WriterEvent += WriterEvent;
                ///////////////////////////////////////////////
                #endregion
            }
        }

        public static CilBody NopBody(ModuleDef module)
        {
            var body = new CilBody();

            var exceptionRef = module.CorLibTypes.GetTypeRef("System", "AccessViolationException");
            var objectCtor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), exceptionRef);

            body.Instructions.Add(OpCodes.Ldstr.ToInstruction("Attempted to read or write protected memory. This is often an indication that other memory is corrupt."));
            body.Instructions.Add(OpCodes.Newobj.ToInstruction(objectCtor));
            body.Instructions.Add(OpCodes.Throw.ToInstruction());

            return body;
        }

        private void WriterEvent(object sender, ModuleWriterEventArgs e)
        {
            var writer = (ModuleWriterBase)sender;
            if(e.Event == ModuleWriterEvent.MDEndWriteMethodBodies)
            {
                ReadOriginalMethodsBodies(writer);

                #region Write Impl '[MethodImpl(MethodImplOptions.NoInlining)]'
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                foreach (var Def in writer.Module.GetTypes())
                {
                    foreach (var Custom_MD in ctx.Targets)
                    {
                        if (Def.Methods.Contains(Custom_MD))
                        {
                            MDToken Token = writer.Metadata.GetToken(Custom_MD);
                            RawMethodRow methodRow = writer.Metadata.TablesHeap.MethodTable[Token.Rid];
                            writer.Metadata.TablesHeap.MethodTable[Token.Rid] = new RawMethodRow(
                                methodRow.RVA,
                                (ushort)(methodRow.ImplFlags | (ushort)MethodImplAttributes.NoInlining),
                                methodRow.Flags,
                                methodRow.Name,
                                methodRow.Signature,
                                methodRow.ParamList);
                        }
                    }
                }
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion
            }
            else if (e.Event == ModuleWriterEvent.BeginStrongNameSign)
            {
                WriteJITDataToRuntime();
            }
        }

        private void ReadOriginalMethodsBodies(ModuleWriterBase writer)
        {
            for (int i = 0; i < JITContext.RealBodies.Count; i++)
            {
                var method = ctx.Targets.ToArray()[i];

                var bodyWriter = new JITMethodBodyReader(writer.Metadata, JITContext.RealBodies[i], writer.Metadata.KeepOldMaxStack || method.Body.KeepOldMaxStack);
                bodyWriter.Read();

                ctx.Runtime.JITMethods.Add(new JITEDMethodInfo
                {
                    Method = ctx.Targets.ToArray()[i],
                    MethodToken = writer.Metadata.GetToken(method).ToInt32(),
                    ILCode = bodyWriter.ILCode,
                    ILCodeSize = bodyWriter.ILCodeSize,
                    MaxStack = bodyWriter.MaxStack
                });
            }
        }

        private void WriteJITDataToRuntime()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(ctx.Runtime.JITMethods.Count);

            foreach (var method in ctx.Runtime.JITMethods)
            {
                writer.Write(method.MethodToken);

                writer.Write(method.MaxStack);
                writer.Write(method.ILCodeSize);
                writer.Write(method.ILCode);
            }

            stream = new MemoryStream(deriver.Encrypt(stream.ToArray())); // Encryp and Compress JIT Data

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var JITDataType = new TypeDefUser(ctx.Runtime.RNMService.NewName(ctx.Runtime.Descriptor.RandomGenerator.NextString()),
                                              ctx.Runtime.RTModule.CorLibTypes.GetTypeRef("System", "ValueType"))
            {
                Layout = TypeAttributes.ExplicitLayout,
                Visibility = TypeAttributes.Sealed,
                IsSealed = true,

                ClassLayout = new ClassLayoutUser(0, (uint)stream.Length)
            };

            ctx.Runtime.RTModule.Types.Add(JITDataType);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var JITFieldWithRVA = new FieldDefUser(ctx.Runtime.RNMService.NewName(ctx.Runtime.Descriptor.RandomGenerator.NextString()),
                new FieldSig(JITDataType.ToTypeSig()), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.HasFieldRVA)
            {
                HasFieldRVA = true,
                InitialValue = stream.ToArray()
            };

            ctx.Runtime.RTSearch.JITRuntime.Fields.Add(JITFieldWithRVA);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
           
            MutationHelper.InjectKey_Int(ctx.Runtime.RTSearch.JITRuntime_Initialize, 0, (int)stream.Length / 4);
            MutationHelper.InjectKey_Int(ctx.Runtime.RTSearch.JITRuntime_Initialize, 1, deriver.Seed); // Write Seed

            MutationHelper.GetInstructionsLocationIndex(ctx.Runtime.RTSearch.JITRuntime_Initialize, true, out var index);

            ctx.Runtime.RTSearch.JITRuntime_Initialize.Body.Instructions.Insert(index, Instruction.Create(OpCodes.Ldtoken, JITFieldWithRVA));
            ctx.Runtime.RTSearch.JITRuntime_Initialize.Body.Instructions.Insert(index + 1,
                Instruction.Create(OpCodes.Call, ctx.Runtime.RTModule.Import(ctx.Runtime.RTSearch.FieldInfo_GetFieldFromHandle_1)));
            ctx.Runtime.RTSearch.JITRuntime_Initialize.Body.Instructions.Insert(index + 2,
                Instruction.Create(OpCodes.Callvirt, ctx.Runtime.RTModule.Import(ctx.Runtime.RTSearch.FieldInfo_get_FieldHandle)));
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}
