using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

using EXGuard.Runtime.RTProtection;

using static NativeMethods;

namespace EXGuard.Runtime.JIT
{
    internal static unsafe class JITRuntime
    {
        static IntPtr EXECModuleHandle;

        static bool ver4
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get;
            [MethodImpl(MethodImplOptions.NoInlining)]
            set;
        }

        static bool FirstRunDone
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get;
            [MethodImpl(MethodImplOptions.NoInlining)]
            set;
        }

        static Dictionary<IntPtr, JITEDMethodInfo> EncryptedHandles
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get;
            [MethodImpl(MethodImplOptions.NoInlining)]
            set;
        }

        static CompileMethodDelegate OriginalCompileMethod
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get;
            [MethodImpl(MethodImplOptions.NoInlining)]
            set;
        }

        static CompileMethodDelegate CustomCompileMethod
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get;
            [MethodImpl(MethodImplOptions.NoInlining)]
            set;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe void Initialize(Version ver)
        {
            if (AntiDump.AntiDumpIsRunning)
            {
                ver4 = ver.Major == 4;
                FirstRunDone = false;

                ulong* ptr = stackalloc ulong[2];

                #region Load JIT Original Compiler Method
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                if (ver4)
                {
                    ptr[0] = 0x642e74696a726c63; //clrjit.d
                    ptr[1] = 0x0000000000006c6c; //ll......
                }
                else
                {
                    ptr[0] = 0x74696a726f63736d; //mscorjit
                    ptr[1] = 0x000000006c6c642e; //.dll....
                }

                IntPtr jit = LoadLibrary(new string((sbyte*)ptr));

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                ptr[0] = 0x000074694a746567; //getJit
                var getPtr = GetProcAddress(jit, new string((sbyte*)ptr));
                var get = (getJit)Utils.GetDelegateForFunctionPointer(getPtr, typeof(getJit));

                IntPtr hookPosition = *get();
                IntPtr original = *(IntPtr*)hookPosition;
                IntPtr trampoline = IntPtr.Zero;

                if (IntPtr.Size == 8)
                {
                    trampoline = Marshal.AllocHGlobal(16);

                    ((ulong*)trampoline)[0] = 0xffffffffffffb848;
                    ((ulong*)trampoline)[1] = 0x90909090e0ffffff;

                    VirtualProtect(trampoline, 12, 0x40, out _);
                    Marshal.WriteIntPtr(trampoline, 2, original);
                }
                else
                {
                    trampoline = Marshal.AllocHGlobal(8);

                    ((ulong*)trampoline)[0] = 0x90e0ffffffffffb8;

                    VirtualProtect(trampoline, 7, 0x40, out _);
                    Marshal.WriteIntPtr(trampoline, 1, original);
                }

                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                OriginalCompileMethod = (CompileMethodDelegate)Utils.GetDelegateForFunctionPointer(trampoline, typeof(CompileMethodDelegate));
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                #region Get ModuleHandle (IntPtr)
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                if (ver4)
                {
                    ptr[0] = 0x0061746144705f6d; //m_pData
                    ptr[1] = 0x0000000000000007; //7 Length
                }
                else
                {
                    ptr[0] = 0x61746144705f5f6d; //m__pData
                    ptr[1] = 0x0000000000000008; //8 Length
                }

                EXECModuleHandle = (IntPtr)VMInstance.__ExecuteModule.GetType().GetField(new string((sbyte*)&ptr[0], 0, (int)ptr[1]),
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(VMInstance.__ExecuteModule);
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                CustomCompileMethod = HookCompileMethod;

                RuntimeHelpers.PrepareDelegate(CustomCompileMethod);
                RuntimeHelpers.PrepareDelegate(OriginalCompileMethod);

                RuntimeHelpers.PrepareMethod(CustomCompileMethod.Method.MethodHandle);
                RuntimeHelpers.PrepareMethod(OriginalCompileMethod.Method.MethodHandle);

                #region Read JITData
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                uint[] dataBuffer = new uint[Mutation.IntKey0];
                EncryptedHandles = new Dictionary<IntPtr, JITEDMethodInfo>();

                if (dataBuffer.Length != 0)
                {
                    RuntimeHelpers.InitializeArray(dataBuffer, Mutation.LocationIndex<RuntimeFieldHandle>());

                    #region Decrypt and Decompress Data
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    uint state = (uint)Mutation.IntKey1;

                    var enc = new uint[0x10];
                    var key = new uint[0x10];
                    for (int i = 0; i < 0x10; i++)
                    {
                        state ^= state >> 13;
                        state ^= state << 25;
                        state ^= state >> 27;

                        key[i] = (uint)state;
                    }

                    var decryptedBuff = new byte[dataBuffer.Length * 4];

                    int buffIndex = 0, decBuffIndex = 0;
                    while (buffIndex < dataBuffer.Length)
                    {
                        for (int j = 0; j < 0x10; j++)
                            enc[j] = dataBuffer[buffIndex + j];

                        Mutation.Crypt(enc, key);

                        for (int j = 0; j < 0x10; j++)
                        {
                            uint value = enc[j];

                            decryptedBuff[decBuffIndex++] = (byte)value;
                            decryptedBuff[decBuffIndex++] = (byte)(value >> 8);
                            decryptedBuff[decBuffIndex++] = (byte)(value >> 16);
                            decryptedBuff[decBuffIndex++] = (byte)(value >> 24);

                            key[j] ^= value;
                        }

                        buffIndex += 0x10;
                    }

                    #region LZMA Decompress
                    ///////////////////////////////////////////////////
                    decryptedBuff = Lzma.Decompress(decryptedBuff);
                    ///////////////////////////////////////////////////
                    #endregion
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    #endregion

                    var reader = new BinaryReader(new MemoryStream(decryptedBuff));

                    var dataLen = reader.ReadInt32();
                    for (int i = 0; i < dataLen; i++)
                    {
                        var jmdInfo = new JITEDMethodInfo();

                        jmdInfo.MethodToken = reader.ReadInt32();
                        jmdInfo.MaxStack = reader.ReadUInt32();
                        jmdInfo.ILCodeSize = reader.ReadUInt32();
                        jmdInfo.ILCode = reader.ReadBytes((int)jmdInfo.ILCodeSize);

                        var methodBase = VMInstance.__ExecuteModule.ResolveMethod(jmdInfo.MethodToken);
                        EncryptedHandles.Add(methodBase.MethodHandle.Value, jmdInfo);
                    }
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                #region Protect JIT Library
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                VirtualProtect(getPtr, (uint)IntPtr.Size, 0x40, out var _oldjl);
                ZeroMemory(getPtr, IntPtr.Zero);
                CryptProtectMemory(getPtr, (uint)IntPtr.Size);
                VirtualProtect(getPtr, (uint)IntPtr.Size, _oldjl, out _oldjl);
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                #region Protect getJit
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                VirtualProtect(jit, (uint)IntPtr.Size, 0x40, out var _oldgetj);
                ZeroMemory(jit, IntPtr.Zero);
                CryptProtectMemory(jit, (uint)IntPtr.Size); ;
                VirtualProtect(jit, (uint)IntPtr.Size, _oldgetj, out _oldgetj);
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                #region Hook
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                VirtualProtect(hookPosition, (uint)IntPtr.Size, 0x40, out var old);
                Marshal.WriteIntPtr(hookPosition, Marshal.GetFunctionPointerForDelegate(CustomCompileMethod));
                VirtualProtect(hookPosition, (uint)IntPtr.Size, old, out old);
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int HookCompileMethod(IntPtr thisPtr, IntPtr corJitInfo, CORINFO_METHOD_INFO* methodInfo, [MarshalAs(UnmanagedType.U4)] CorJitFlag flags, IntPtr nativeEntry, IntPtr nativeSizeOfCode)
        {
            if (corJitInfo == null)
                return OriginalCompileMethod(thisPtr, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode);

            if (methodInfo->ModuleHandle != EXECModuleHandle)
                return OriginalCompileMethod(thisPtr, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode);

            if (!EncryptedHandles.ContainsKey(methodInfo->MethodHandle))
                return OriginalCompileMethod(thisPtr, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode);

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var nmethodInfo = *methodInfo;
            JITHooker(ref nmethodInfo);

            VirtualProtect((IntPtr)methodInfo, 0x88, 0x40, out var old);
            memcpy((IntPtr)methodInfo, (IntPtr)(&nmethodInfo), 0x88);
            VirtualProtect((IntPtr)methodInfo, 0x88, old, out old);
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            int ret = 0;
            if (flags == CorJitFlag.CORJIT_UNKNOWN && !FirstRunDone)
                FirstRunDone = true;
            else
            {
                flags |= CorJitFlag.CORJIT_FLAG_PREJIT | CorJitFlag.CORJIT_FLAG_MAKEFINALCODE;
                ret = OriginalCompileMethod(thisPtr, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode);

                nmethodInfo = new CORINFO_METHOD_INFO();
                nmethodInfo.ModuleHandle = IntPtr.Zero;
                nmethodInfo.MethodHandle = IntPtr.Zero;
                nmethodInfo.ILCode = IntPtr.Zero;
                nmethodInfo.ILCodeSize = 0;
                nmethodInfo.MaxStack = 0;

                methodInfo = &nmethodInfo;
            }

            return ret;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void JITHooker(ref CORINFO_METHOD_INFO methodInfo)
        {
            if (AntiDump.AntiDumpIsRunning)
            {
                var jmdInfo = EncryptedHandles[methodInfo.MethodHandle];

                var ptrBuffer = Marshal.AllocHGlobal(jmdInfo.ILCode.Length);
                Marshal.Copy(jmdInfo.ILCode, 0, ptrBuffer, jmdInfo.ILCode.Length);

                methodInfo.ILCode = ptrBuffer;
                methodInfo.ILCodeSize = jmdInfo.ILCodeSize;
                methodInfo.MaxStack = jmdInfo.MaxStack;
            }
        }
    }
}
