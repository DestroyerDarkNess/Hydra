using System;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using EXGuard.Core.Services;

using EXGuard.DynCipher;
using EXGuard.DynCipher.AST;
using EXGuard.DynCipher.Generation;
using EXGuard.Core.Helpers.System;

namespace EXGuard.Core.RT {
    public class ILVDynamicDeriver
    {
        public int Seed = 0;

        StatementBlock derivation;
        VMRuntime VMRT;
        Action<uint[], uint[]> encryptFunc;

        public void Initialize(VMRuntime runtime)
        {
            Seed = BitConverter.ToInt32(Encoding.Default.GetBytes(runtime.RTSearch.VMData_Ctor.Name), 0);

            new DynCipherService().GenerateCipherPair(runtime.Descriptor.RandomGenerator, out derivation, out _);

            var dmCodeGen = new DMCodeGen(typeof(void), new[] {
                Tuple.Create("{BUFFER}", typeof(uint[])),
                Tuple.Create("{KEY}", typeof(uint[]))
            });

            dmCodeGen.GenerateCIL(derivation);

            VMRT = runtime;
            encryptFunc = dmCodeGen.Compile<Action<uint[], uint[]>>();
        }

        public byte[] Encrypt(byte[] data, int offset)
        {
            data = (byte[])data.Clone();
            ulong state = (uint)Seed;

            var dst = new uint[0x10];
            var src = new uint[0x10];
            for (int i = 0; i < 0x10; i++)
            {
                state = (state * state) % 0x143fc089;
                src[i] = (uint)state;
                dst[i] = (uint)((state * state) % 0x444d56fb);
            }

            var key = new uint[src.Length];
            Buffer.BlockCopy(dst, offset * sizeof(uint), key, 0, src.Length * sizeof(uint));
            encryptFunc(key, src);

            for (int i = 0; i < 0x10; i++)
            {
                state ^= state >> 13;
                state ^= state << 25;
                state ^= state >> 27;

                src[i] = 0;
                dst[i] = 0;

                switch (i % 3)
                {
                    case 0:
                        key[i] = key[i] ^ (uint)state;
                        break;
                    case 1:
                        key[i] = key[i] * (uint)state;
                        break;
                    case 2:
                        key[i] = key[i] + (uint)state;
                        break;
                }
            }

            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= (byte)state;
                if ((i & 0xff) == 0)
                    state = (state * state) % 0x8a5cb7;
            }

            #region LZMA Compress
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            data = VMRT.CompressionService.LZMA_Compress(data);
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            #endregion

            Array.Resize(ref data, (data.Length + 3) & ~3);

            var encryptedData = new byte[data.Length];

            int keyIndex = 0;
            for (int i = 0; i < data.Length; i += 4)
            {
                var datum = (uint)(data[i + 0] | (data[i + 1] << 8) | (data[i + 2] << 16) | (data[i + 3] << 24));
                uint encrypted = datum ^ key[keyIndex & 0xf];

                key[keyIndex & 0xf] = (key[keyIndex & 0xf] ^ datum) + 0x3ddb2819;

                encryptedData[i + 0] = (byte)(encrypted >> 0);
                encryptedData[i + 1] = (byte)(encrypted >> 8);
                encryptedData[i + 2] = (byte)(encrypted >> 16);
                encryptedData[i + 3] = (byte)(encrypted >> 24);

                keyIndex++;
            }

            return encryptedData;
        }

        public IEnumerable<Instruction> EmitDecrypt(MethodDef method, Local dst, Local src)
        {
            var ret = new List<Instruction>();
            var codeGen = new CodeGen(dst, src, method, ret);
            codeGen.GenerateCIL(derivation);
            codeGen.Commit(method.Body);
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