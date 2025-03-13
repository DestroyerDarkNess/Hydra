using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.RT;
using EXGuard.DynCipher;
using EXGuard.Core.Services;

using EXGuard.Core.Helpers;
using EXGuard.Core.Helpers.System;

namespace EXGuard.Core.RTProtections.Constants
{
    public class ConstantsProtection
    {
        private static CEContext context;

        public void Execute(ModuleDef module, VMRuntime runtime)
        {
            context = new CEContext();
            context.Random = new RandomGenerator(32);
            context.DynCipher = new DynCipherService();
            context.VMRuntime = runtime;
            context.Module = module;
            context.Options = runtime.RTModuleWriterOptions;

            context.DecoderCount = 1;
            context.ModeHandler = new DynamicMode();
            context.Compressor = runtime.CompressionService;
 
            InjectHelpers(context);
            MutateInitializer(context);

            new EncodePhase().Execute(context);

            context.ReferenceRepl.Clear();
            context.EncodedBuffer.Clear();

            context.ReferenceRepl = new Dictionary<MethodDef, List<ReplaceableInstructionReference>>();
            context.EncodedBuffer = new List<uint>();
        }

        private void InjectHelpers(CEContext moduleCtx)
        {
            moduleCtx.InitMethod = context.VMRuntime.RTSearch.RTConstantsProtection_Initialize;
            moduleCtx.Decoders = new List<DecoderInfo>();

            for (int i = 0; i < moduleCtx.DecoderCount; i++)
            {
                MethodDef decoderInst = context.VMRuntime.RTSearch.RTConstantsProtection_Get;
                for (int j = 0; j < decoderInst.Body.Instructions.Count; j++)
                {
                    var instr = decoderInst.Body.Instructions[j];
                    var method = instr.Operand as IMethod;

                    if (instr.OpCode == OpCodes.Call &&
                        method.DeclaringType.Name == RTMap.Mutation &&
                        method.Name == RTMap.Mutation_Value_T)
                    {
                        decoderInst.Body.Instructions[j] = Instruction.Create(OpCodes.Sizeof, new GenericMVar(0).ToTypeDefOrRef());
                    }
                }

                var decoderDesc = new DecoderDesc();

                decoderDesc.StringID = (byte)(moduleCtx.Random.NextByte() & 3);

                do decoderDesc.NumberID = (byte)(moduleCtx.Random.NextByte() & 3); while (decoderDesc.NumberID == decoderDesc.StringID);

                do decoderDesc.InitializerID = (byte)(moduleCtx.Random.NextByte() & 3); while (decoderDesc.InitializerID == decoderDesc.StringID || decoderDesc.InitializerID == decoderDesc.NumberID);

                MutationHelper.InjectKeys_Int(decoderInst,
                                          new[] { 0, 1, 2 },
                                          new int[] { decoderDesc.StringID, decoderDesc.NumberID, decoderDesc.InitializerID });

                decoderDesc.Data = moduleCtx.ModeHandler.CreateDecoder(decoderInst, moduleCtx);

                moduleCtx.Decoders.Add(new DecoderInfo
                {
                    Method = decoderInst,
                    DecoderDesc = decoderDesc
                });
            }
        }

        private void MutateInitializer(CEContext moduleCtx)
        {
            moduleCtx.InitMethod.Body.SimplifyMacros(moduleCtx.InitMethod.Parameters);

            List<Instruction> instrs = moduleCtx.InitMethod.Body.Instructions.ToList();
            for (int i = 0; i < instrs.Count; i++)
            {
                Instruction instr = instrs[i];
                var method = instr.Operand as IMethod;

                /* Mutate initializer */
                if (instr.OpCode == OpCodes.Call)
                {
                    if (method.DeclaringType.Name == RTMap.Mutation &&
                        method.Name == RTMap.Mutation_Crypt)
                    {
                        Instruction ldBlock = instrs[i - 2];
                        Instruction ldKey = instrs[i - 1];
                        
                        Debug.Assert(ldBlock.OpCode == OpCodes.Ldloc && ldKey.OpCode == OpCodes.Ldloc);

                        instrs.RemoveAt(i);
                        instrs.RemoveAt(i - 1);
                        instrs.RemoveAt(i - 2);

                        instrs.InsertRange(i - 2, moduleCtx.ModeHandler.EmitDecrypt(moduleCtx.InitMethod, moduleCtx, (Local)ldBlock.Operand, (Local)ldKey.Operand)); //This is mutator
                    }
                }
            }

            moduleCtx.InitMethod.Body.Instructions.Clear();

            foreach (Instruction instr in instrs)
                moduleCtx.InitMethod.Body.Instructions.Add(instr);
        }
    }
}
