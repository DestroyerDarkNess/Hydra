using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.Collections.Generic;

using EXGuard.Core;
using EXGuard.Core.RT;
using EXGuard.Core.RT.Mutation;

namespace EXGuard.Internal
{
    public class EXGuardTask
    {
        public void Exceute(ModuleDefMD module, HashSet<MethodDef> methods, string outPath, string runtimeName, string snPath, string snPass)
        {
            var _init = new InitializePhase(module)
            {
                Methods = methods,

                RT_OUT_Directory = Path.GetDirectoryName(outPath),
                RTName = runtimeName,

                SNK_File = snPath,
                SNK_Password = snPass
            };

            _init.Initialize();
            _init.GetProtectedFile(out var exec);
            _init.SaveRuntime();

            File.WriteAllBytes(outPath, exec);

            _init.Dispose();
        }
    }
}
