﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace HydraEngine.Protection.CodeEncryption.Stuffs
{
    internal class NormalDeriver : IKeyDeriver
    {
        public void Init() { }
        public uint[] DeriveKey(uint[] a, uint[] b)
        {
            var ret = new uint[0x10];
            for (int i = 0; i < 0x10; i++)
            {
                switch (i % 3)
                {
                    case 0:
                        ret[i] = a[i] ^ b[i];
                        break;
                    case 1:
                        ret[i] = a[i] * b[i];
                        break;
                    case 2:
                        ret[i] = a[i] + b[i];
                        break;
                }
            }
            return ret;
        }
        public IEnumerable<Instruction> EmitDerivation(MethodDef method, Local dst, Local src)
        {
            for (int i = 0; i < 0x10; i++)
            {
                yield return Instruction.Create(OpCodes.Ldloc, dst);
                yield return Instruction.Create(OpCodes.Ldc_I4, i);
                yield return Instruction.Create(OpCodes.Ldloc, dst);
                yield return Instruction.Create(OpCodes.Ldc_I4, i);
                yield return Instruction.Create(OpCodes.Ldelem_U4);
                yield return Instruction.Create(OpCodes.Ldloc, src);
                yield return Instruction.Create(OpCodes.Ldc_I4, i);
                yield return Instruction.Create(OpCodes.Ldelem_U4);
                switch (i % 3)
                {
                    case 0:
                        yield return Instruction.Create(OpCodes.Xor);
                        break;
                    case 1:
                        yield return Instruction.Create(OpCodes.Mul);
                        break;
                    case 2:
                        yield return Instruction.Create(OpCodes.Add);
                        break;
                }
                yield return Instruction.Create(OpCodes.Stelem_I4);
            }
        }
    }
}