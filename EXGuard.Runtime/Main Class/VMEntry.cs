using System;
using System.Reflection;
using System.Diagnostics;

using EXGuard.Runtime.JIT;
using EXGuard.Runtime.RTProtection;

namespace EXGuard.Runtime {
	public unsafe class VMEntry {
        public static void EntryInitialize(Version ver)
        {
            #region Anti Dump Protection For RT
            /////////////////////////////
            AntiDump.Initialize();
            /////////////////////////////
            #endregion

            if (AntiDump.AntiDumpIsRunning)
            {
                #region Call Constant String Protection "Initialize
                ///////////////////////////////////////////
                Constant.Initialize();
                ///////////////////////////////////////////
                #endregion

                if (VMInstance.__ExecuteModule == null)
                    VMInstance.__ExecuteModule = new StackFrame(1, false).GetMethod().Module;

                if (VMInstance.STATIC_Instance == null)
                    VMInstance.STATIC_Instance = new VMInstance();

                JITRuntime.Initialize(ver);
            }
            else
                throw new BadImageFormatException("Anti Dump not running!");
        }
    }
}