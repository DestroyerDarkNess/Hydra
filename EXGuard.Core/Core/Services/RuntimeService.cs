using System;
using System.IO;
using System.Reflection;

using dnlib.DotNet;

namespace EXGuard.Core.Services
{
	internal class RuntimeService {
        private static ModuleDef rtModule;

		public static TypeDef GetRuntimeType(string runtimeDllName, string fullName) {
			if (rtModule == null) {
				LoadConfuserRuntimeModule(runtimeDllName);
			}
			return rtModule.Find(fullName, true);
		}

        public static TypeDef GetRuntimeType(Module runtimeDllModule, string fullName)
        {
            if (rtModule == null)
            {
                LoadConfuserRuntimeModule(runtimeDllModule);
            }
            return rtModule.Find(fullName, true);
        }

        private static void LoadConfuserRuntimeModule(string runtimeDllName) {
			var module = typeof(RuntimeService).Assembly.ManifestModule;
			string rtPath = runtimeDllName;
			var creationOptions = new ModuleCreationOptions() { TryToLoadPdbFromDisk = true };
			if (module.FullyQualifiedName[0] != '<') {
				rtPath = Path.Combine(Path.GetDirectoryName(module.FullyQualifiedName), rtPath);
				if (File.Exists(rtPath)) {
					try {
						rtModule = ModuleDefMD.Load(rtPath, creationOptions);
					}
					catch (IOException) { }
				}
				if (rtModule == null) {
					rtPath = runtimeDllName;
				}
			}
			if (rtModule == null) {
				rtModule = ModuleDefMD.Load(rtPath, creationOptions);
			}
			rtModule.EnableTypeDefFindCache = true;
		}


        private static void LoadConfuserRuntimeModule(Module runtimeDllModule)
        {
            var creationOptions = new ModuleCreationOptions() { TryToLoadPdbFromDisk = true };
            if (rtModule == null)
            {
                try
                {
                    rtModule = ModuleDefMD.Load(runtimeDllModule, creationOptions);
                }
                catch (IOException) { }
            }
            rtModule.EnableTypeDefFindCache = true;
        }
    }
}
