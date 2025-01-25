Public Class MenuStripZ
    Inherits MenuStrip

    Public Sub New()
        Me.Renderer = New MyMenuRenderer()
    End Sub
End Class

Public Class MyMenuRenderer
    Inherits ToolStripRenderer

    Protected Overrides Sub OnRenderMenuItemBackground(ByVal e As ToolStripItemRenderEventArgs)
        MyBase.OnRenderMenuItemBackground(e)

        If e.Item.Enabled Then

            If e.Item.IsOnDropDown = False AndAlso e.Item.Selected Then
                Dim rect = New Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1)
                Dim rect2 = New Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1)
                e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(60, 60, 60)), rect)
                e.Graphics.DrawRectangle(New Pen(New SolidBrush(Color.Black)), rect2)
                e.Item.ForeColor = Color.White
            Else
                e.Item.ForeColor = Color.White
            End If

            If e.Item.IsOnDropDown AndAlso e.Item.Selected Then
                Dim rect = New Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1)
                e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(60, 60, 60)), rect)
                e.Graphics.DrawRectangle(New Pen(New SolidBrush(Color.Black)), rect)
                e.Item.ForeColor = Color.White
            End If

            If (TryCast(e.Item, ToolStripMenuItem)).DropDown.Visible AndAlso e.Item.IsOnDropDown = False Then
                Dim rect = New Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1)
                Dim rect2 = New Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1)
                e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(20, 20, 20)), rect)
                e.Graphics.DrawRectangle(New Pen(New SolidBrush(Color.Black)), rect2)
                e.Item.ForeColor = Color.White
            End If
        End If
    End Sub

    Protected Overrides Sub OnRenderSeparator(ByVal e As ToolStripSeparatorRenderEventArgs)
        MyBase.OnRenderSeparator(e)
        Dim DarkLine = New SolidBrush(Color.FromArgb(30, 30, 30))
        Dim rect = New Rectangle(30, 3, e.Item.Width - 30, 1)
        e.Graphics.FillRectangle(DarkLine, rect)
    End Sub

    Protected Overrides Sub OnRenderItemCheck(ByVal e As ToolStripItemImageRenderEventArgs)
        MyBase.OnRenderItemCheck(e)

        If e.Item.Selected Then
            Dim rect = New Rectangle(4, 2, 18, 18)
            Dim rect2 = New Rectangle(5, 3, 16, 16)
            Dim b As SolidBrush = New SolidBrush(Color.Black)
            Dim b2 As SolidBrush = New SolidBrush(Color.FromArgb(220, 220, 220))
            e.Graphics.FillRectangle(b, rect)
            e.Graphics.FillRectangle(b2, rect2)
            e.Graphics.DrawImage(e.Image, New Point(5, 3))
        Else
            Dim rect = New Rectangle(4, 2, 18, 18)
            Dim rect2 = New Rectangle(5, 3, 16, 16)
            Dim b As SolidBrush = New SolidBrush(Color.White)
            Dim b2 As SolidBrush = New SolidBrush(Color.FromArgb(255, 80, 90, 90))
            e.Graphics.FillRectangle(b, rect)
            e.Graphics.FillRectangle(b2, rect2)
            e.Graphics.DrawImage(e.Image, New Point(5, 3))
        End If
    End Sub

    Protected Overrides Sub OnRenderImageMargin(ByVal e As ToolStripRenderEventArgs)
        MyBase.OnRenderImageMargin(e)
        Dim rect = New Rectangle(0, 0, e.ToolStrip.Width, e.ToolStrip.Height)
        e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(20, 20, 20)), rect)
        Dim DarkLine = New SolidBrush(Color.FromArgb(20, 20, 20))
        Dim rect3 = New Rectangle(0, 0, 26, e.AffectedBounds.Height)
        e.Graphics.FillRectangle(DarkLine, rect3)
        e.Graphics.DrawLine(New Pen(New SolidBrush(Color.FromArgb(20, 20, 20))), 28, 0, 28, e.AffectedBounds.Height)
        Dim rect2 = New Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1)
        e.Graphics.DrawRectangle(New Pen(New SolidBrush(Color.Black)), rect2)
    End Sub
End Class