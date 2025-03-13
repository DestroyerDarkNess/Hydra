using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using EXGuard.Runtime.Dynamic;
using EXGuard.Runtime.Execution;

namespace EXGuard.Runtime {
	internal static unsafe class Utils {
        public static Constants ReadConstants(BinaryReader reader)
        {
            var _const = new Constants();

            _const.REG_R0 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_R1 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_R2 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_R3 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_R4 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_R5 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_R6 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_R7 = (byte)Decrypt(reader.ReadInt32());
            _const.REG_BP = (byte)Decrypt(reader.ReadInt32());
            _const.REG_SP = (byte)Decrypt(reader.ReadInt32());
            _const.REG_IP = (byte)Decrypt(reader.ReadInt32());
            _const.REG_FL = (byte)Decrypt(reader.ReadInt32());
            _const.REG_K1 = (byte)Decrypt(reader.ReadInt32());
            //_const.REG_K2 = (byte)Decrypt(reader.ReadInt32());
            //_const.REG_M1 = (byte)Decrypt(reader.ReadInt32());
            //_const.REG_M2 = (byte)Decrypt(reader.ReadInt32());

            _const.FL_OVERFLOW = (byte)Decrypt(reader.ReadInt32());
            _const.FL_CARRY = (byte)Decrypt(reader.ReadInt32());
            _const.FL_ZERO = (byte)Decrypt(reader.ReadInt32());
            _const.FL_SIGN = (byte)Decrypt(reader.ReadInt32());
            _const.FL_UNSIGNED = (byte)Decrypt(reader.ReadInt32());
            //_const.FL_BEHAV1 = (byte)Decrypt(reader.ReadInt32());
            //_const.FL_BEHAV2 = (byte)Decrypt(reader.ReadInt32());
            //_const.FL_BEHAV3 = (byte)Decrypt(reader.ReadInt32());

            _const.OP_NOP = (byte)Decrypt(reader.ReadInt32());
            _const.OP_LIND_PTR = (byte)Decrypt(reader.ReadInt32());
            _const.OP_LIND_OBJECT = (byte)Decrypt(reader.ReadInt32());
            _const.OP_LIND_BYTE = (byte)Decrypt(reader.ReadInt32());
            _const.OP_LIND_WORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_LIND_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_LIND_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SIND_PTR = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SIND_OBJECT = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SIND_BYTE = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SIND_WORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SIND_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SIND_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_POP = (byte)Decrypt(reader.ReadInt32());
            _const.OP_PUSHR_OBJECT = (byte)Decrypt(reader.ReadInt32());
            _const.OP_PUSHR_BYTE = (byte)Decrypt(reader.ReadInt32());
            _const.OP_PUSHR_WORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_PUSHR_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_PUSHR_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_PUSHI_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_PUSHI_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SX_BYTE = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SX_WORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SX_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_CALL = (byte)Decrypt(reader.ReadInt32());
            _const.OP_RET = (byte)Decrypt(reader.ReadInt32());
            _const.OP_NOR_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_NOR_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_CMP = (byte)Decrypt(reader.ReadInt32());
            _const.OP_CMP_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_CMP_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_CMP_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_CMP_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_JZ = (byte)Decrypt(reader.ReadInt32());
            _const.OP_JNZ = (byte)Decrypt(reader.ReadInt32());
            _const.OP_JMP = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SWT = (byte)Decrypt(reader.ReadInt32());
            _const.OP_ADD_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_ADD_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_ADD_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_ADD_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SUB_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SUB_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_MUL_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_MUL_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_MUL_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_MUL_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_DIV_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_DIV_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_DIV_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_DIV_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_REM_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_REM_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_REM_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_REM_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SHR_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SHR_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SHL_DWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_SHL_QWORD = (byte)Decrypt(reader.ReadInt32());
            _const.OP_FCONV_R32_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_FCONV_R64_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_FCONV_R32 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_FCONV_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_ICONV_PTR = (byte)Decrypt(reader.ReadInt32());
            _const.OP_ICONV_R64 = (byte)Decrypt(reader.ReadInt32());
            _const.OP_VCALL = (byte)Decrypt(reader.ReadInt32());
            _const.OP_TRY = (byte)Decrypt(reader.ReadInt32());
            _const.OP_LEAVE = (byte)Decrypt(reader.ReadInt32());

            _const.VCALL_EXIT = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_BREAK = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_ECALL = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_CAST = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_CKFINITE = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_CKOVERFLOW = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_RANGECHK = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_INITOBJ = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_LDFLD = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_LDFTN = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_TOKEN = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_THROW = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_SIZEOF = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_STFLD = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_BOX = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_UNBOX = (byte)Decrypt(reader.ReadInt32());
            _const.VCALL_LOCALLOC = (byte)Decrypt(reader.ReadInt32());

            _const.ECALL_CALL = (byte)Decrypt(reader.ReadInt32());
            _const.ECALL_CALLVIRT = (byte)Decrypt(reader.ReadInt32());
            _const.ECALL_NEWOBJ = (byte)Decrypt(reader.ReadInt32());
            _const.ECALL_CALLVIRT_CONSTRAINED = (byte)Decrypt(reader.ReadInt32());

            //_const.FLAG_INSTANCE = (byte)Decrypt(reader.ReadInt32());

            _const.EH_CATCH = (byte)Decrypt(reader.ReadInt32());
            _const.EH_FILTER = (byte)Decrypt(reader.ReadInt32());
            _const.EH_FAULT = (byte)Decrypt(reader.ReadInt32());
            _const.EH_FINALLY = (byte)Decrypt(reader.ReadInt32());

            return _const;
        }

        public static unsafe int Decrypt(int input, double Key = 0)
        {
            byte[] KEY = new byte[8];

            if (Key == 0)
            {
                KEY[0] = (byte)Mutation.IntKey0;
                KEY[1] = (byte)Mutation.IntKey1;
                KEY[2] = (byte)Mutation.IntKey2;
                KEY[3] = (byte)Mutation.IntKey3;
                KEY[4] = (byte)Mutation.IntKey4;
                KEY[5] = (byte)Mutation.IntKey5;
                KEY[6] = (byte)Mutation.IntKey6;
                KEY[7] = (byte)Mutation.IntKey7;
            }
            else
                KEY = BitConverter.GetBytes(Key);

            int IV0 = 0;
            int IV1 = 0;
            int IV2 = 0;
            int IV3 = 0;
            int[] IVS = new int[8];

            for (int a = 0; a < 8; a++)
                IVS[a] = KEY[a] % (a + 1) ^ (int)input ^ (a + 1) ^ (int)input;

            for (int i = 0; i < 8; i++)
            {
                int X = (int)Math.Log10(IVS[i]);

                IV0 ^= IVS[i] ^ (((IVS[i] ^ (int)X)) * i >> (int)((float)i * (float)0.25F));
                IV1 += IVS[i] >> (((IVS[i] ^ (int)X)) * i << (int)((short)i + (float)0.58F));
                IV2 -= IVS[i] << (((IVS[i] ^ (int)X)) * i >> (int)((float)i * (float)0.41F));
                IV3 ^= IVS[i] + (((IVS[i] ^ (int)X)) * i << (int)((float)i - (float)0.99F));
            }

            return (IV0 ^ input) ^ (IV1 ^ input) ^ (IV2 ^ input) ^ IV3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type type)
        {
            return (Delegate)typeof(Marshal).GetMethod("GetDelegateForFunctionPointer", new Type[]
            {
                typeof(IntPtr),
                typeof(Type)
            }).Invoke(null, new object[] { ptr, type });
        }

        public static void UpdateFL(VMContext ctx, ulong op1, ulong op2, ulong flResult, ulong result, ref byte fl, byte mask) {
			const ulong SignMask = (1U << 63);
			byte flag = 0;

			if (result == 0)
				flag |= ctx.Data.Constants.FL_ZERO;

			if ((result & SignMask) != 0)
				flag |= ctx.Data.Constants.FL_SIGN;

			if (((op1 ^ flResult) & (op2 ^ flResult) & SignMask) != 0)
				flag |= ctx.Data.Constants.FL_OVERFLOW;

			if (((op1 ^ (op1 ^ op2) & (op2 ^ flResult)) & SignMask) != 0)
				flag |= ctx.Data.Constants.FL_CARRY;

			fl = (byte)((fl & ~mask) | (flag & mask));
		}
    }
}