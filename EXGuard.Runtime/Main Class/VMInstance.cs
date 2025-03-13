using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using EXGuard.Runtime.Data;
using EXGuard.Runtime.Execution;
using EXGuard.Runtime.RTProtection;
using EXGuard.Runtime.Execution.Internal;

namespace EXGuard.Runtime
{
    internal unsafe class VMInstance
    {
        public static Module __ExecuteModule;
        public static VMInstance STATIC_Instance;

        public VMData Data
        {
            get
            {
                return VMData.GetVMData();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public object Invoke(object[] arguments, RuntimeMethodHandle handle)
        {
            if (AntiDump.AntiDumpIsRunning)
            {
                MethodBase virtedMethod = MethodBase.GetMethodFromHandle(handle);
                var export = Data.LookupExport(virtedMethod);

                return Invoke((ulong)export.CodeAddress, export.EntryKey, export.Signature, arguments);
            }
            else
                throw new BadImageFormatException("Anti Dump not running!");
        }

        private object Invoke(ulong codeAddr, uint key, VMFuncSig sig, object[] arguments)
        {
            var ctxStack = new Stack<VMContext>();
            var ctx = new VMContext(this);

            if (arguments == null)
                arguments = new object[0];

            if (ctx != null)
                ctxStack.Push(ctx);

            try
            {
                Debug.Assert(sig.ParamTypes.Length == arguments.Length);

                ctx.Stack.SetTopPosition((uint)arguments.Length + 1);

                for (uint i = 0; i < arguments.Length; i++)
                {
                    var paramType = sig.ParamTypes[i];

                    if (AntiDump.AntiDumpIsRunning)
                    {
                        if (paramType.IsByRef)
                            ctx.Stack[i + 1] = new VMSlot { O = arguments[i] };
                        else if (paramType.IsPointer)
                            ctx.Stack[i + 1] = new VMSlot { U8 = (ulong)Pointer.Unbox(arguments[i]) };
                        else
                            ctx.Stack[i + 1] = VMSlot.FromObject(arguments[i], sig.ParamTypes[i]);
                    }
                }

                ctx.Stack[(uint)arguments.Length + 1] = new VMSlot { U8 = 1 };

                ctx.Registers[ctx.Data.Constants.REG_K1] = new VMSlot { U8 = key };
                ctx.Registers[ctx.Data.Constants.REG_BP] = new VMSlot { U8 = 0 };
                ctx.Registers[ctx.Data.Constants.REG_SP] = new VMSlot { U8 = unchecked((ulong)arguments.Length + 1) };
                ctx.Registers[ctx.Data.Constants.REG_IP] = new VMSlot { U8 = codeAddr };

                VMDispatcher.Invoke(ctx);

                Debug.Assert(ctx.EHStack.Count == 0);

                object retVal = null;
                if (sig.RetType != typeof(void))
                {
                    var retSlot = ctx.Registers[ctx.Data.Constants.REG_R0];
                    if (Type.GetTypeCode(sig.RetType) == TypeCode.String && retSlot.O == null)
                        retVal = Data.LookupString(retSlot.U4);
                    else
                        retVal = retSlot.ToObject(sig.RetType);
                }

                return retVal;
            }
            finally
            {
                ctx.Stack.FreeAllLocalloc();

                if (ctxStack.Count > 0)
                    ctx = ctxStack.Pop();
            }
        }
    }
}