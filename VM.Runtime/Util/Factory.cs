using VM.Runtime.Handler;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace VM.Runtime.Util
{
    public delegate int SizeOfFactory();
    public static class Factory
    {
        private static Dictionary<Type, SizeOfFactory> _sizeofFactories =
            new Dictionary<Type, SizeOfFactory>();

        #region Dynamic Arithmetics
        private static CallSite<Func<CallSite, object, object, object>> CreateBinaryBinder(ExpressionType expressionType)
        {
            return CallSite<Func<CallSite, object, object, object>>.Create(
                Binder.BinaryOperation(CSharpBinderFlags.None,
                expressionType,
                typeof(Factory), new CSharpArgumentInfo[] {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                })
            );
        }

        private static CallSite<Func<CallSite, object, object>> CreateUnaryBinder(ExpressionType expressionType)
        {
            return CallSite<Func<CallSite, object, object>>.Create(
                Binder.UnaryOperation(CSharpBinderFlags.None,
                expressionType,
                typeof(Factory), new CSharpArgumentInfo[] {
                    CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                })
            );
        }
        private static Dictionary<HxOpCodes, object> _arithmeticFactories =
            new Dictionary<HxOpCodes, object>()
            {
                { HxOpCodes.AAdd, CreateBinaryBinder(ExpressionType.Add) },
                { HxOpCodes.AAnd, CreateBinaryBinder(ExpressionType.And) },
                { HxOpCodes.ADiv, CreateBinaryBinder(ExpressionType.Divide) },
                { HxOpCodes.AMul, CreateBinaryBinder(ExpressionType.Multiply) },
                { HxOpCodes.ANeg, CreateUnaryBinder(ExpressionType.Negate) },
                { HxOpCodes.ANot, CreateUnaryBinder(ExpressionType.OnesComplement) },
                { HxOpCodes.AOr, CreateBinaryBinder(ExpressionType.Or) },
                { HxOpCodes.ARem, CreateBinaryBinder(ExpressionType.Modulo) },
                { HxOpCodes.AShl, CreateBinaryBinder(ExpressionType.LeftShift) },
                { HxOpCodes.AShr, CreateBinaryBinder(ExpressionType.RightShift) },
                { HxOpCodes.ASub, CreateBinaryBinder(ExpressionType.Subtract) },
                { HxOpCodes.AXor, CreateBinaryBinder(ExpressionType.ExclusiveOr) },
                { HxOpCodes.HxClt, CreateBinaryBinder(ExpressionType.LessThan) },
                { HxOpCodes.ACeq, CreateBinaryBinder(ExpressionType.Equal) }
            };
        #endregion

        public static int CreateSizeOfFactory(Type sizeType)
        {
            if (_sizeofFactories.ContainsKey(sizeType))
            {
                return _sizeofFactories[sizeType]();
            }
            else
            {
                var dynamicMethod = new DynamicMethod(string.Empty,
                    typeof(int), Type.EmptyTypes,
                    typeof(Factory).Module, true);
                var ilGen = dynamicMethod.GetILGenerator();
                ilGen.Emit(OpCodes.Sizeof, sizeType);
                ilGen.Emit(OpCodes.Ret);
                var sizeDelegate = (SizeOfFactory)dynamicMethod.CreateDelegate(typeof(SizeOfFactory));
                _sizeofFactories[sizeType] = sizeDelegate;
                return sizeDelegate();
            }
            throw new ArgumentOutOfRangeException();
        }

        public static object CreateBoxFactory(object obj, Type boxType)
        {
            var dyn = new DynamicMethod(string.Empty, boxType, new[] { typeof(object) }, typeof(Factory).Module, true);
            var ilgen = dyn.GetILGenerator();
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Box, boxType);
            ilgen.Emit(OpCodes.Ret);
            return dyn.Invoke(null, new[] { obj });
        }

        public static object CreateArithmethicFactory(HxOpCodes key, object[] reversedPops)
        {
            dynamic result = null;
            switch (key)
            {
                case HxOpCodes.ANeg:
                case HxOpCodes.ANot:
                    var unaryFactory = (CallSite<Func<CallSite, object, object>>)_arithmeticFactories[key];
                    result = unaryFactory.Target(unaryFactory, reversedPops[0]);
                    break;
                default:
                    var firstValue = reversedPops[0];
                    var secondValue = reversedPops[1];
                    var realKey = _arithmeticFactories[key];
                    var arithmeticFactory = (CallSite<Func<CallSite, object, object, object>>)realKey;
                    result = arithmeticFactory.Target(arithmeticFactory, firstValue, secondValue);
                    break;
            }
            return result;
        }

        public static object CreateArithmethicFactoryM(object[] reversedPops, ExpressionType ET)
        {
            dynamic result = null;
            var firstValue = reversedPops[0];
            var secondValue = reversedPops[1];
            var realKey = CreateBinaryBinder(ET);
            var arithmeticFactory = (CallSite<Func<CallSite, object, object, object>>)realKey;
            result = arithmeticFactory.Target(arithmeticFactory, firstValue, secondValue);
            return result;
        }
    }
}