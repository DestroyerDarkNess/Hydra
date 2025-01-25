Imports System.Runtime.InteropServices
Imports Hydra.Core

Public Class Program

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function SetProcessDPIAware() As Boolean : End Function

    Public Shared Sub Main()

        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        SetProcessDPIAware()

        Console.Title = "Hydra.NET [Private Protector]"
        Application.Run(New MainWindow)

    End Sub


End Class
