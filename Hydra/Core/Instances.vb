Namespace Core

    Public Class Instances

        Public Shared MainInstance As MainWindow = Nothing
        Public Shared FileVer As String = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileVersion & "_Release"

    End Class

End Namespace