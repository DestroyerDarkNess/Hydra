using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

internal static unsafe class NativeMethods
{
    #region Win32 (Private)
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    private static extern bool CryptProtectMemory(IntPtr pData, uint cbData, uint dwFlags);

    [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    private static extern bool CryptUnprotectMemory(IntPtr pData, uint cbData, uint dwFlags);

    [DllImport("kernel32.dll", EntryPoint = "LocalAlloc")]
    private static extern void* LocalAlloc(int uFlags, ulong sizetdwBytes);

    [DllImport("kernel32.dll", EntryPoint = "LocalFree", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr handle);

    [DllImport("user32.dll", EntryPoint = "MessageBoxA")]
    private static extern int MessageBoxA(IntPtr hWnd, string lpText, string lpCaption, uint uType);
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion

    #region Win32 (Public)
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", EntryPoint = "VirtualProtect", SetLastError = true)]
    internal static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", EntryPoint = "VirtualProtect", SetLastError = true)]
    internal static extern IntPtr VirtualProtect(IntPtr lpAddress, IntPtr dwSize, IntPtr flNewProtect, ref IntPtr lpflOldProtect);

    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    internal static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

    [DllImport("Kernel32.dll", EntryPoint = "RtlSecureZeroMemory", SetLastError = false)]
    internal static extern void SecureZeroMemory(byte* dest, int size);

    [DllImport("kernel32.dll", EntryPoint = "CheckRemoteDebuggerPresent", ExactSpelling = true, SetLastError = true)]
    internal static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

    [DllImport("kernel32.dll", EntryPoint = "ZeroMemory", SetLastError = true)]
    internal static extern IntPtr ZeroMemory(IntPtr addr, IntPtr size);

    [DllImport("kernel32.dll", EntryPoint = "ZeroMemory", SetLastError = true)]
    internal static extern bool ZeroMemory(byte* destination, int length);

    [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true)]
    internal static extern IntPtr LoadLibrary(string lib);

    [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", CharSet = CharSet.Ansi, SetLastError = true)]
    internal static extern IntPtr LoadLibrary(IntPtr lpFileName);

    [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", SetLastError = true)]
    internal static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
    internal static extern void* _GetProcAddress(IntPtr lib, string proc);

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
    internal static extern IntPtr GetProcAddress(IntPtr lib, string proc);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    internal static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr procName);

    [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    internal static extern IntPtr MemSet(IntPtr dest, int c, UIntPtr count);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern void* GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", EntryPoint = "Wow64DisableWow64FsRedirection", SetLastError = true)]
    public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

    [DllImport("kernel32.dll", EntryPoint = "Wow64RevertWow64FsRedirection", SetLastError = true)]
    public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

    [DllImport("kernel32.dll", EntryPoint = "SwitchToThread", SetLastError = true)]
    internal static extern bool SwitchToThread();
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion

    #region Public Fields
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static readonly IntPtr NULL = IntPtr.Zero;
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion

    #region Public Methods
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #region MessageBox
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    internal static MessageBoxResult MessageBox(string text)
    {
        return (MessageBoxResult)MessageBoxA(IntPtr.Zero, text, "\0", (uint)MessageBoxButtons.OK);
    }

    internal static MessageBoxResult MessageBox(string text, string caption)
    {
        return (MessageBoxResult)MessageBoxA(IntPtr.Zero, text, caption, (uint)MessageBoxButtons.OK);
    }

    internal static MessageBoxResult MessageBox(string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        return (MessageBoxResult)MessageBoxA(IntPtr.Zero, text, caption, (uint)buttons);
    }

    internal static MessageBoxResult MessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        return (MessageBoxResult)MessageBoxA(IntPtr.Zero, text, caption, ((uint)buttons) | ((uint)icon));
    }

    internal static MessageBoxResult MessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton button)
    {
        return (MessageBoxResult)MessageBoxA(IntPtr.Zero, text, caption, ((uint)buttons) | ((uint)icon) | ((uint)button));
    }

    internal static MessageBoxResult MessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton button, MessageBoxModal modal)
    {
        return (MessageBoxResult)MessageBoxA(IntPtr.Zero, text, caption, ((uint)buttons) | ((uint)icon) | ((uint)button) | ((uint)modal));
    }


    /// <summary>
    /// Specifies constants defining which buttons to display on a <see cref="T:NativeMethods.MessageBoxA" />.
    /// </summary>
    internal enum MessageBoxButtons
    {
        /// <summary>
        /// The message box contains three push buttons: Abort, Retry, and Ignore.
        /// </summary>
        AbortRetryIgnore = 0x00000002,

        /// <summary>
        /// The message box contains three push buttons: Cancel, Try Again, Continue.
        /// </summary>
        CancelTryIgnore = 0x00000006,

        /// <summary>
        /// Adds a Help button to the message box.
        /// </summary>
        Help = 0x00004000,

        /// <summary>
        /// The message box contains one push button: OK. This is the default.
        /// </summary>
        OK = 0x00000000,

        /// <summary>
        /// The message box contains two push buttons: OK and Cancel.
        /// </summary>
        OKCancel = 0x00000001,

        /// <summary>
        /// The message box contains two push buttons: Retry and Cancel.
        /// </summary>
        RetryCancel = 0x00000005,

        /// <summary>
        /// The message box contains two push buttons: Yes and No.
        /// </summary>
        YesNo = 0x00000004,

        /// <summary>
        /// The message box contains three push buttons: Yes, No, and Cancel.
        /// </summary>
        YesNoCancel = 0x00000003
    }

    /// <summary>
    /// The message box returns an integer value that indicates which button the user clicked.
    /// </summary>
    internal enum MessageBoxResult
    {
        /// <summary>
        /// The 'Abort' button was selected.
        /// </summary>
        Abort = 3,

        /// <summary>
        /// The 'Cancel' button was selected.
        /// </summary>
        Cancel = 2,

        /// <summary>
        /// The 'Continue' button was selected.
        /// </summary>
        Continue = 11,

        /// <summary>
        /// The 'Ignore' button was selected.
        /// </summary>
        Ignore = 5,

        /// <summary>
        /// The 'No' button was selected.
        /// </summary>
        No = 7,

        /// <summary>
        /// The 'OK' button was selected.
        /// </summary>
        Ok = 1,

        /// <summary>
        /// The 'Retry' button was selected.
        /// </summary>
        Retry = 10,

        /// <summary>
        /// The 'Yes' button was selected.
        /// </summary>
        Yes = 6
    }

    /// <summary>
    /// To indicate the default button, specify one of the following values.
    /// </summary>
    internal enum MessageBoxDefaultButton : uint
    {
        /// <summary>
        /// The first button is the default button.
        /// </summary>
        Button1 = 0x00000000,

        /// <summary>
        /// The second button is the default button.
        /// </summary>
        Button2 = 0x00000100,

        /// <summary>
        /// The third button is the default button.
        /// </summary>
        Button3 = 0x00000200,

        /// <summary>
        /// The fourth button is the default button.
        /// </summary>
        Button4 = 0x00000300,
    }

    /// <summary>
    /// To indicate the modality of the dialog box, specify one of the following values.
    /// </summary>
    internal enum MessageBoxModal : uint
    {
        /// <summary>
        /// The user must respond to the message box before continuing work in the window identified by the hWnd parameter. However, the user can move to the windows of other threads and work in those windows. Depending on the hierarchy of windows in the application, the user may be able to move to other windows within the thread. All child windows of the parent of the message box are automatically disabled, but pop-up windows are not.
        /// </summary>
        Application = 0x00000000,

        /// <summary>
        /// Same as <see cref="Application"/> except that the message box has the Top Most style. Use system-modal message boxes to notify the user of serious, potentially damaging errors that require immediate attention (for example, running out of memory).
        /// </summary>
        System = 0x00001000,

        /// <summary>
        /// Same as <see cref="Application"/> except that all the top-level windows belonging to the current thread are disabled if the hWnd parameter is NULL. Use this flag when the calling application or library does not have a window handle available but still needs to prevent input to other windows in the calling thread without suspending other threads.
        /// </summary>
        Task = 0x00002000
    }

    /// <summary>
    /// To display an icon in the message box, specify one of the following values.
    /// </summary>
    internal enum MessageBoxIcon : uint
    {
        /// <summary>
        /// The message box contains no symbols.
        /// </summary>
		None,

        /// <summary>
        /// An exclamation-point icon appears in the message box.
        /// </summary>
        Warning = 0x00000030,

        /// <summary>
        /// An icon consisting of a lowercase letter `i` in a circle appears in the message box.
        /// </summary>
        Information = 0x00000040,

        /// <summary>
        /// A question-mark icon appears in the message box.
        /// </summary>
        /// <remarks>
        /// The question-mark message icon is no longer recommended because it does not clearly represent a specific type of message and because the phrasing of a message as a question could apply to any message type. In addition, users can confuse the message symbol question mark with Help information. Therefore, do not use this question mark message symbol in your message boxes. The system continues to support its inclusion only for backward compatibility.
        /// </remarks>
        Question = 0x00000020,

        /// <summary>
        /// A stop-sign icon appears in the message box.
        /// </summary>
        Error = 0x00000010
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion

    internal static void* malloc(ulong sizetdwBytes)
    {
        return LocalAlloc(0, sizetdwBytes);
    }

    internal static bool FreeMemory(IntPtr hglobal)
    {
        if (NULL == LocalFree(hglobal))
        {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CryptProtectMemory(IntPtr pBuffer, uint byteCount)
    {
        CryptProtectMemory(pBuffer, byteCount, 0x00); //CRYPTPROTECTMEMORY_SAME_PROCESS
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CryptUnprotectMemory(IntPtr pBuffer, uint byteCount)
    {
        CryptUnprotectMemory(pBuffer, byteCount, 0x00); //CRYPTPROTECTMEMORY_SAME_PROCESS
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion

    #region Public Delegates
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal delegate int CompileMethodDelegate(IntPtr thisPtr, IntPtr corJitInfo, CORINFO_METHOD_INFO* methodInfo, [MarshalAs(UnmanagedType.U4)] CorJitFlag flags, IntPtr nativeEntry, IntPtr nativeSizeOfCode);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal delegate IntPtr* getJit();
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion

    #region Public Structs
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x88)]
    internal unsafe struct CORINFO_METHOD_INFO
    {
        public IntPtr MethodHandle;
        public IntPtr ModuleHandle;
        public IntPtr ILCode;
        public uint ILCodeSize;
        public uint MaxStack;
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion

    #region Public Enums
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public enum CorJitFlag : uint
    {
        CORJIT_UNKNOWN = 216669565U, // i dont understand wtf is this?

        CORJIT_FLAG_CALL_GETJITFLAGS = 0xffffffff, // Indicates that the JIT should retrieve flags in the form of a
                                                   // pointer to a CORJIT_FLAGS value via ICorJitInfo::getJitFlags().
        CORJIT_FLAG_SPEED_OPT = 0,
        CORJIT_FLAG_SIZE_OPT = 1,
        CORJIT_FLAG_DEBUG_CODE = 2, // generate "debuggable" code (no code-mangling optimizations)
        CORJIT_FLAG_DEBUG_EnC = 3, // We are in Edit-n-Continue mode
        CORJIT_FLAG_DEBUG_INFO = 4, // generate line and local-var info
        CORJIT_FLAG_MIN_OPT = 5, // disable all jit optimizations (not necessarily debuggable code)
        CORJIT_FLAG_ENABLE_CFG = 6, // generate CFG enabled code
        CORJIT_FLAG_MCJIT_BACKGROUND = 7, // Calling from multicore JIT background thread, do not call JitComplete
        CORJIT_FLAG_UNUSED2 = 8,
        CORJIT_FLAG_UNUSED3 = 9,
        CORJIT_FLAG_UNUSED4 = 10,
        CORJIT_FLAG_UNUSED5 = 11,
        CORJIT_FLAG_UNUSED6 = 12,
        CORJIT_FLAG_OSR = 13, // Generate alternate version for On Stack Replacement
        CORJIT_FLAG_ALT_JIT = 14, // JIT should consider itself an ALT_JIT
        CORJIT_FLAG_UNUSED10 = 17,
        CORJIT_FLAG_MAKEFINALCODE = 18, // Use the final code generator, i.e., not the interpreter.
        CORJIT_FLAG_READYTORUN = 19, // Use version-resilient code generation
        CORJIT_FLAG_PROF_ENTERLEAVE = 20, // Instrument prologues/epilogues
        CORJIT_FLAG_UNUSED7 = 21,
        CORJIT_FLAG_PROF_NO_PINVOKE_INLINE = 22, // Disables PInvoke inlining
        CORJIT_FLAG_SKIP_VERIFICATION = 23, // (lazy) skip verification - determined without doing a full resolve. See comment below
        CORJIT_FLAG_PREJIT = 24, // jit or prejit is the execution engine.
        CORJIT_FLAG_RELOC = 25, // Generate relocatable code
        CORJIT_FLAG_IMPORT_ONLY = 26, // Only import the function
        CORJIT_FLAG_IL_STUB = 27, // method is an IL stub
        CORJIT_FLAG_PROCSPLIT = 28, // JIT should separate code into hot and cold sections
        CORJIT_FLAG_BBINSTR = 29, // Collect basic block profile information
        CORJIT_FLAG_BBOPT = 30, // Optimize method based on profile information
        CORJIT_FLAG_FRAMED = 31, // All methods have an EBP frame
        CORJIT_FLAG_BBINSTR_IF_LOOPS = 32, // JIT must instrument current method if it has loops
        CORJIT_FLAG_PUBLISH_SECRET_PARAM = 33, // JIT must place stub secret param into local 0.  (used by IL stubs)
        CORJIT_FLAG_UNUSED9 = 34,
        CORJIT_FLAG_SAMPLING_JIT_BACKGROUND = 35, // JIT is being invoked as a result of stack sampling for hot methods in the background
        CORJIT_FLAG_USE_PINVOKE_HELPERS = 36, // The JIT should use the PINVOKE_{BEGIN,END} helpers instead of emitting inline transitions
        CORJIT_FLAG_REVERSE_PINVOKE = 37, // The JIT should insert REVERSE_PINVOKE_{ENTER,EXIT} helpers into method prolog/epilog
        CORJIT_FLAG_TRACK_TRANSITIONS = 38, // The JIT should insert the helper variants that track transitions.
        CORJIT_FLAG_TIER0 = 39, // This is the initial tier for tiered compilation which should generate code as quickly as possible
        CORJIT_FLAG_TIER1 = 40, // This is the final tier (for now) for tiered compilation which should generate high quality code
        CORJIT_FLAG_RELATIVE_CODE_RELOCS = 41, // JIT should generate PC-relative address computations instead of EE relocation records
        CORJIT_FLAG_NO_INLINING = 42, // JIT should not inline any called method into this method
        CORJIT_FLAG_SOFTFP_ABI = 43, // On ARM should enable armel calling convention
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    #endregion
}
