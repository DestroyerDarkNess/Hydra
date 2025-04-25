Public Class MainWindow

    Public ProjectDesignerForm As ProjectDesigner = Nothing

    Private Sub MainWindow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StartRutines()
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

        Guna2Panel3.Visible = False
        Guna2Panel3.SendToBack()
        Guna2Panel2.Visible = True
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

End Class