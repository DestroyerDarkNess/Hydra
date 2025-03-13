using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.RT;
using EXGuard.Core.Helpers;
using EXGuard.Core.Services;
using EXGuard.Core.Helpers.System;

namespace EXGuard.Core.RTProtections.Constants
{
    public class EncodePhase
    {
        public CEContext context;

        public void Execute(CEContext ctx)
        {
            context = ctx;

            if (context == null)
                return;

            var ldc = new Dictionary<object, List<InstructionReference>>();

            // Extract constants
            ExtractConstants(context, ldc);

            // Encode constants
            context.ReferenceRepl = new Dictionary<MethodDef, List<ReplaceableInstructionReference>>();
            context.EncodedBuffer = new List<uint>();

            foreach (var entry in ldc)
            {
                if (entry.Key is string)
                {
                    EncodeString(context, (string)entry.Key, entry.Value);
                }
            }

            ReferenceReplacer.ReplaceReference(context);          

            // compress
            var encodedBuff = new byte[context.EncodedBuffer.Count * 4];

            int buffIndex = 0;
            foreach (uint dat in context.EncodedBuffer)//make bytes from uint.
            {
                encodedBuff[buffIndex++] = (byte)((dat >> 0) & 0xff);
                encodedBuff[buffIndex++] = (byte)((dat >> 8) & 0xff);
                encodedBuff[buffIndex++] = (byte)((dat >> 16) & 0xff);
                encodedBuff[buffIndex++] = (byte)((dat >> 24) & 0xff);
            }

            Debug.Assert(buffIndex == encodedBuff.Length);

            // compress data
            encodedBuff = context.Compressor.LZMA_Compress(encodedBuff);

            uint compressedLen = (uint)(encodedBuff.Length + 3) / 4;
            compressedLen = (compressedLen + 0xfu) & ~0xfu;

            var compressedBuff = new uint[compressedLen];
            Buffer.BlockCopy(encodedBuff, 0, compressedBuff, 0, encodedBuff.Length);

            Debug.Assert(compressedLen % 0x10 == 0);

            // encrypt
            uint seed = 123456;

            var key = new uint[0x10];
            uint state = seed;
            for (int i = 0; i < 0x10; i++)
            {
                state ^= state >> 12;
                state ^= state << 25;
                state ^= state >> 27;

                key[i] = state;
            }

            var encryptedBuffer = new byte[compressedBuff.Length * 4];

            buffIndex = 0;
            while (buffIndex < compressedBuff.Length)
            {
                uint[] enc = context.ModeHandler.Encrypt(compressedBuff, buffIndex, key);

                for (int j = 0; j < 0x10; j++)
                    key[j] ^= compressedBuff[buffIndex + j];

                Buffer.BlockCopy(enc, 0, encryptedBuffer, buffIndex * 4, 0x40);

                buffIndex += 0x10;
            }

            Debug.Assert(buffIndex == compressedBuff.Length);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            var CONSTDataType = new TypeDefUser(ctx.VMRuntime.RNMService.NewName(ctx.VMRuntime.Descriptor.RandomGenerator.NextString()),
                                              ctx.VMRuntime.RTModule.CorLibTypes.GetTypeRef("System", "ValueType"))
            {
                Layout = TypeAttributes.ExplicitLayout,
                Visibility = TypeAttributes.Sealed,
                IsSealed = true,

                ClassLayout = new ClassLayoutUser(0, (uint)encryptedBuffer.Length)
            };

            ctx.VMRuntime.RTModule.Types.Add(CONSTDataType);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var CONSTFieldWithRVA = new FieldDefUser(ctx.VMRuntime.RNMService.NewName(ctx.VMRuntime.Descriptor.RandomGenerator.NextString()),
                new FieldSig(CONSTDataType.ToTypeSig()), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.HasFieldRVA)
            {
                HasFieldRVA = true,
                InitialValue = encryptedBuffer
            };

            ctx.VMRuntime.RTSearch.RTConstantsProtection.Fields.Add(CONSTFieldWithRVA);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            MutationHelper.InjectKey_Int(context.InitMethod, 0, encryptedBuffer.Length / 4);
            MutationHelper.InjectKey_Int(context.InitMethod, 1, (int)seed); // Write Seed

            MutationHelper.GetInstructionsLocationIndex(context.InitMethod, true, out var index);

            context.InitMethod.Body.Instructions.Insert(index,     Instruction.Create(OpCodes.Ldtoken, CONSTFieldWithRVA));
            context.InitMethod.Body.Instructions.Insert(index + 1, Instruction.Create(OpCodes.Call, ctx.VMRuntime.RTModule.Import(ctx.VMRuntime.RTSearch.FieldInfo_GetFieldFromHandle_1)));
            context.InitMethod.Body.Instructions.Insert(index + 2, Instruction.Create(OpCodes.Callvirt, ctx.VMRuntime.RTModule.Import(ctx.VMRuntime.RTSearch.FieldInfo_get_FieldHandle)));
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        int EncodeByteArray(CEContext moduleCtx, byte[] buff)
        {
            int buffIndex = moduleCtx.EncodedBuffer.Count;
            moduleCtx.EncodedBuffer.Add((uint)buff.Length);

            // byte[] -> uint[]
            int integral = buff.Length / 4, remainder = buff.Length % 4;
            for (int i = 0; i < integral; i++)
            {
                var data = (uint)(buff[i * 4] | (buff[i * 4 + 1] << 8) | (buff[i * 4 + 2] << 16) | (buff[i * 4 + 3] << 24));
                moduleCtx.EncodedBuffer.Add(data);
            }

            if (remainder > 0)
            {
                int baseIndex = integral * 4;

                uint r = 0;
                for (int i = 0; i < remainder; i++)
                    r |= (uint)(buff[baseIndex + i] << (i * 8));

                moduleCtx.EncodedBuffer.Add(r);
            }

            return buffIndex;
        }

        void EncodeString(CEContext moduleCtx, string value, List<InstructionReference> references)
        {
            int buffIndex = EncodeByteArray(moduleCtx, Encoding.UTF8.GetBytes(value));
            UpdateReference(moduleCtx, references, buffIndex, desc => desc.StringID);
        }

        void UpdateReference(CEContext moduleCtx, List<InstructionReference> references, int buffIndex, Func<DecoderDesc, byte> typeID)
        {
            foreach (var instr in references)
            {
                int i = moduleCtx.Random.NextInt32(0, moduleCtx.Decoders.Count - 1);
                DecoderInfo decoderInfo = moduleCtx.Decoders[i];
                DecoderDesc desc = decoderInfo.DecoderDesc;

                uint id = (uint)buffIndex | (uint)(typeID(desc) << 30);
                id = moduleCtx.ModeHandler.Encode(desc.Data, moduleCtx, id);

                uint key = moduleCtx.Random.NextUInt32();
                id ^= key;

                moduleCtx.ReferenceRepl.AddListEntry(instr.Method, new ReplaceableInstructionReference
                {
                    Target = instr.Instruction,
                    Id = id,
                    Key = key,
                    Decoder = decoderInfo.Method
                });
            }
        }

        void ExtractConstants(CEContext context, Dictionary<object, List<InstructionReference>> ldc)
        {
            foreach (var def in context.Module.Types)
            {
                if(def != context.VMRuntime.RTSearch.RTConstantsProtection)
                {
                    foreach (MethodDef method in def.Methods)
                    {
                        if (!method.HasBody)
                            continue;

                        foreach (Instruction instr in method.Body.Instructions)
                        {
                            if (instr.OpCode != OpCodes.Ldstr)
                                continue;

                            string operand = (string)instr.Operand;
                            if (string.IsNullOrEmpty(operand))
                                continue;

                            ldc.AddListEntry(operand, new InstructionReference
                            {
                                Method = method,
                                Instruction = instr,
                            });
                        }
                    }
                }
            }
        }
    }
}
