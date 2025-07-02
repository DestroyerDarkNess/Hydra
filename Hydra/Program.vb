Imports System.Runtime.InteropServices
Imports Hydra.Core

Public Class Program

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function SetProcessDPIAware() As Boolean : End Function

    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Shared Function AllocConsole() As Boolean : End Function

    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Shared Function FreeConsole() As Boolean : End Function

    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Shared Function GetConsoleWindow() As IntPtr : End Function

    Public Shared Sub Main()

        ' Obtener argumentos de línea de comandos
        Dim args() As String = Environment.GetCommandLineArgs()

        ' Remover el primer argumento (nombre del ejecutable)
        If args.Length > 1 Then
            Dim actualArgs(args.Length - 2) As String
            Array.Copy(args, 1, actualArgs, 0, args.Length - 1)
            args = actualArgs
        Else
            args = New String() {}
        End If

        ' Parsear argumentos
        Dim cmdArgs As CommandLineArgs = CommandLineArgs.Parse(args)

        ' Mostrar ayuda si se solicitó
        If cmdArgs.ShowHelp Then
            AllocConsole()
            CommandLineArgs.ShowHelps()
            FreeConsole()
            Return
        End If

        ' Verificar si hay errores en los argumentos
        If Not cmdArgs.IsValid Then
            AllocConsole()
            Console.WriteLine("ERROR: " & cmdArgs.ErrorMessage)
            Console.WriteLine()
            CommandLineArgs.ShowHelps()
            FreeConsole()
            Environment.Exit(1)
            Return
        End If

        ' Determinar modo de ejecución
        If Not String.IsNullOrEmpty(cmdArgs.InputFile) Then
            ' Modo línea de comandos
            ExecuteCommandLineMode(cmdArgs)
        Else
            ' Modo GUI tradicional
            ExecuteGUIMode(cmdArgs)
        End If

    End Sub

    Private Shared Sub ExecuteCommandLineMode(cmdArgs As CommandLineArgs)
        Dim exitCode As Integer = 0

        Select Case cmdArgs.ExecutionModes
            Case CommandLineArgs.ExecutionMode.Console
                ' Mostrar consola y ocultar ventanas
                AllocConsole()
                Console.Title = "Hydra.NET Console Mode"
                exitCode = ConsoleRunner.RunProtection(cmdArgs)
                FreeConsole()

            Case CommandLineArgs.ExecutionMode.Hidden
                ' Ejecutar completamente oculto
                exitCode = ConsoleRunner.RunProtection(cmdArgs)

            Case CommandLineArgs.ExecutionMode.GUI
                ' Mostrar GUI pero cargar archivo automáticamente
                Application.EnableVisualStyles()
                Application.SetCompatibleTextRenderingDefault(False)
                SetProcessDPIAware()

                Dim mainWindow As New MainWindow()
                mainWindow.CommandLineArgs = cmdArgs
                Application.Run(mainWindow)
                Return

        End Select

        Environment.Exit(exitCode)
    End Sub

    Private Shared Sub ExecuteGUIMode(cmdArgs As CommandLineArgs)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        SetProcessDPIAware()

        Console.Title = "Hydra.NET [Private Protector]"
        Application.Run(New MainWindow())
    End Sub

End Class