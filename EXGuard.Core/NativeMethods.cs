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
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", EntryPoint = "VirtualProtect", SetLastError = true)]
    internal static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", EntryPoint = "VirtualProtect", SetLastError = true)]
    internal static extern IntPtr VirtualProtect(IntPtr lpAddress, IntPtr dwSize, IntPtr flNewProtect, ref IntPtr lpflOldProtect);

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
    /////////////////////////////////////////////////////////////////////////////////

    [DllImport("kernel32.dll", EntryPoint = "SwitchToThread", SetLastError = true)]
    internal static extern bool SwitchToThread();
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
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
}
