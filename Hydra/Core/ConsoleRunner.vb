Imports System.IO
Imports dnlib.DotNet
Imports dnlib.DotNet.Writer
Imports Guna.UI2.WinForms
Imports HydraEngine.Core

Public Class ConsoleRunner

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean : End Function

    Private Const SW_HIDE As Integer = 0
    Private Const SW_SHOW As Integer = 5

    Public Shared Function RunProtection(args As CommandLineArgs) As Integer
        Try
            Console.WriteLine("Hydra.NET Console Mode")
            Console.WriteLine("=====================")
            Console.WriteLine()

            Dim tempDesigner As New ProjectDesigner()
            tempDesigner.IsConsoleMode = True
            tempDesigner.ShowInTaskbar = False
            tempDesigner.WindowState = FormWindowState.Minimized
            tempDesigner.Show()
            AddHandler tempDesigner.Shown, Sub()
                                               ShowWindow(tempDesigner.Handle, SW_HIDE)
                                           End Sub
            Console.WriteLine($"Loading assembly: {args.InputFile}")

            If tempDesigner.LoadFile(args.InputFile) = False Then
                Console.WriteLine("ERROR: Failed to load assembly. Make sure it's a valid .NET assembly.")
                Return 1
            End If

            Console.WriteLine($"✓ Assembly loaded successfully: {tempDesigner.Assembly.Name}")
            'Console.WriteLine($"  Type: {If(assembly.is, "Executable", "Library")}")
            Console.WriteLine($"  Architecture: {tempDesigner.Assembly.Machine}")
            Console.WriteLine()

            ' Cargar preset
            Dim preset As ProtectionPreset = Nothing

            If Not String.IsNullOrEmpty(args.PresetFile) Then
                Console.WriteLine($"Loading preset from file: {args.PresetFile}")
                preset = PresetManager.ImportPreset(args.PresetFile)
            Else
                Console.WriteLine($"Loading preset: {args.PresetName}")
                preset = PresetManager.LoadPreset(args.PresetName)
            End If

            If preset Is Nothing Then
                Console.WriteLine("ERROR: Failed to load preset.")
                Return 1
            End If

            Console.WriteLine($"✓ Preset loaded: {preset.Name}")
            Console.WriteLine($"  Description: {preset.Description}")
            Console.WriteLine()

            ' Aplicar preset al formulario temporal
            PresetManager.ApplyPresetToForm(preset, tempDesigner)

            Console.WriteLine("Starting protection process...")
            Console.WriteLine("-----------------------------")

            ' Obtener configuración de protecciones
            Dim protections As List(Of HydraEngine.Models.Protection) = tempDesigner.MakeConfig()

            'Console.Clear()
            tempDesigner.BuildButton.Enabled = False
            tempDesigner.Build(tempDesigner.Assembly, tempDesigner.assemblyResolver, protections)

            Console.WriteLine($"✓ {protections.Count} protections configured")
            Console.WriteLine()

            Dim success As Boolean = False

            For i = 0 To 2
                Console.Title = $"Hydra.NET Console Mode - Protection in progress... ({tempDesigner.Guna2ProgressBar1.Value}%)"
                If tempDesigner.BuildButton.Enabled = True Then
                    success = True
                    Exit For
                End If

                Application.DoEvents()
                i -= 1
            Next

            If success Then
                Console.WriteLine()
                Console.WriteLine("✓ Protection completed successfully!")
                Console.WriteLine($"✓ Output file: {args.OutputFile}")

                If File.Exists(args.OutputFile) Then
                    Dim outputInfo As New FileInfo(args.OutputFile)
                    Console.WriteLine($"✓ File size: {FormatFileSize(outputInfo.Length)}")
                End If

                Return 0
            Else
                Console.WriteLine()
                Console.WriteLine("✗ Protection failed!")
                Return 1
            End If
        Catch ex As Exception
            Console.WriteLine($"ERROR: {ex.Message}")
            If args.ExecutionModes = CommandLineArgs.ExecutionMode.Console Then
                Console.WriteLine()
                Console.WriteLine("Stack trace:")
                Console.WriteLine(ex.StackTrace)
            End If
            Return 1
        End Try
    End Function

    Private Shared Function FormatFileSize(bytes As Long) As String
        If bytes < 1024 Then
            Return $"{bytes} bytes"
        ElseIf bytes < 1024 * 1024 Then
            Return $"{Math.Round(bytes / 1024.0, 1)} KB"
        Else
            Return $"{Math.Round(bytes / (1024.0 * 1024.0), 1)} MB"
        End If
    End Function

End Class