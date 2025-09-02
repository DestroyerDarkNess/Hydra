Imports Hydra.Core

Public Class MainWindow

    Public ProjectDesignerForm As ProjectDesigner = Nothing
    Public Property CommandLineArgs As CommandLineArgs = Nothing

    Private Sub MainWindow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StartRutines()

        ' Si hay argumentos de línea de comandos, cargar archivo automáticamente
        If CommandLineArgs IsNot Nothing AndAlso Not String.IsNullOrEmpty(CommandLineArgs.InputFile) Then
            LoadFileFromCommandLine()
        End If
    End Sub

    Public Sub StartRutines()
        Guna2ShadowForm1.SetShadowForm(Me)
        Core.Instances.MainInstance = Me

        'Guna2Panel2.Visible = False
        'Guna2Panel3.Visible = True
        'Guna2Panel3.BringToFront()
        'ProjectDesignerForm = New ProjectDesigner With {.TopLevel = False, .Visible = True, .Name = "LoginUI"}
        'AddHandler ProjectDesignerForm.FormClosed, AddressOf Project_Closed
        'ProjectDesignerForm.Dock = DockStyle.Fill
        'ProjectDesignerForm.LoadFile("D:\test\BIllsTheOG.exe")
        'ProjectDesignerForm.LoadFile("C:\Users\s4lsa\source\repos\MusiCloud\MusiCloud\bin\Debug\MusiCloud.exe")
        'ProjectDesignerForm.LoadFile("C:\Users\s4lsa\Downloads\CV_PREMIUM\CV_PREMIUM.exe")
        'ProjectDesignerForm.LoadFile("C:\Users\s4lsa\AppData\Local\Programs\QuickBeat\QuickBeat.exe")
        'ProjectDesignerForm.LoadFile("C:\Users\s4lsa\source\repos\dumptest\dumptest\bin\Debug\dumptest.exe")
        'ProjectDesignerForm.LoadFile("C:\Users\s4lsa\source\repos\ClickableTransparentOverlay\Examples\MultiThreadedOverlay\bin\Debug\net7.0\MultiThreadedOverlay.exe")
        'Guna2Panel3.Controls.Add(ProjectDesignerForm)
    End Sub

    Private Sub OpenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenToolStripMenuItem.Click
        Open()
    End Sub

    Private Sub Open()
        Dim FilesSelected As List(Of String) = Core.Helpers.Utils.OpenFile("Executable Files|*.exe|DLL Files|*.dll")

        If FilesSelected IsNot Nothing Then
            Clear()

            Dim FilePath As String = FilesSelected.Item(0)
            Guna2Panel2.Visible = False
            Guna2Panel3.Visible = True
            Guna2Panel3.BringToFront()

            ProjectDesignerForm = New ProjectDesigner With {.TopLevel = False, .Visible = True, .Name = "LoginUI"}
            AddHandler ProjectDesignerForm.FormClosed, AddressOf Project_Closed
            ProjectDesignerForm.Dock = DockStyle.Fill
            ProjectDesignerForm.LoadFile(FilePath)

            Guna2Panel3.Controls.Add(ProjectDesignerForm)

        End If
    End Sub

    Private Sub CloseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CloseToolStripMenuItem.Click
        Clear()
    End Sub

    Public Sub Clear()
        For Each ControlEx As Control In Guna2Panel3.Controls
            If TypeOf ControlEx Is ProjectDesigner Then
                Dim ProyectControl As ProjectDesigner = ControlEx
                Guna2Panel3.Controls.Remove(ProyectControl)
                ProyectControl.Dispose()
            End If
        Next
        PresetsComboBox.Visible = False
        Guna2Panel3.Visible = False
        Guna2Panel3.SendToBack()
        Guna2Panel2.Visible = True
        Console.Clear()
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Process.GetCurrentProcess.Kill()
    End Sub

    Private Sub HomePageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HomePageToolStripMenuItem.Click
        Process.Start("https://toolslib.net/downloads/viewdownload/600-hydranet/")
    End Sub

    Dim DiagAbout As New Hydra.About

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        Guna2Panel2.Visible = False
        DiagAbout.ShowDialog()
        Guna2Panel2.Visible = True
    End Sub

    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs) Handles Guna2Button1.Click
        Process.GetCurrentProcess.Kill()
    End Sub

    Private Sub Guna2Button13_Click(sender As Object, e As EventArgs) Handles Guna2Button13.Click
        Open()
    End Sub

    Private Sub Project_Closed(sender As Object, e As EventArgs)
        Clear()
    End Sub

    Private Sub LogInLabel5_Click(sender As Object, e As EventArgs) Handles LogInLabel5.Click, LogInLabel6.Click, PictureBox1.Click
        Process.Start("https://discord.gg/C4evgU4Tas")
    End Sub

    Private Sub PresetsComboBox_VisibleChanged(sender As Object, e As EventArgs) Handles PresetsComboBox.VisibleChanged
        Guna2Button2.Visible = PresetsComboBox.Visible
    End Sub

    Private Sub Guna2Button2_Click(sender As Object, e As EventArgs) Handles Guna2Button2.Click
        If (ProjectDesignerForm Is Nothing) Then Exit Sub
        ProjectDesignerForm.OpenPresetManager()
    End Sub

    Private Sub PresetsComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles PresetsComboBox.SelectedIndexChanged
        If (ProjectDesignerForm Is Nothing) Then Exit Sub
        Dim SelectedTextValue = PresetsComboBox.Items(PresetsComboBox.SelectedIndex).ToString
        ProjectDesignerForm.LoadSelectedPreset(SelectedTextValue)
    End Sub

    Private Sub LoadFileFromCommandLine()
        Try
            If CommandLineArgs Is Nothing OrElse String.IsNullOrEmpty(CommandLineArgs.InputFile) Then
                Return
            End If

            ' Verificar que el archivo existe
            If Not IO.File.Exists(CommandLineArgs.InputFile) Then
                MessageBox.Show($"File not found: {CommandLineArgs.InputFile}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Cargar el archivo usando la lógica existente
            Clear()

            Guna2Panel2.Visible = False
            Guna2Panel3.Visible = True
            Guna2Panel3.BringToFront()

            ProjectDesignerForm = New ProjectDesigner With {.TopLevel = False, .Visible = True, .Name = "LoginUI"}
            AddHandler ProjectDesignerForm.FormClosed, AddressOf Project_Closed
            ProjectDesignerForm.Dock = DockStyle.Fill
            ProjectDesignerForm.LoadFile(CommandLineArgs.InputFile)

            Guna2Panel3.Controls.Add(ProjectDesignerForm)

            ' Si hay un preset especificado, aplicarlo después de cargar
            If Not String.IsNullOrEmpty(CommandLineArgs.PresetName) Then
                ' Esperar un poco para que se cargue completamente el ProjectDesigner
                Dim timer As New Timer()
                timer.Interval = 1000
                AddHandler timer.Tick, Sub(s, e)
                                           timer.Stop()
                                           timer.Dispose()
                                           ApplyCommandLinePreset()
                                       End Sub
                timer.Start()
            ElseIf Not String.IsNullOrEmpty(CommandLineArgs.PresetFile) Then
                ' Cargar preset desde archivo
                Dim timer As New Timer()
                timer.Interval = 1000
                AddHandler timer.Tick, Sub(s, e)
                                           timer.Stop()
                                           timer.Dispose()
                                           ApplyCommandLinePresetFromFile()
                                       End Sub
                timer.Start()
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading file from command line: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ApplyCommandLinePreset()
        Try
            If ProjectDesignerForm IsNot Nothing AndAlso Not String.IsNullOrEmpty(CommandLineArgs.PresetName) Then
                ProjectDesignerForm.LoadSelectedPreset(CommandLineArgs.PresetName)

                ' Si hay archivo de salida especificado, configurarlo
                If Not String.IsNullOrEmpty(CommandLineArgs.OutputFile) Then
                    ProjectDesignerForm.OutputTextBox.Text = CommandLineArgs.OutputFile
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error applying preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ApplyCommandLinePresetFromFile()
        Try
            If ProjectDesignerForm IsNot Nothing AndAlso Not String.IsNullOrEmpty(CommandLineArgs.PresetFile) Then
                Dim preset As ProtectionPreset = PresetManager.ImportPreset(CommandLineArgs.PresetFile)
                If preset IsNot Nothing Then
                    PresetManager.ApplyPresetToForm(preset, ProjectDesignerForm)

                    ' Si hay archivo de salida especificado, configurarlo
                    If Not String.IsNullOrEmpty(CommandLineArgs.OutputFile) Then
                        ProjectDesignerForm.OutputTextBox.Text = CommandLineArgs.OutputFile
                    End If
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error applying preset from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Class