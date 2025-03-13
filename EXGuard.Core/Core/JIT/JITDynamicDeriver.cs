using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.RT;
using EXGuard.DynCipher;
using EXGuard.DynCipher.AST;
using EXGuard.Core.Helpers.System;
using EXGuard.DynCipher.Generation;

namespace EXGuard.Core.JIT {
    public class JITDynamicDeriver
    {
        public int Seed = 0;

        VMRuntime VMRT;
        Action<uint[], uint[]> encryptFunc;

        public IEnumerable<Instruction> EmitDecrypt(MethodDef init, VMRuntime runtime, Local block, Local key)
        {
            Seed = BitConverter.ToInt32(Encoding.Default.GetBytes(runtime.RTSearch.JITRuntime_Initialize.Name), 0);

            StatementBlock encrypt, decrypt;
            new DynCipherService().GenerateCipherPair(runtime.Descriptor.RandomGenerator, out encrypt, out decrypt);
            var ret = new List<Instruction>();

            var codeGen = new CodeGen(block, key, init, ret);
            codeGen.GenerateCIL(decrypt);
            codeGen.Commit(init.Body);

            var dmCodeGen = new DMCodeGen(typeof(void), new[] {
                Tuple.Create("{BUFFER}", typeof(uint[])),
                Tuple.Create("{KEY}", typeof(uint[]))
            });

            dmCodeGen.GenerateCIL(encrypt);

            VMRT = runtime;
            encryptFunc = dmCodeGen.Compile<Action<uint[], uint[]>>();

            return ret;
        }

        public byte[] Encrypt(byte[] data)
        {
            var moduleBuff = VMRT.CompressionService.LZMA_Compress(data);

            uint compressedLen = (uint)(moduleBuff.Length + 3) / 4;
            compressedLen = (compressedLen + 0xfu) & ~0xfu;
           
            var compressedBuff = new uint[compressedLen];
            Buffer.BlockCopy(moduleBuff, 0, compressedBuff, 0, moduleBuff.Length);
            
            Debug.Assert(compressedLen % 0x10 == 0);
            
            uint state = (uint)Seed;

            var key = new uint[0x10];
            for (int i = 0; i < 0x10; i++)
            {
                state ^= state >> 13;
                state ^= state << 25;
                state ^= state >> 27;

                key[i] = state;
            }

            var encryptedBuffer = new byte[compressedBuff.Length * 4];

            int buffIndex = 0;
            while (buffIndex < compressedBuff.Length)
            {
                uint[] enc = Encrypt(compressedBuff, buffIndex, key);

                for (int j = 0; j < 0x10; j++)
                    key[j] ^= compressedBuff[buffIndex + j];

                Buffer.BlockCopy(enc, 0, encryptedBuffer, buffIndex * 4, 0x40);

                buffIndex += 0x10;
            }

            Debug.Assert(buffIndex == compressedBuff.Length);

            return encryptedBuffer;
        }

        public uint[] Encrypt(uint[] data, int offset, uint[] key)
        {
            var ret = new uint[key.Length];
            Buffer.BlockCopy(data, offset * sizeof(uint), ret, 0, key.Length * sizeof(uint));
            encryptFunc(ret, key);

            return ret;
        }

        class CodeGen : CILCodeGen
        {
            readonly Local block;
            readonly Local key;

            public CodeGen(Local block, Local key, MethodDef method, IList<Instruction> instrs)
             : base(method, instrs)
            {
                this.block = block;
                this.key = key;
            }

            protected override Local Var(Variable var)
            {
                if (var.Name == "{BUFFER}")
                    return block;
                if (var.Name == "{KEY}")
                    return key;
                return base.Var(var);
            }
        }
    }
}