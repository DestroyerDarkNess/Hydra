using System;

namespace HydraEngine.Protection.Method.Runtime
{
    public static class M2D
    {
        public static TDelegate CreateDelegate<TDelegate>(System.Reflection.MethodInfo methodInfo)
        {
            return (TDelegate)(object)Delegate.CreateDelegate(typeof(TDelegate), methodInfo);
        }
    }
}
