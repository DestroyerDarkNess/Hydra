Imports System.IO

Public Class CommandLineArgs

    Public Property InputFile As String = ""
    Public Property PresetName As String = ""
    Public Property PresetFile As String = ""
    Public Property ExecutionModes As ExecutionMode = ExecutionMode.GUI
    Public Property OutputFile As String = ""
    Public Property ShowHelp As Boolean = False
    Public Property IsValid As Boolean = False
    Public Property ErrorMessage As String = ""

    Public Enum ExecutionMode
        GUI         ' Show full GUI interface
        Console     ' Show only console, hide GUI
        Hidden      ' Run completely hidden
    End Enum

    Public Shared Function Parse(args() As String) As CommandLineArgs
        Dim result As New CommandLineArgs()

        If args Is Nothing OrElse args.Length = 0 Then
            result.ExecutionModes = ExecutionMode.GUI
            result.IsValid = True
            Return result
        End If

        Try
            Dim i As Integer = 0
            While i < args.Length
                Dim arg As String = args(i).ToLower()

                Select Case arg
                    Case "-file", "--file", "-f"
                        If i + 1 < args.Length Then
                            result.InputFile = args(i + 1)
                            i += 1
                        Else
                            result.ErrorMessage = "Missing value for -file argument"
                            Return result
                        End If

                    Case "-preset", "--preset", "-p"
                        If i + 1 < args.Length Then
                            result.PresetName = args(i + 1)
                            i += 1
                        Else
                            result.ErrorMessage = "Missing value for -preset argument"
                            Return result
                        End If

                    Case "-preset-file", "--preset-file", "-pf"
                        If i + 1 < args.Length Then
                            result.PresetFile = args(i + 1)
                            i += 1
                        Else
                            result.ErrorMessage = "Missing value for -preset-file argument"
                            Return result
                        End If

                    Case "-mode", "--mode", "-m"
                        If i + 1 < args.Length Then
                            Dim mode As String = args(i + 1).ToLower()
                            Select Case mode
                                Case "gui", "interface", "window"
                                    result.ExecutionModes = ExecutionMode.GUI
                                Case "console", "cmd", "terminal"
                                    result.ExecutionModes = ExecutionMode.Console
                                Case "hidden", "invisible", "silent"
                                    result.ExecutionModes = ExecutionMode.Hidden
                                Case Else
                                    result.ErrorMessage = $"Invalid execution mode: {args(i + 1)}. Valid modes: gui, console, hidden"
                                    Return result
                            End Select
                            i += 1
                        Else
                            result.ErrorMessage = "Missing value for -mode argument"
                            Return result
                        End If

                    Case "-output", "--output", "-o"
                        If i + 1 < args.Length Then
                            result.OutputFile = args(i + 1)
                            i += 1
                        Else
                            result.ErrorMessage = "Missing value for -output argument"
                            Return result
                        End If

                    Case "-help", "--help", "-h", "/?"
                        result.ShowHelp = True
                        result.IsValid = True
                        Return result

                    Case Else
                        result.ErrorMessage = $"Unknown argument: {args(i)}"
                        Return result
                End Select

                i += 1
            End While

            ' Validar argumentos
            If Not String.IsNullOrEmpty(result.InputFile) Then
                ' Modo comando - validar requerimientos
                If Not File.Exists(result.InputFile) Then
                    result.ErrorMessage = $"Input file does not exist: {result.InputFile}"
                    Return result
                End If

                If String.IsNullOrEmpty(result.PresetName) AndAlso String.IsNullOrEmpty(result.PresetFile) Then
                    result.ErrorMessage = "Either -preset or -preset-file must be specified when using -file"
                    Return result
                End If

                If Not String.IsNullOrEmpty(result.PresetFile) AndAlso Not File.Exists(result.PresetFile) Then
                    result.ErrorMessage = $"Preset file does not exist: {result.PresetFile}"
                    Return result
                End If

                ' Si no se especifica output, generar uno automático
                If String.IsNullOrEmpty(result.OutputFile) Then
                    Dim dir As String = Path.GetDirectoryName(result.InputFile)
                    Dim nameWithoutExt As String = Path.GetFileNameWithoutExtension(result.InputFile)
                    Dim ext As String = Path.GetExtension(result.InputFile)
                    result.OutputFile = Path.Combine(dir, $"{nameWithoutExt}_Protected{ext}")
                End If
            End If

            result.IsValid = True
        Catch ex As Exception
            result.ErrorMessage = $"Error parsing arguments: {ex.Message}"
        End Try

        Return result
    End Function

    Public Shared Sub ShowHelps()
        Console.WriteLine("Hydra.NET - Command Line Usage")
        Console.WriteLine("==============================")
        Console.WriteLine()
        Console.WriteLine("GUI Mode (default):")
        Console.WriteLine("  Hydra.exe")
        Console.WriteLine()
        Console.WriteLine("Command Line Mode:")
        Console.WriteLine("  Hydra.exe -file <input> -preset <name> [options]")
        Console.WriteLine("  Hydra.exe -file <input> -preset-file <json> [options]")
        Console.WriteLine()
        Console.WriteLine("Arguments:")
        Console.WriteLine("  -file, -f          Input .NET assembly (.exe or .dll)")
        Console.WriteLine("  -preset, -p        Preset name (Basic, Advanced, Maximum, Renaming Only)")
        Console.WriteLine("  -preset-file, -pf  Path to preset JSON file")
        Console.WriteLine("  -output, -o        Output file path (optional)")
        Console.WriteLine("  -mode, -m          Execution mode:")
        Console.WriteLine("                     gui      - Show full interface (default)")
        Console.WriteLine("                     console  - Show console only, hide GUI")
        Console.WriteLine("                     hidden   - Run completely hidden")
        Console.WriteLine("  -help, -h          Show this help")
        Console.WriteLine()
        Console.WriteLine("Examples:")
        Console.WriteLine("  Hydra.exe -file MyApp.exe -preset Basic -mode console")
        Console.WriteLine("  Hydra.exe -file MyDll.dll -preset-file custom.json -output MyDll_Secured.dll")
        Console.WriteLine("  Hydra.exe -file Target.exe -preset Maximum -mode hidden")
        Console.WriteLine()
    End Sub

End Class