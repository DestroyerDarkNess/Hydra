Imports System.Reflection

Public Class DLLItem

    Public Property DllPath As String = String.Empty

    Public Property Info As String = String.Empty

    Private Sub DLLItem_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim DllName As String = IO.Path.GetFileName(DllPath)
        LogInLabel2.Text = DllName
        LogInLabel3.Text = Info
    End Sub

    Public Function IsEnabled() As Boolean
        Return Guna2CheckBox3.Checked
    End Function

    Public Function Libz() As Boolean
        Return (Guna2ComboBox1.SelectedIndex = 0)
    End Function

    Public Function Embed() As Boolean
        Return (Guna2ComboBox1.SelectedIndex = 1)
    End Function

    Public Function Merged() As Boolean
        Return (Guna2ComboBox1.SelectedIndex = 2)
    End Function

End Class
