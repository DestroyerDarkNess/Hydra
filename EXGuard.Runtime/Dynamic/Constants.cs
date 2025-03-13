#pragma warning disable 0649

namespace EXGuard.Runtime.Dynamic
{
    internal class Constants
    {
        public byte REG_R0;
        public byte REG_R1;
        public byte REG_R2;
        public byte REG_R3;
        public byte REG_R4;
        public byte REG_R5;
        public byte REG_R6;
        public byte REG_R7;
        public byte REG_BP;
        public byte REG_SP;
        public byte REG_IP;
        public byte REG_FL;
        public byte REG_K1;
        //public byte REG_K2;
        //public byte REG_M1;
        //public byte REG_M2;

        public byte FL_OVERFLOW;
        public byte FL_CARRY;
        public byte FL_ZERO;
        public byte FL_SIGN;
        public byte FL_UNSIGNED;
        //public byte FL_BEHAV1;
        //public byte FL_BEHAV2;
        //public byte FL_BEHAV3;

        public byte OP_NOP;
        public byte OP_LIND_PTR;
        public byte OP_LIND_OBJECT;
        public byte OP_LIND_BYTE;
        public byte OP_LIND_WORD;
        public byte OP_LIND_DWORD;
        public byte OP_LIND_QWORD;
        public byte OP_SIND_PTR;
        public byte OP_SIND_OBJECT;
        public byte OP_SIND_BYTE;
        public byte OP_SIND_WORD;
        public byte OP_SIND_DWORD;
        public byte OP_SIND_QWORD;
        public byte OP_POP;
        public byte OP_PUSHR_OBJECT;
        public byte OP_PUSHR_BYTE;
        public byte OP_PUSHR_WORD;
        public byte OP_PUSHR_DWORD;
        public byte OP_PUSHR_QWORD;
        public byte OP_PUSHI_DWORD;
        public byte OP_PUSHI_QWORD;
        public byte OP_SX_BYTE;
        public byte OP_SX_WORD;
        public byte OP_SX_DWORD;
        public byte OP_CALL;
        public byte OP_RET;
        public byte OP_NOR_DWORD;
        public byte OP_NOR_QWORD;
        public byte OP_CMP;
        public byte OP_CMP_DWORD;
        public byte OP_CMP_QWORD;
        public byte OP_CMP_R32;
        public byte OP_CMP_R64;
        public byte OP_JZ;
        public byte OP_JNZ;
        public byte OP_JMP;
        public byte OP_SWT;
        public byte OP_ADD_DWORD;
        public byte OP_ADD_QWORD;
        public byte OP_ADD_R32;
        public byte OP_ADD_R64;
        public byte OP_SUB_R32;
        public byte OP_SUB_R64;
        public byte OP_MUL_DWORD;
        public byte OP_MUL_QWORD;
        public byte OP_MUL_R32;
        public byte OP_MUL_R64;
        public byte OP_DIV_DWORD;
        public byte OP_DIV_QWORD;
        public byte OP_DIV_R32;
        public byte OP_DIV_R64;
        public byte OP_REM_DWORD;
        public byte OP_REM_QWORD;
        public byte OP_REM_R32;
        public byte OP_REM_R64;
        public byte OP_SHR_DWORD;
        public byte OP_SHR_QWORD;
        public byte OP_SHL_DWORD;
        public byte OP_SHL_QWORD;
        public byte OP_FCONV_R32_R64;
        public byte OP_FCONV_R64_R32;
        public byte OP_FCONV_R32;
        public byte OP_FCONV_R64;
        public byte OP_ICONV_PTR;
        public byte OP_ICONV_R64;
        public byte OP_VCALL;
        public byte OP_TRY;
        public byte OP_LEAVE;

        public byte VCALL_EXIT;
        public byte VCALL_BREAK;
        public byte VCALL_ECALL;
        public byte VCALL_CAST;
        public byte VCALL_CKFINITE;
        public byte VCALL_CKOVERFLOW;
        public byte VCALL_RANGECHK;
        public byte VCALL_INITOBJ;
        public byte VCALL_LDFLD;
        public byte VCALL_LDFTN;
        public byte VCALL_TOKEN;
        public byte VCALL_THROW;
        public byte VCALL_SIZEOF;
        public byte VCALL_STFLD;
        public byte VCALL_BOX;
        public byte VCALL_UNBOX;
        public byte VCALL_LOCALLOC;

        public byte ECALL_CALL;
        public byte ECALL_CALLVIRT;
        public byte ECALL_NEWOBJ;
        public byte ECALL_CALLVIRT_CONSTRAINED;

        //public byte FLAG_INSTANCE;

        public byte EH_CATCH;
        public byte EH_FILTER;
        public byte EH_FAULT;
        public byte EH_FINALLY;
    }
}