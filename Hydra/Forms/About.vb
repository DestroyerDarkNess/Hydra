Public Class About

    Private Sub About_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Core.Instances.MainInstance IsNot Nothing Then Me.Location = Core.Helpers.Utils.CenterForm(Core.Instances.MainInstance, Me, Me.Location)
        Label6.Text = "v" & Core.Instances.FileVer
    End Sub

    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs) Handles Guna2Button1.Click
        Me.Close()
    End Sub

    Private Sub Guna2Button13_Click(sender As Object, e As EventArgs) Handles Guna2Button13.Click
        Process.Start("https://toolslib.net/downloads/viewdownload/600-hydranet/files/2489/")
    End Sub

End Class