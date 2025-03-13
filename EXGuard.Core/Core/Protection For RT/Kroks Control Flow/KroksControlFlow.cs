using System;
using System.Text;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using EXGuard.DynCipher;
using EXGuard.Core.Helpers;
using EXGuard.Core.Services;

namespace EXGuard.Core.RTProtections.KroksCFlow
{
    public static class KroksControlFlow
    {
        public static void Execute(MethodDef method, int repeat)
        {
            var ret = new KroksContext();
            ret.Intensity = 60 / 100.0;
            ret.Depth = 6;
            ret.JunkCode = true;
            ret.Method = method;
            ret.StateVariable = new Local(method.Module.CorLibTypes.Int32);

            method.Body.Variables.Add(ret.StateVariable);

            ret.DynCipher = new DynCipherService();
            ret.Random = new RandomGenerator(32);

            if (method.HasBody && method.Body.Instructions.Count > 0)
            {
                for (int a = 0; a < repeat; a++) //1x repeat
                    ret.ProcessMethod(method.Body, ret);

                method.Body.SimplifyBranches();
            }
        }
    }
}
