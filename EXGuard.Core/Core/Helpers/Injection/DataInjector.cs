using System;
using System.Linq;
using System.Text;

using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace EXGuard.Core.Helpers.Injection
{
    public static class DataInjector
    {
        public static void InjectByteArr(byte[] bytes, MethodDef target, FieldDef field, int i = 0)
        {
            var module = target.Module;
            var instructions = target.Body.Instructions;
            var count = 2;

            instructions.Insert(i, OpCodes.Ldc_I4.ToInstruction(bytes.Length));
            instructions.Insert(i + 1, OpCodes.Newarr.ToInstruction(module.CorLibTypes.Byte));
            instructions.Insert(i + 2, OpCodes.Dup.ToInstruction());

            for (int j =0; j < bytes.Length; j++)
            {
                var value = Convert.ToInt32(bytes[j]);
                instructions.Insert(i + ++count, OpCodes.Ldc_I4.ToInstruction(j));
                instructions.Insert(i + ++count, OpCodes.Ldc_I4.ToInstruction(value));
                instructions.Insert(i + ++count, OpCodes.Stelem_I1.ToInstruction());
                instructions.Insert(i + ++count, OpCodes.Dup.ToInstruction());
            }

            instructions.Insert(i + ++count, OpCodes.Pop.ToInstruction());
            instructions.Insert(i + count, OpCodes.Stsfld.ToInstruction(field));

        }
    }
}
