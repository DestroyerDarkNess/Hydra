using VM.Runtime.Util;
using System;

namespace VM.Runtime.Handler.Impl
{
    public class Ldtoken : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var str = (string)instruction.Operand.GetObject();

            var prefix = Helper.ReadPrefix(str);
            var mdtoken = int.Parse(str.Substring(1));

            object result;
            switch (prefix)
            {
                case 0:
                    result = Helper.ResolveMethod(mdtoken).MethodHandle; // Method
                    break;
                case 1:
                    result = Helper.ResolveMember(mdtoken).MethodHandle; // MemberRef
                    break;
                case 2:
                    result = Helper.ResolveField(mdtoken).FieldHandle; // IField
                    break;
                case 3:
                    result = Helper.ResolveType(mdtoken).TypeHandle; // ITypeOrDef
                    break;
                default:
                    throw new InvalidOperationException("Invalid prefix value.");
            }

            vmContext.Stack.Push(result);


            vmContext.Index++;
        }
    }
}