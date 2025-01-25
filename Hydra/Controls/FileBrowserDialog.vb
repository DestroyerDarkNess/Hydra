Imports System
Imports System.ComponentModel
Imports System.Drawing.Design
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace FileBorserDialog

#Region "Editor"

    Public Class FolderNameEditor
        Inherits UITypeEditor

        Public Overrides Function GetEditStyle(ByVal context As ITypeDescriptorContext) As UITypeEditorEditStyle
            Return UITypeEditorEditStyle.Modal
        End Function

        Public Overrides Function EditValue(ByVal context As ITypeDescriptorContext, ByVal provider As IServiceProvider, ByVal value As Object) As Object
            Dim browser As FolderBrowserDialog = New FolderBrowserDialog()

            If value IsNot Nothing Then
                browser.DirectoryPath = String.Format("{0}", value)
            End If

            If browser.ShowDialog(Nothing) = DialogResult.OK Then
                Return browser.DirectoryPath
            End If

            Return value
        End Function
    End Class

#End Region

#Region "FolderBrowserDialog Base"


    <Description("提供一個Vista樣式的選擇文件對話框")>
    <EditorAttribute(GetType(FolderNameEditor), GetType(UITypeEditor))>
    Public Class FolderBrowserDialog
        Inherits Component

        Public Sub New()
        End Sub


#Region "Public Property"

        Public Property DirectoryPath As String

        Public Function ShowDialog(ByVal owner As IWin32Window) As DialogResult
            Dim hwndOwner As IntPtr = If(owner IsNot Nothing, owner.Handle, GetActiveWindow())
            Dim dialog As IFileOpenDialog = CType(New FileOpenDialog(), IFileOpenDialog)

            Try
                Dim item As IShellItem = Nothing

                If Not String.IsNullOrEmpty(DirectoryPath) Then
                    Dim idl As IntPtr
                    Dim atts As UInteger = 0

                    If SHILCreateFromPath(DirectoryPath, idl, atts) = 0 Then

                        If SHCreateShellItem(IntPtr.Zero, IntPtr.Zero, idl, item) = 0 Then
                            dialog.SetFolder(item)
                        End If
                    End If
                End If

                dialog.SetOptions(FOS.FOS_PICKFOLDERS Or FOS.FOS_FORCEFILESYSTEM)
                Dim hr As UInteger = dialog.Show(hwndOwner)
                If hr = ERROR_CANCELLED Then Return DialogResult.Cancel
                If hr <> 0 Then Return DialogResult.Abort
                dialog.GetResult(item)
                Dim path As String = String.Empty
                item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, path)
                DirectoryPath = path
                Return DialogResult.OK
            Finally
                Marshal.ReleaseComObject(dialog)
            End Try
        End Function

#End Region

#Region "BaseType"
        <DllImport("shell32.dll")>
        Private Shared Function SHILCreateFromPath(
        <MarshalAs(UnmanagedType.LPWStr)> ByVal pszPath As String, <Out> ByRef ppIdl As IntPtr, ByRef rgflnOut As UInteger) As Integer
        End Function

        <DllImport("shell32.dll")>
        Private Shared Function SHCreateShellItem(ByVal pidlParent As IntPtr, ByVal psfParent As IntPtr, ByVal pidl As IntPtr, <Out> ByRef ppsi As IShellItem) As Integer
        End Function

        <DllImport("user32.dll")>
        Private Shared Function GetActiveWindow() As IntPtr
        End Function

        Private Const ERROR_CANCELLED As Integer = &H800704C7

        <ComImport>
        <Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")>
        Private Class FileOpenDialog
        End Class

        <ComImport>
        <Guid("42f85136-db7e-439c-85f1-e4075d135fc8")>
        <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
        Private Interface IFileOpenDialog
            <PreserveSig>
            Function Show(
            <[In]> ByVal parent As IntPtr) As UInteger ' IModalWindow
            Sub SetFileTypes()  ' not fully defined
            Sub SetFileTypeIndex(
            <[In]> ByVal iFileType As UInteger)
            Sub GetFileTypeIndex(<Out> ByRef piFileType As UInteger)
            Sub Advise() ' not fully defined
            Sub Unadvise()
            Sub SetOptions(
<[In]> ByVal fos As FOS)
            Sub GetOptions(<Out> ByRef pfos As FOS)
            Sub SetDefaultFolder(ByVal psi As IShellItem)
            Sub SetFolder(ByVal psi As IShellItem)
            Sub GetFolder(<Out> ByRef ppsi As IShellItem)
            Sub GetCurrentSelection(<Out> ByRef ppsi As IShellItem)
            Sub SetFileName(
<[In], MarshalAs(UnmanagedType.LPWStr)> ByVal pszName As String)
            Sub GetFileName(<Out>
<MarshalAs(UnmanagedType.LPWStr)> ByRef pszName As String)
            Sub SetTitle(
<[In], MarshalAs(UnmanagedType.LPWStr)> ByVal pszTitle As String)
            Sub SetOkButtonLabel(
<[In], MarshalAs(UnmanagedType.LPWStr)> ByVal pszText As String)
            Sub SetFileNameLabel(
<[In], MarshalAs(UnmanagedType.LPWStr)> ByVal pszLabel As String)
            Sub GetResult(<Out> ByRef ppsi As IShellItem)
            Sub AddPlace(ByVal psi As IShellItem, ByVal alignment As Integer)
            Sub SetDefaultExtension(
<[In], MarshalAs(UnmanagedType.LPWStr)> ByVal pszDefaultExtension As String)
            Sub Close(ByVal hr As Integer)
            Sub SetClientGuid()  ' not fully defined
            Sub ClearClientData()
            Sub SetFilter(
<MarshalAs(UnmanagedType.Interface)> ByVal pFilter As IntPtr)
            Sub GetResults(<Out>
<MarshalAs(UnmanagedType.Interface)> ByRef ppenum As IntPtr) ' not fully defined
            Sub GetSelectedItems(<Out>
            <MarshalAs(UnmanagedType.Interface)> ByRef ppsai As IntPtr) ' not fully defined
        End Interface

        <ComImport>
        <Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")>
        <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
        Private Interface IShellItem
            Sub BindToHandler() ' not fully defined
            Sub GetParent() ' not fully defined
            Sub GetDisplayName(
            <[In]> ByVal sigdnName As SIGDN, <Out>
            <MarshalAs(UnmanagedType.LPWStr)> ByRef ppszName As String)
            Sub GetAttributes()  ' not fully defined
            Sub Compare()  ' not fully defined
        End Interface

        Private Enum SIGDN As Integer
            SIGDN_DESKTOPABSOLUTEEDITING = &H8004C000
            SIGDN_DESKTOPABSOLUTEPARSING = &H80028000
            SIGDN_FILESYSPATH = &H80058000
            SIGDN_NORMALDISPLAY = 0
            SIGDN_PARENTRELATIVE = &H80080001
            SIGDN_PARENTRELATIVEEDITING = &H80031001
            SIGDN_PARENTRELATIVEFORADDRESSBAR = &H8007C001
            SIGDN_PARENTRELATIVEPARSING = &H80018001
            SIGDN_URL = &H80068000
        End Enum

        <Flags>
        Private Enum FOS
            FOS_ALLNONSTORAGEITEMS = &H80
            FOS_ALLOWMULTISELECT = &H200
            FOS_CREATEPROMPT = &H2000
            FOS_DEFAULTNOMINIMODE = &H20000000
            FOS_DONTADDTORECENT = &H2000000
            FOS_FILEMUSTEXIST = &H1000
            FOS_FORCEFILESYSTEM = &H40
            FOS_FORCESHOWHIDDEN = &H10000000
            FOS_HIDEMRUPLACES = &H20000
            FOS_HIDEPINNEDPLACES = &H40000
            FOS_NOCHANGEDIR = 8
            FOS_NODEREFERENCELINKS = &H100000
            FOS_NOREADONLYRETURN = &H8000
            FOS_NOTESTFILECREATE = &H10000
            FOS_NOVALIDATE = &H100
            FOS_OVERWRITEPROMPT = 2
            FOS_PATHMUSTEXIST = &H800
            FOS_PICKFOLDERS = &H20
            FOS_SHAREAWARE = &H4000
            FOS_STRICTFILETYPES = 4
        End Enum
#End Region
    End Class
#End Region

End Namespace
