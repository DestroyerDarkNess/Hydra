namespace VM.Runtime.Handler
{
    public enum HxOpCodes
    {
        // custom
        HxCall, // universal call
        HxLdc, // universal ldc]
        HxArray, // array stuff
        HxLoc,
        HxArg,
        HxFld,
        HxConv,
        HxBeq,
        HxBge,
        
        // stuff where operand is null
        //AClt, //  y<x
        ANeg, // -val
        ANot, //  ˜val
        AAnd, // true and true
        AShr,  // >>
        AShl, //  <<
        ARem, // module  y % x
        ARem_Un, // module  y % x
        ACeq, // x == y
        AMul,  // y * x
        AMul_Ovf,  // checked(y * x)
        AMul_Ovf_Un,  // checked(y * x)
        ANop, //  nop do nothing
        //ACgt, // y > x (first value compare first)
        AAdd, // y+x
        AAdd_Ovf, // checked(y+x)
        AAdd_Ovf_Un, // checked(y+x)
        ASub,  // y-x
        ASub_Ovf,  // checked(y-x)
        ASub_Ovf_Un,  // checked(y-x)
        ARet ,// return  
        AXor, // y^x
        APop, // pop value on top of stack
        ALdlen, // get len of array
        ADup, // dup on top of stack
        ADiv, // y / x
        ADiv_Un, // y / x
            
        // not custom & not null
        Ldtoken, // ldtoken
        Brfalse, // if <pop> == 0, transfer
        Brtrue, // if <pop> == 1, transfer
        //Br, // unconditional transfer
        Box,
        Newobj,

        Ldloca,
        Ldftn,
        Newarr,
        Castclass,
        Initobj,
        Constrained,
        AOr,
        HxCgt,
        HxClt,
        HxBr,
        Endfinally
    }
}