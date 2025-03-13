using System;
using System.Collections.Generic;

using dnlib.DotNet;

namespace EXGuard.Core.Services
{
    internal class TraceService
    {
        public MethodTrace Trace(MethodDef method)
        {
            bool flag = method == null;
            if (flag)
            {
                throw new ArgumentNullException("method");
            }
            return this.cache.GetValueOrDefaultLazy(method, (MethodDef m) => this.cache[m] = new MethodTrace(m)).Trace();
        }

        private readonly Dictionary<MethodDef, MethodTrace> cache = new Dictionary<MethodDef, MethodTrace>();
    }
}
