using VM.Runtime.Util;
using System;

namespace VM.Runtime.Handler.Impl.Custom
{
    public class HxConv : HxOpCode
    {
        public override void Execute(Context vmContext, HxInstruction instruction)
        {
            var id = (int)instruction.Operand.GetObject();
            object item = vmContext.Stack.Pop().GetObject();

            object result;
            switch (id)
            {
                case 0:
                    result = (object)Convert.ToSingle(item); // convert to "float". Conv_R4
                    break;
                case 1:
                    result = (object)Convert.ToDouble(item); // convert to "double". Conv_R8
                    break;
                case 2:
                    result = (object)Convert.ToInt32(item); // convert to "int32". Conv_I4
                    break;
                case 3:
                    result = (object)Convert.ToInt64(item); // convert to "int64". Conv_I8
                    break;
                case 4:
                    result = (object)((Int32)Convert.ToByte(item)); // convert to "unsigned int8" then extends to "int32". Conv_U1
                    break;
                case 5:
                    result = (object)((Int64)Convert.ToUInt64(item)); // convert to "unsigned int64" then extends to "int64". Conv_U8
                    break;
                case 6:
                    result = (object)((Int32)Convert.ToUInt16(item)); // convert to "unsigned int16", then extends to "int32". Conv_U2
                    break;
                case 7:
                    result = (object)((Int32)Convert.ToUInt32(item)); // converts to "unsigned int32", then extends to "int32". Conv_U4
                    break;
                default:
                    result = item; // Should not happen, otherwise I or YOU messed up in the converter.. And yes this is a useless comment.
                    break;
            }

            vmContext.Stack.Push(result);

            vmContext.Index++;
        }
    }
}