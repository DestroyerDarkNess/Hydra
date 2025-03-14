using VM.Runtime.Handler;
using VM.Runtime.Handler.Impl;
using VM.Runtime.Handler.Impl.Custom;
using VM.Runtime.Util;
using System.Collections.Generic;

namespace VM.Runtime
{
    public class Context
    {
        public List<HxInstruction> Instructions { get; }
        public Dictionary<HxOpCodes, HxOpCode> Handlers;
        public VmStack Stack;
        public VmLocal Locals;
        public VmArgs Args;

        public int Index { get; set; }

        public Context(List<HxInstruction> instrs, object[] args)
        {
            Index = 0;
            Instructions = instrs;
            Stack = new VmStack();
            Locals = new VmLocal();
            Args = new VmArgs(args);

            Handlers = new Dictionary<HxOpCodes, HxOpCode>
            {
                // custom
                {HxOpCodes.HxCall, new HxCall()}, // universal call
                {HxOpCodes.HxLdc, new HxLdc()}, // universal ldc]
                {HxOpCodes.HxArray, new HxArray()}, // array stuff
                {HxOpCodes.HxLoc, new HxLoc()}, // stloc, ldloc
                {HxOpCodes.HxArg, new HxArg()}, // starg, ldarg
                {HxOpCodes.HxFld, new HxFld()}, // fld
                {HxOpCodes.HxConv, new HxConv()}, // conv

                // stuff where operand is null
                {HxOpCodes.HxClt, new HxClt()}, //  y<x
                //{HxOpCodes.ACgt, new Cgt()}, // y > x (first value compare first)

                {HxOpCodes.ANeg, new Neg()}, // -val
                {HxOpCodes.ANot, new Not()}, //  ˜val
                {HxOpCodes.AAnd, new And()}, // true and true

                {HxOpCodes.AShr, new Shr()}, // >>
                {HxOpCodes.AShl, new Shl()}, //  <<
                {HxOpCodes.AXor, new Xor()}, //  y^x

                {HxOpCodes.ARem, new Rem()}, // module  y % x
                {HxOpCodes.ACeq, new Ceq()}, // x == y
                {HxOpCodes.AMul, new Mul()}, // y * x
                {HxOpCodes.ANop, new Nop()}, //  nop do nothing

                {HxOpCodes.AAdd, new Add()}, // y+x
                {HxOpCodes.ASub, new Sub()}, // y-x
                {HxOpCodes.ARet, new Ret()}, // return  
                {HxOpCodes.APop, new Pop()}, // pop val on top of stack
                {HxOpCodes.ALdlen, new Ldlen()}, // length of array
                {HxOpCodes.ADup, new Dup()}, // dup on top of stack
                {HxOpCodes.ADiv, new Div()}, // divide

                // not custom & not null operand
                {HxOpCodes.Ldtoken, new Ldtoken()},
                {HxOpCodes.HxBr, new HxBr()},
                {HxOpCodes.Brtrue, new Brtrue()},
                {HxOpCodes.Brfalse, new Brfalse()},
                {HxOpCodes.Box, new Box()},
                {HxOpCodes.Newobj, new Newobj()},

                {HxOpCodes.Ldloca, new Ldloca()},
                {HxOpCodes.Ldftn, new Ldftn()},
                {HxOpCodes.Newarr, new Newarr()},
                {HxOpCodes.Castclass, new Castclass()},
                {HxOpCodes.AOr, new Or()},
                {HxOpCodes.Initobj, new Initobj()},
                {HxOpCodes.Constrained, new Constrained()},
                {HxOpCodes.HxCgt, new HxCgt()},
                {HxOpCodes.Endfinally, new Endfinally()},
                {HxOpCodes.HxBeq, new HxBeq()},
                {HxOpCodes.HxBge, new HxBge()},
            };
        }
        
        public Value Run()
        {
            do
            {
                var instruction = Instructions[Index];
                Handlers[instruction.OpCode].Execute(this, instruction);
            } while (Instructions.Count > Index);
            return Stack.Pop();
        }
    }
}