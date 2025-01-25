using System;
using System.Collections.Generic;
using System.Linq;

namespace VM.Runtime.Util
{
    public class VmStack
    {
        public Stack<Value> Stack { get; }
        
        public int Count => Stack.Count;

        public VmStack() => Stack = new Stack<Value>();

        public void Push(object obj) => Stack.Push(new Value(obj));
        public void Push(Value value) => Stack.Push(value);
        public Value Get(int i) => Stack.ToList()[i];
        public Value Peek() => Stack.Peek();
        public Value Pop() => Stack.Pop();

        public object Cast(Value value, Type castType)
        {
            object valueObject = value.GetObject();
            if (valueObject is null || castType is null) return null;

            Dictionary<TypeCode, Func<object, object>> conversionMap = new Dictionary<TypeCode, Func<object, object>>
            {
                { TypeCode.Empty, obj => obj },
                { TypeCode.Object, obj => obj },
                { TypeCode.DBNull, obj => DBNull.Value },
                { TypeCode.Boolean, obj => obj is int I32B ? Convert.ToBoolean(I32B) : Convert.ToBoolean(obj) },
                { TypeCode.Char, obj => Convert.ToChar(obj) },
                { TypeCode.SByte, obj => Convert.ToSByte(obj) },
                { TypeCode.Byte, obj => Convert.ToByte(obj) },
                { TypeCode.Int16, obj => Convert.ToInt16(obj) },
                { TypeCode.UInt16, obj => Convert.ToUInt16(obj) },
                { TypeCode.Int32, obj => Convert.ToInt32(obj) },
                { TypeCode.UInt32, obj => Convert.ToUInt32(obj) },
                { TypeCode.Int64, obj => Convert.ToInt64(obj) },
                { TypeCode.UInt64, obj => Convert.ToUInt64(obj) },
                { TypeCode.Single, obj => Convert.ToSingle(obj) },
                { TypeCode.Double, obj => Convert.ToDouble(obj) },
                { TypeCode.Decimal, obj => Convert.ToDecimal(obj) },
                { TypeCode.DateTime, obj => Convert.ToDateTime(obj) },
                { TypeCode.String, obj => Convert.ToString(obj) },
            };

            if (conversionMap.TryGetValue(Type.GetTypeCode(castType ?? typeof(object)), out var conversion))
                return conversion(valueObject);

            try
            {
                return Convert.ChangeType(valueObject, castType);
            }
            catch
            {
                return Factory.CreateBoxFactory(valueObject, castType);
            }
        }
    }
}