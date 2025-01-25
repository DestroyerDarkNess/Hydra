Public Class MessageDialog

    Private Sub MessageDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Core.Instances.MainInstance IsNot Nothing Then Me.Location = Core.Helpers.Utils.CenterForm(Core.Instances.MainInstance, Me, Me.Location)

        StatusLabel.Font = New System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
    End Sub


    Public Shared Sub ShowAsync(ByVal MessageText As String, Optional ByVal TitleText As String = "DIH Pro", Optional ByVal ColorEx As Color = Nothing, Optional ByVal ButtonMessage As MessageBoxButtons = MessageBoxButtons.OK)
        Dim MsgInstance As New MessageDialog
        Dim ShowEx As Task(Of DialogResult) = MsgInstance.ShowDialogAsync(MessageText, TitleText, ColorEx, ButtonMessage)
    End Sub

    Dim ResultEx As DialogResult = Nothing
    Dim ClickedResult As Boolean = False

    Public Async Function ShowDialogAsync(ByVal MessageText As String, Optional ByVal TitleText As String = "DIH Pro", Optional ByVal ColorEx As Color = Nothing, Optional ByVal ButtonMessage As MessageBoxButtons = MessageBoxButtons.OK) As Task(Of DialogResult)

        StatusLabel.Text = MessageText
        Label2.Text = TitleText

        If Not ColorEx = Nothing Then
            Guna2Panel1.BorderColor = ColorEx
            Label2.BackColor = ColorEx
        End If

        If ButtonMessage = MessageBoxButtons.OK Then
            Guna2Button1.Visible = False
            Guna2Button3.Location = Guna2Button1.Location
            ResultEx = DialogResult.Cancel
        ElseIf ButtonMessage = MessageBoxButtons.YesNo Then
            Guna2Button3.Text = "Yes"
            Guna2Button1.Text = "No"
            ResultEx = DialogResult.No
        End If

        Me.TopMost = True
        Me.Show()

        For i As Integer = 0 To 2
            If ClickedResult = True Then
                Exit For
            End If
            Application.DoEvents()
            i -= 1
        Next

        Me.Close()
        Return ResultEx
    End Function

    Public Sub MakeDialog(ByVal MessageText As String, Optional ByVal TitleText As String = "DIH Pro", Optional ByVal ColorEx As Color = Nothing, Optional ByVal ButtonMessage As MessageBoxButtons = MessageBoxButtons.OK)

        StatusLabel.Text = MessageText
        Label2.Text = TitleText

        If Not ColorEx = Nothing Then
            Guna2Panel1.BorderColor = ColorEx
            Label2.BackColor = ColorEx
        End If

        If ButtonMessage = MessageBoxButtons.OK Then
            Guna2Button1.Visible = False
            Guna2Button3.Location = Guna2Button1.Location
        ElseIf ButtonMessage = MessageBoxButtons.YesNo Then
            Guna2Button3.Text = "Yes"
            Guna2Button1.Text = "No"
        End If

        Me.TopMost = True

    End Sub

    Private Sub Guna2Button3_Click(sender As Object, e As EventArgs) Handles Guna2Button3.Click
        If ResultEx = Nothing Then
            ResultEx = DialogResult.OK
            Me.DialogResult = ResultEx
            Me.Close()
        Else
            If ResultEx = DialogResult.Cancel Then
                ResultEx = DialogResult.OK
            Else
                ResultEx = DialogResult.Yes
            End If
            ClickedResult = True
        End If
    End Sub

    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs) Handles Guna2Button1.Click
        If ResultEx = Nothing Then
            ResultEx = DialogResult.Cancel
            Me.DialogResult = ResultEx
            Me.Close()
        Else
            ClickedResult = True
        End If
    End Sub

    Private Sub MessageDialog_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing

    End Sub

End Class