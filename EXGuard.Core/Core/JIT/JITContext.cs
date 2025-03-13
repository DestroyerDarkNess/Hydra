using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.Core.RT;

namespace EXGuard.Core.JIT
{
    public class JITContext
    {
        public VMRuntime Runtime;
        public HashSet<MethodDef> Targets;
        public static List<CilBody> RealBodies;
    }
}
