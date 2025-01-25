' // Booster Theme GDI+
' // 9 Controls
' // Made by AeroRev9 / Naywyn
' // 02/22.

Option Strict On

Imports System.Threading
Imports System.Drawing.Text
Imports System.Drawing.Drawing2D
Imports System.ComponentModel

Friend Module HelpersXylos


    Public Sub CenterString(G As Graphics, T As String, F As Font, C As Color, R As Rectangle)
        Dim TS As SizeF = G.MeasureString(T, F)

        Using B As New SolidBrush(C)
            G.DrawString(T, F, B, New Point(CInt(R.Width / 2 - (TS.Width / 2)), CInt(R.Height / 2 - (TS.Height / 2))))
        End Using
    End Sub

    Public Function ColorFromHex(Hex As String) As Color
        Return Color.FromArgb(CInt(Long.Parse(String.Format("FFFFFFFFFF{0}", Hex.Substring(1)), Globalization.NumberStyles.HexNumber)))
    End Function

    Public Function FullRectangle(S As Size, Subtract As Boolean) As Rectangle

        If Subtract Then
            Return New Rectangle(0, 0, S.Width - 1, S.Height - 1)
        Else
            Return New Rectangle(0, 0, S.Width, S.Height)
        End If

    End Function

    Public Function RoundRect1(ByVal Rect As Rectangle, ByVal Rounding As Integer, Optional ByVal Style As RoundingStyle = RoundingStyle.All) As Drawing2D.GraphicsPath

        Dim GP As New Drawing2D.GraphicsPath()
        Dim AW As Integer = Rounding * 2

        GP.StartFigure()

        If Rounding = 0 Then
            GP.AddRectangle(Rect)
            GP.CloseAllFigures()
            Return GP
        End If

        Select Case Style
            Case RoundingStyle.All
                GP.AddArc(New Rectangle(Rect.X, Rect.Y, AW, AW), -180, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
                GP.AddArc(New Rectangle(Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 90, 90)
            Case RoundingStyle.Top
                GP.AddArc(New Rectangle(Rect.X, Rect.Y, AW, AW), -180, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddLine(New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y + Rect.Height))
            Case RoundingStyle.Bottom
                GP.AddLine(New Point(Rect.X, Rect.Y), New Point(Rect.X + Rect.Width, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
                GP.AddArc(New Rectangle(Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 90, 90)
            Case RoundingStyle.Left
                GP.AddArc(New Rectangle(Rect.X, Rect.Y, AW, AW), -180, 90)
                GP.AddLine(New Point(Rect.X + Rect.Width, Rect.Y), New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height))
                GP.AddArc(New Rectangle(Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 90, 90)
            Case RoundingStyle.Right
                GP.AddLine(New Point(Rect.X, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
            Case RoundingStyle.TopRight
                GP.AddLine(New Point(Rect.X, Rect.Y + 1), New Point(Rect.X, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddLine(New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height - 1), New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height))
                GP.AddLine(New Point(Rect.X + 1, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y + Rect.Height))
            Case RoundingStyle.BottomRight
                GP.AddLine(New Point(Rect.X, Rect.Y + 1), New Point(Rect.X, Rect.Y))
                GP.AddLine(New Point(Rect.X + Rect.Width - 1, Rect.Y), New Point(Rect.X + Rect.Width, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
                GP.AddLine(New Point(Rect.X + 1, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y + Rect.Height))
        End Select

        GP.CloseAllFigures()

        Return GP

    End Function

End Module

Public Class XylosTabControl
    Inherits TabControl

    Private G As Graphics
    Private Rect As Rectangle
    Private _OverIndex As Integer = -1

    Public Property FirstHeaderBorder As Boolean

    Private Property OverIndex As Integer
        Get
            Return _OverIndex
        End Get
        Set(value As Integer)
            _OverIndex = value
            Invalidate()
        End Set
    End Property

    Sub New()
        DoubleBuffered = True
        Alignment = TabAlignment.Left
        SizeMode = TabSizeMode.Fixed
        ItemSize = New Size(40, 180)
    End Sub

    Protected Overrides Sub OnCreateControl()
        MyBase.OnCreateControl()
        SetStyle(ControlStyles.UserPaint, True)
    End Sub

    Protected Overrides Sub OnControlAdded(e As ControlEventArgs)
        MyBase.OnControlAdded(e)
        e.Control.BackColor = Color.FromArgb(34, 35, 41) ' Color.White
        e.Control.ForeColor = HelpersXylos.ColorFromHex("#7C858E")
        e.Control.Font = New Font("Segoe UI", 9)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(HelpersXylos.ColorFromHex("#FFFFFF"))

        For I As Integer = 0 To TabPages.Count - 1

            Rect = GetTabRect(I)

            If String.IsNullOrEmpty(CType(TabPages(I).Tag, String)) Then

                If SelectedIndex = I Then

                    Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#3375ED")), TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#BECCD9")), TextFont As New Font("Segoe UI semibold", 9)
                        G.FillRectangle(Background, New Rectangle(Rect.X - 5, Rect.Y + 1, Rect.Width + 7, Rect.Height))
                        G.DrawString(TabPages(I).Text, TextFont, TextColor, New Point(Rect.X + 50 + (ItemSize.Height - 180), Rect.Y + 12))
                    End Using

                Else

                    Using TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#919BA6")), TextFont As New Font("Segoe UI semibold", 9)
                        G.DrawString(TabPages(I).Text, TextFont, TextColor, New Point(Rect.X + 50 + (ItemSize.Height - 180), Rect.Y + 12))
                    End Using

                End If

                If Not OverIndex = -1 And Not SelectedIndex = OverIndex Then

                    Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#2F3338")), TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#919BA6")), TextFont As New Font("Segoe UI semibold", 9)
                        G.FillRectangle(Background, New Rectangle(GetTabRect(OverIndex).X - 5, GetTabRect(OverIndex).Y + 1, GetTabRect(OverIndex).Width + 7, GetTabRect(OverIndex).Height))
                        G.DrawString(TabPages(OverIndex).Text, TextFont, TextColor, New Point(GetTabRect(OverIndex).X + 50 + (ItemSize.Height - 180), GetTabRect(OverIndex).Y + 12))
                    End Using

                    If Not IsNothing(ImageList) Then
                        If Not TabPages(OverIndex).ImageIndex < 0 Then
                            G.DrawImage(ImageList.Images(TabPages(OverIndex).ImageIndex), New Rectangle(GetTabRect(OverIndex).X + 25 + (ItemSize.Height - 180), CInt(GetTabRect(OverIndex).Y + ((GetTabRect(OverIndex).Height / 2) - 9)), 16, 16))
                        End If
                    End If

                End If


                If Not IsNothing(ImageList) Then
                    If Not TabPages(I).ImageIndex < 0 Then
                        G.DrawImage(ImageList.Images(TabPages(I).ImageIndex), New Rectangle(Rect.X + 25 + (ItemSize.Height - 180), CInt(Rect.Y + ((Rect.Height / 2) - 9)), 16, 16))
                    End If
                End If

            Else

                Using TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#6A7279")), TextFont As New Font("Segoe UI", 7, FontStyle.Bold), Border As New Pen(HelpersXylos.ColorFromHex("#2B2F33"))

                    If FirstHeaderBorder Then
                        G.DrawLine(Border, New Point(Rect.X - 5, Rect.Y + 1), New Point(Rect.Width + 7, Rect.Y + 1))
                    Else
                        If Not I = 0 Then
                            G.DrawLine(Border, New Point(Rect.X - 5, Rect.Y + 1), New Point(Rect.Width + 7, Rect.Y + 1))
                        End If
                    End If

                    G.DrawString(TabPages(I).Text.ToUpper, TextFont, TextColor, New Point(Rect.X + 25 + (ItemSize.Height - 180), Rect.Y + 16))

                End Using

            End If

        Next

    End Sub

    Protected Overrides Sub OnSelecting(e As TabControlCancelEventArgs)
        MyBase.OnSelecting(e)

        If Not IsNothing(e.TabPage) Then
            If Not String.IsNullOrEmpty(CType(e.TabPage.Tag, String)) Then
                e.Cancel = True
            Else
                OverIndex = -1
            End If
        End If

    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)

        For I As Integer = 0 To TabPages.Count - 1
            If GetTabRect(I).Contains(e.Location) And Not SelectedIndex = I And String.IsNullOrEmpty(CType(TabPages(I).Tag, String)) Then
                OverIndex = I
                Exit For
            Else
                OverIndex = -1
            End If
        Next

    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        OverIndex = -1
    End Sub

End Class

Public Class XylosCheckBox
    Inherits Control

    Public Event CheckedChanged(sender As Object, e As EventArgs)

    Private _Checked As Boolean
    Private _EnabledCalc As Boolean
    Private G As Graphics

    Private B64Enabled As String = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAA00lEQVQ4T6WTwQ2CMBSG30/07Ci6gY7gxZoIiYADuAIrsIDpQQ/cHMERZBOuXHimDSWALYL01EO/L//724JmLszk6S+BCOIExFsmL50sEH4kAZxVciYuJgnacD16Plpgg8tFtYMILntQdSXiZ3aXqa1UF/yUsoDw4wKglQaZZPa4RW3JEKzO4RjEbyJaN1BL8gvWgsMp3ADeq0lRJ2FimLZNYWpmFbudUJdolXTLyG2wTmDODUiccEfgSDIIfwmMxAMStS+XHPZn7l/z6Ifk+nSzBR8zi2d9JmVXSgAAAABJRU5ErkJggg=="
    Private B64Disabled As String = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAA1UlEQVQ4T6WTzQ2CQBCF56EnLpaiXvUAJBRgB2oFtkALdEAJnoVEMIGzdEIFjNkFN4DLn+xpD/N9efMWQAsPFvL0lyBMUg8MiwzyZwuiJAuI6CyTMxezBC24EuSTBTp4xaaN6JWdqKQbge6udfB1pfbBjrMvEMZZAdCm3ilw7eO1KRmCxRyiOH0TsFUQs5KMwVLweKY7ALFKUZUTECD6qdquCxM7i9jNhLJEraQ5xZzrYJngO9crGYBbAm2SEfhHoCQGeeK+Ls1Ld+fuM0/+kPp+usWCD10idEOGa4QuAAAAAElFTkSuQmCC"

    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            _Checked = value
            Invalidate()
        End Set
    End Property

    Public Shadows Property Enabled As Boolean
        Get
            Return EnabledCalc
        End Get
        Set(value As Boolean)
            _EnabledCalc = value

            If Enabled Then
                Cursor = Cursors.Hand
            Else
                Cursor = Cursors.Default
            End If

            Invalidate()
        End Set
    End Property


    Public Property EnabledCalc As Boolean
        Get
            Return _EnabledCalc
        End Get
        Set(value As Boolean)
            Enabled = value
            Invalidate()
        End Set
    End Property

    Sub New()
        DoubleBuffered = True
        Enabled = True
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Color.White)

        If Enabled Then

            Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#F3F4F7")), Border As New Pen(HelpersXylos.ColorFromHex("#D0D5D9")), TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#7C858E")), TextFont As New Font("Segoe UI", 9)
                G.FillPath(Background, HelpersXylos.RoundRect1(New Rectangle(0, 0, 16, 16), 3))
                G.DrawPath(Border, HelpersXylos.RoundRect1(New Rectangle(0, 0, 16, 16), 3))
                G.DrawString(Text, TextFont, TextColor, New Point(25, 0))
            End Using

            If Checked Then

                Using I As Image = Image.FromStream(New IO.MemoryStream(Convert.FromBase64String(B64Enabled)))
                    G.DrawImage(I, New Rectangle(3, 3, 11, 11))
                End Using

            End If

        Else

            Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#F5F5F8")), Border As New Pen(HelpersXylos.ColorFromHex("#E1E1E2")), TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#D0D3D7")), TextFont As New Font("Segoe UI", 9)
                G.FillPath(Background, HelpersXylos.RoundRect1(New Rectangle(0, 0, 16, 16), 3))
                G.DrawPath(Border, HelpersXylos.RoundRect1(New Rectangle(0, 0, 16, 16), 3))
                G.DrawString(Text, TextFont, TextColor, New Point(25, 0))
            End Using

            If Checked Then

                Using I As Image = Image.FromStream(New IO.MemoryStream(Convert.FromBase64String(B64Disabled)))
                    G.DrawImage(I, New Rectangle(3, 3, 11, 11))
                End Using

            End If

        End If

    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)

        If Enabled Then
            Checked = Not Checked
            RaiseEvent CheckedChanged(Me, e)
        End If

    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        Size = New Size(Width, 18)
    End Sub

End Class

Public Class XylosRadioButton
    Inherits Control

    Public Event CheckedChanged(sender As Object, e As EventArgs)

    Private _Checked As Boolean
    Private _EnabledCalc As Boolean
    Private G As Graphics

    Public Property Checked As Boolean
        Get
            Return _Checked
        End Get
        Set(value As Boolean)
            _Checked = value
            Invalidate()
        End Set
    End Property

    Public Shadows Property Enabled As Boolean
        Get
            Return EnabledCalc
        End Get
        Set(value As Boolean)
            _EnabledCalc = value

            If Enabled Then
                Cursor = Cursors.Hand
            Else
                Cursor = Cursors.Default
            End If

            Invalidate()
        End Set
    End Property

    Public Property EnabledCalc As Boolean
        Get
            Return _EnabledCalc
        End Get
        Set(value As Boolean)
            Enabled = value
            Invalidate()
        End Set
    End Property

    Sub New()
        DoubleBuffered = True
        Enabled = True
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Color.White)

        If Enabled Then

            Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#F3F4F7")), Border As New Pen(HelpersXylos.ColorFromHex("#D0D5D9")), TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#7C858E")), TextFont As New Font("Segoe UI", 9)
                G.FillEllipse(Background, New Rectangle(0, 0, 16, 16))
                G.DrawEllipse(Border, New Rectangle(0, 0, 16, 16))
                G.DrawString(Text, TextFont, TextColor, New Point(25, 0))
            End Using

            If Checked Then

                Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#575C62"))
                    G.FillEllipse(Background, New Rectangle(4, 4, 8, 8))
                End Using

            End If

        Else

            Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#F5F5F8")), Border As New Pen(HelpersXylos.ColorFromHex("#E1E1E2")), TextColor As New SolidBrush(HelpersXylos.ColorFromHex("#D0D3D7")), TextFont As New Font("Segoe UI", 9)
                G.FillEllipse(Background, New Rectangle(0, 0, 16, 16))
                G.DrawEllipse(Border, New Rectangle(0, 0, 16, 16))
                G.DrawString(Text, TextFont, TextColor, New Point(25, 0))
            End Using

            If Checked Then

                Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#BCC1C6"))
                    G.FillEllipse(Background, New Rectangle(4, 4, 8, 8))
                End Using

            End If

        End If

    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)

        If Enabled Then

            For Each C As Control In Parent.Controls
                If TypeOf C Is XylosRadioButton Then
                    DirectCast(C, XylosRadioButton).Checked = False
                End If
            Next

            Checked = Not Checked
            RaiseEvent CheckedChanged(Me, e)
        End If

    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        Size = New Size(Width, 18)
    End Sub

End Class

Public Class XylosNotice
    Inherits TextBox

    Private G As Graphics
    Private B64 As String = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAABL0lEQVQ4T5VT0VGDQBB9e2cBdGBSgTIDEr9MCw7pI0kFtgB9yFiC+KWMmREqMOnAAuDWOfAiudzhyA/svtvH7Xu7BOv5eH2atVKtwbwk0LWGGVyDqLzoRB7e3u/HJTQOdm+PGYjWNuk4ZkIW36RbkzsS7KqiBnB1Usw49DHh8oQEXMfJKhwgAM4/Mw7RIp0NeLG3ScCcR4vVhnTPnVCf9rUZeImTdKnz71VREnBnn5FKzMnX95jA2V6vLufkBQFESTq0WBXsEla7owmcoC6QJMKW2oCUePY5M0lAjK0iBAQ8TBGc2/d7+uvnM/AQNF4Rp4bpiGkRfTb2Gigx12+XzQb3D9JfBGaQzHWm7HS000RJ2i/av5fJjPDZMplErwl1GxDpMTbL1YC5lCwze52/AQFekh7wKBpGAAAAAElFTkSuQmCC"

    Sub New()
        DoubleBuffered = True
        Enabled = False
        [ReadOnly] = True
        BorderStyle = BorderStyle.None
        Multiline = True
        Cursor = Cursors.Default
    End Sub

    Protected Overrides Sub OnCreateControl()
        MyBase.OnCreateControl()
        SetStyle(ControlStyles.UserPaint, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(MyBase.BackColor)
        'HelpersXylos.ColorFromHex("#FFFDE8")                  HelpersXylos.ColorFromHex("#F2F3F7")        HelpersXylos.ColorFromHex("#B9B595")
        Using Background As New SolidBrush(MyBase.BackColor), MainBorder As New Pen(MyBase.BackColor), TextColor As New SolidBrush(MyBase.ForeColor), TextFont As New Font("Segoe UI", 9)
            G.FillPath(Background, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
            G.DrawPath(MainBorder, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
            G.DrawString(Text, TextFont, TextColor, New Point(30, 6))
        End Using

        Using I As Image = Image.FromStream(New IO.MemoryStream(Convert.FromBase64String(B64)))
            G.DrawImage(I, New Rectangle(8, CInt(Height / 2 - 8), 16, 16))
        End Using

    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)

    End Sub

End Class

Public Class XylosTextBox
    Inherits Control

    Enum MouseState As Byte
        None = 0
        Over = 1
        Down = 2
    End Enum

    Private WithEvents TB As New TextBox
    Private G As Graphics
    Private State As MouseState
    Private IsDown As Boolean

    Private _EnabledCalc As Boolean
    Private _allowpassword As Boolean = False
    Private _maxChars As Integer = 32767
    Private _textAlignment As HorizontalAlignment
    Private _multiLine As Boolean = False
    Private _readOnly As Boolean = False

    Public Shadows Property Enabled As Boolean
        Get
            Return EnabledCalc
        End Get
        Set(value As Boolean)
            TB.Enabled = value
            _EnabledCalc = value
            Invalidate()
        End Set
    End Property

    Public Property EnabledCalc As Boolean
        Get
            Return _EnabledCalc
        End Get
        Set(value As Boolean)
            Enabled = value
            Invalidate()
        End Set
    End Property

    Public Shadows Property UseSystemPasswordChar() As Boolean
        Get
            Return _allowpassword
        End Get
        Set(ByVal value As Boolean)
            TB.UseSystemPasswordChar = UseSystemPasswordChar
            _allowpassword = value
            Invalidate()
        End Set
    End Property

    Public Shadows Property MaxLength() As Integer
        Get
            Return _maxChars
        End Get
        Set(ByVal value As Integer)
            _maxChars = value
            TB.MaxLength = MaxLength
            Invalidate()
        End Set
    End Property

    Public Shadows Property TextAlign() As HorizontalAlignment
        Get
            Return _textAlignment
        End Get
        Set(ByVal value As HorizontalAlignment)
            _textAlignment = value
            Invalidate()
        End Set
    End Property

    Public Shadows Property MultiLine() As Boolean
        Get
            Return _multiLine
        End Get
        Set(ByVal value As Boolean)
            _multiLine = value
            TB.Multiline = value
            OnResize(EventArgs.Empty)
            Invalidate()
        End Set
    End Property

    Public Shadows Property [ReadOnly]() As Boolean
        Get
            Return _readOnly
        End Get
        Set(ByVal value As Boolean)
            _readOnly = value
            If TB IsNot Nothing Then
                TB.ReadOnly = value
            End If
        End Set
    End Property

    Protected Overrides Sub OnTextChanged(ByVal e As EventArgs)
        MyBase.OnTextChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnBackColorChanged(ByVal e As EventArgs)
        MyBase.OnBackColorChanged(e)
        Invalidate()
    End Sub

    Protected Overrides Sub OnForeColorChanged(ByVal e As EventArgs)
        MyBase.OnForeColorChanged(e)
        TB.ForeColor = ForeColor
        Invalidate()
    End Sub

    Protected Overrides Sub OnFontChanged(ByVal e As EventArgs)
        MyBase.OnFontChanged(e)
        TB.Font = Font
    End Sub

    Protected Overrides Sub OnGotFocus(ByVal e As EventArgs)
        MyBase.OnGotFocus(e)
        TB.Focus()
    End Sub

    Private Sub TextChangeTb() Handles TB.TextChanged
        Text = TB.Text
    End Sub

    Private Sub TextChng() Handles MyBase.TextChanged
        TB.Text = Text
    End Sub

    Public Sub NewTextBox()
        With TB
            .Text = String.Empty
            .BackColor = Color.White
            .ForeColor = HelpersXylos.ColorFromHex("#7C858E")
            .TextAlign = HorizontalAlignment.Left
            .BorderStyle = BorderStyle.None
            .Location = New Point(3, 3)
            .Font = New Font("Segoe UI", 9)
            .Size = New Size(Width - 3, Height - 3)
            .UseSystemPasswordChar = UseSystemPasswordChar
        End With
    End Sub

    Sub New()
        MyBase.New()
        NewTextBox()
        Controls.Add(TB)
        SetStyle(ControlStyles.UserPaint Or ControlStyles.SupportsTransparentBackColor, True)
        DoubleBuffered = True
        TextAlign = HorizontalAlignment.Left
        ForeColor = HelpersXylos.ColorFromHex("#7C858E")
        Font = New Font("Segoe UI", 9)
        Size = New Size(130, 29)
        Enabled = True
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Color.White)

        If Enabled Then

            TB.ForeColor = HelpersXylos.ColorFromHex("#7C858E")

            If State = MouseState.Down Then

                Using Border As New Pen(HelpersXylos.ColorFromHex("#78B7E6"))
                    G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 12))
                End Using

            Else

                Using Border As New Pen(HelpersXylos.ColorFromHex("#D0D5D9"))
                    G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 12))
                End Using

            End If

        Else

            TB.ForeColor = HelpersXylos.ColorFromHex("#7C858E")

            Using Border As New Pen(HelpersXylos.ColorFromHex("#E1E1E2"))
                G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 12))
            End Using

        End If

        TB.TextAlign = TextAlign
        TB.UseSystemPasswordChar = UseSystemPasswordChar

    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        If Not MultiLine Then
            Dim tbheight As Integer = TB.Height
            TB.Location = New Point(10, CType(((Height / 2) - (tbheight / 2) - 0), Integer))
            TB.Size = New Size(Width - 20, tbheight)
        Else
            TB.Location = New Point(10, 10)
            TB.Size = New Size(Width - 20, Height - 20)
        End If
    End Sub

    Protected Overrides Sub OnEnter(e As EventArgs)
        MyBase.OnEnter(e)
        State = MouseState.Down : Invalidate()
    End Sub

    Protected Overrides Sub OnLeave(e As EventArgs)
        MyBase.OnLeave(e)
        State = MouseState.None : Invalidate()
    End Sub

End Class

Public Class XylosProgressBar
    Inherits Control

#Region " Drawing "

    Private _Val As Integer = 0
    Private _Min As Integer = 0
    Private _Max As Integer = 100

    Public Property Stripes As Color = Color.DarkGreen
    Public Property BackgroundColor As Color = Color.Green


    Public Property Value As Integer
        Get
            Return _Val
        End Get
        Set(value As Integer)
            _Val = value
            Invalidate()
        End Set
    End Property

    Public Property Minimum As Integer
        Get
            Return _Min
        End Get
        Set(value As Integer)
            _Min = value
            Invalidate()
        End Set
    End Property

    Public Property Maximum As Integer
        Get
            Return _Max
        End Get
        Set(value As Integer)
            _Max = value
            Invalidate()
        End Set
    End Property

    Sub New()
        DoubleBuffered = True
        Maximum = 100
        Minimum = 0
        Value = 0
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        Dim G As Graphics = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Color.White)

        Using Border As New Pen(HelpersXylos.ColorFromHex("#D0D5D9"))
            G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 6))
        End Using

        If Not Value = 0 Then

            Using Background As New Drawing2D.HatchBrush(Drawing2D.HatchStyle.LightUpwardDiagonal, Stripes, BackgroundColor)
                G.FillPath(Background, HelpersXylos.RoundRect1(New Rectangle(0, 0, CInt(Value / Maximum * Width - 1), Height - 1), 6))
            End Using

        End If


    End Sub

#End Region

End Class

Public Class XylosCombobox
    Inherits ComboBox

    Private G As Graphics
    Private Rect As Rectangle
    Private _EnabledCalc As Boolean

    Public Shadows Property Enabled As Boolean
        Get
            Return EnabledCalc
        End Get
        Set(value As Boolean)
            _EnabledCalc = value
            Invalidate()
        End Set
    End Property

    Public Property EnabledCalc As Boolean
        Get
            Return _EnabledCalc
        End Get
        Set(value As Boolean)
            MyBase.Enabled = value
            Enabled = value
            Invalidate()
        End Set
    End Property

    Sub New()
        DoubleBuffered = True
        DropDownStyle = ComboBoxStyle.DropDownList
        Cursor = Cursors.Hand
        Enabled = True
        DrawMode = DrawMode.OwnerDrawFixed
        ItemHeight = 20
    End Sub

    Protected Overrides Sub OnCreateControl()
        MyBase.OnCreateControl()
        SetStyle(ControlStyles.UserPaint, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Color.White)

        If Enabled Then

            Using Border As New Pen(HelpersXylos.ColorFromHex("#D0D5D9")), TriangleColor As New SolidBrush(HelpersXylos.ColorFromHex("#7C858E")), TriangleFont As New Font("Marlett", 13)
                G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 6))
                G.DrawString("6", TriangleFont, TriangleColor, New Point(Width - 22, 3))
            End Using

        Else

            Using Border As New Pen(HelpersXylos.ColorFromHex("#E1E1E2")), TriangleColor As New SolidBrush(HelpersXylos.ColorFromHex("#D0D3D7")), TriangleFont As New Font("Marlett", 13)
                G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 6))
                G.DrawString("6", TriangleFont, TriangleColor, New Point(Width - 22, 3))
            End Using

        End If

        If Not IsNothing(Items) Then

            Using ItemsFont As New Font("Segoe UI", 9), ItemsColor As New SolidBrush(HelpersXylos.ColorFromHex("#7C858E"))

                If Enabled Then

                    If Not SelectedIndex = -1 Then
                        G.DrawString(GetItemText(Items(SelectedIndex)), ItemsFont, ItemsColor, New Point(7, 4))
                    Else
                        Try
                            G.DrawString(GetItemText(Items(0)), ItemsFont, ItemsColor, New Point(7, 4))
                        Catch
                        End Try
                    End If

                Else

                    Using DisabledItemsColor As New SolidBrush(HelpersXylos.ColorFromHex("#D0D3D7"))

                        If Not SelectedIndex = -1 Then
                            G.DrawString(GetItemText(Items(SelectedIndex)), ItemsFont, DisabledItemsColor, New Point(7, 4))
                        Else
                            G.DrawString(GetItemText(Items(0)), ItemsFont, DisabledItemsColor, New Point(7, 4))
                        End If

                    End Using

                End If

            End Using

        End If

    End Sub

    Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)
        MyBase.OnDrawItem(e)
        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        If Enabled Then
            e.DrawBackground()
            Rect = e.Bounds

            Try

                Using ItemsFont As New Font("Segoe UI", 9), Border As New Pen(HelpersXylos.ColorFromHex("#D0D5D9"))

                    If (e.State And DrawItemState.Selected) = DrawItemState.Selected Then

                        Using ItemsColor As New SolidBrush(Color.White), Itembackground As New SolidBrush(HelpersXylos.ColorFromHex("#78B7E6"))
                            G.FillRectangle(Itembackground, Rect)
                            G.DrawString(GetItemText(Items(e.Index)), New Font("Segoe UI", 9), Brushes.White, New Point(Rect.X + 5, Rect.Y + 1))
                        End Using

                    Else
                        Using ItemsColor As New SolidBrush(HelpersXylos.ColorFromHex("#7C858E"))
                            G.FillRectangle(Brushes.White, Rect)
                            G.DrawString(GetItemText(Items(e.Index)), New Font("Segoe UI", 9), ItemsColor, New Point(Rect.X + 5, Rect.Y + 1))
                        End Using

                    End If

                End Using

            Catch
            End Try

        End If

    End Sub

    Protected Overrides Sub OnSelectedItemChanged(ByVal e As EventArgs)
        MyBase.OnSelectedItemChanged(e)
        Invalidate()
    End Sub

End Class

Public Class XylosSeparator
    Inherits Control

    Private G As Graphics

    Sub New()
        DoubleBuffered = True
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        Using C As New Pen(HelpersXylos.ColorFromHex("#EBEBEC"))
            G.DrawLine(C, New Point(0, 0), New Point(Width, 0))
        End Using

    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        Size = New Size(Width, 2)
    End Sub

End Class

Public Class XylosButton
    Inherits Control

    Private G As Graphics
    Private State As MouseState

    Private _EnabledCalc As Boolean

    Public Shadows Event Click(sender As Object, e As EventArgs)

    Sub New()
        DoubleBuffered = True
        Enabled = True
    End Sub

    Public Shadows Property Enabled As Boolean
        Get
            Return EnabledCalc
        End Get
        Set(value As Boolean)
            _EnabledCalc = value
            Invalidate()
        End Set
    End Property

    Public Property EnabledCalc As Boolean
        Get
            Return _EnabledCalc
        End Get
        Set(value As Boolean)
            Enabled = value
            Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        If Enabled Then

            Select Case State

                Case MouseState.Over

                    Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#FDFDFD"))
                        G.FillPath(Background, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
                    End Using

                Case MouseState.Down

                    Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#F0F0F0"))
                        G.FillPath(Background, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
                    End Using

                Case Else

                    Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#F6F6F6"))
                        G.FillPath(Background, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
                    End Using

            End Select

            Using ButtonFont As New Font("Segoe UI", 9), Border As New Pen(HelpersXylos.ColorFromHex("#C3C3C3"))
                G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
                HelpersXylos.CenterString(G, Text, ButtonFont, HelpersXylos.ColorFromHex("#7C858E"), HelpersXylos.FullRectangle(Size, False))
            End Using

        Else

            Using Background As New SolidBrush(HelpersXylos.ColorFromHex("#F3F4F7")), Border As New Pen(HelpersXylos.ColorFromHex("#DCDCDC")), ButtonFont As New Font("Segoe UI", 9)
                G.FillPath(Background, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
                G.DrawPath(Border, HelpersXylos.RoundRect1(HelpersXylos.FullRectangle(Size, True), 3))
                HelpersXylos.CenterString(G, Text, ButtonFont, HelpersXylos.ColorFromHex("#D0D3D7"), HelpersXylos.FullRectangle(Size, False))
            End Using

        End If

    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        State = MouseState.Over : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        State = MouseState.None : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)

        If Enabled Then
            RaiseEvent Click(Me, e)
        End If

        State = MouseState.Over : Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        State = MouseState.Down : Invalidate()
    End Sub

End Class

Friend Module Helpers1

    Public G As Graphics
    Private TargetStringMeasure As SizeF

    Enum MouseState1 As Byte
        None = 0
        Over = 1
        Down = 2
    End Enum

    Enum RoundingStyle As Byte
        All = 0
        Top = 1
        Bottom = 2
        Left = 3
        Right = 4
        TopRight = 5
        BottomRight = 6
    End Enum

    Public Function ColorFromHex(Hex As String) As Color
        Return Color.FromArgb(CInt(Long.Parse(String.Format("FFFFFFFFFF{0}", Hex.Substring(1)), Globalization.NumberStyles.HexNumber)))
    End Function

    Public Function MiddlePoint1(TargetText As String, TargetFont As Font, Rect As Rectangle) As Point
        TargetStringMeasure = G.MeasureString(TargetText, TargetFont)
        Return New Point(CInt(Rect.Width / 2 - TargetStringMeasure.Width / 2), CInt(Rect.Height / 2 - TargetStringMeasure.Height / 2))
    End Function

    Public Function RoundRect1(Rect As Rectangle, Rounding As Integer, Optional Style As RoundingStyle = RoundingStyle.All) As GraphicsPath

        Dim GP As New GraphicsPath()
        Dim AW As Integer = Rounding * 2

        GP.StartFigure()

        If Rounding = 0 Then
            GP.AddRectangle(Rect)
            GP.CloseAllFigures()
            Return GP
        End If

        Select Case Style
            Case RoundingStyle.All
                GP.AddArc(New Rectangle(Rect.X, Rect.Y, AW, AW), -180, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
                GP.AddArc(New Rectangle(Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 90, 90)
            Case RoundingStyle.Top
                GP.AddArc(New Rectangle(Rect.X, Rect.Y, AW, AW), -180, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddLine(New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y + Rect.Height))
            Case RoundingStyle.Bottom
                GP.AddLine(New Point(Rect.X, Rect.Y), New Point(Rect.X + Rect.Width, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
                GP.AddArc(New Rectangle(Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 90, 90)
            Case RoundingStyle.Left
                GP.AddArc(New Rectangle(Rect.X, Rect.Y, AW, AW), -180, 90)
                GP.AddLine(New Point(Rect.X + Rect.Width, Rect.Y), New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height))
                GP.AddArc(New Rectangle(Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 90, 90)
            Case RoundingStyle.Right
                GP.AddLine(New Point(Rect.X, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
            Case RoundingStyle.TopRight
                GP.AddLine(New Point(Rect.X, Rect.Y + 1), New Point(Rect.X, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Y, AW, AW), -90, 90)
                GP.AddLine(New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height - 1), New Point(Rect.X + Rect.Width, Rect.Y + Rect.Height))
                GP.AddLine(New Point(Rect.X + 1, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y + Rect.Height))
            Case RoundingStyle.BottomRight
                GP.AddLine(New Point(Rect.X, Rect.Y + 1), New Point(Rect.X, Rect.Y))
                GP.AddLine(New Point(Rect.X + Rect.Width - 1, Rect.Y), New Point(Rect.X + Rect.Width, Rect.Y))
                GP.AddArc(New Rectangle(Rect.Width - AW + Rect.X, Rect.Height - AW + Rect.Y, AW, AW), 0, 90)
                GP.AddLine(New Point(Rect.X + 1, Rect.Y + Rect.Height), New Point(Rect.X, Rect.Y + Rect.Height))
        End Select

        GP.CloseAllFigures()

        Return GP

    End Function

End Module

Public Class BoosterButton
    Inherits Button

    Private State As MouseState1
    Private Gradient As LinearGradientBrush

    Sub New()
        DoubleBuffered = True
        Font = New Font("Segoe UI", 9)
        ForeColor = HelpersXylos.ColorFromHex("#B6B6B6")
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.Opaque Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Parent.BackColor)

        If Enabled Then

            Select Case State
                Case MouseState1.None
                    Gradient = New LinearGradientBrush(New Rectangle(0, 0, Width - 1, Height - 1), HelpersXylos.ColorFromHex("#606060"), HelpersXylos.ColorFromHex("#4E4E4E"), LinearGradientMode.Vertical)

                Case MouseState1.Over
                    Gradient = New LinearGradientBrush(New Rectangle(0, 0, Width - 1, Height - 1), HelpersXylos.ColorFromHex("#6A6A6A"), HelpersXylos.ColorFromHex("#585858"), LinearGradientMode.Vertical)

                Case MouseState1.Down
                    Gradient = New LinearGradientBrush(New Rectangle(0, 0, Width - 1, Height - 1), HelpersXylos.ColorFromHex("#565656"), HelpersXylos.ColorFromHex("#444444"), LinearGradientMode.Vertical)

            End Select

            G.FillPath(Gradient, HelpersXylos.RoundRect1(New Rectangle(0, 0, Width - 1, Height - 1), 3))

            Using Border As New Pen(HelpersXylos.ColorFromHex("#323232"))
                G.DrawPath(Border, HelpersXylos.RoundRect1(New Rectangle(0, 0, Width - 1, Height - 1), 3))
            End Using

            '// Top Line
            Select Case State

                Case MouseState1.None

                    Using TopLine As New Pen(HelpersXylos.ColorFromHex("#737373"))
                        G.DrawLine(TopLine, 4, 1, Width - 4, 1)
                    End Using

                Case MouseState1.Over

                    Using TopLine As New Pen(HelpersXylos.ColorFromHex("#7D7D7D"))
                        G.DrawLine(TopLine, 4, 1, Width - 4, 1)
                    End Using

                Case MouseState1.Down

                    Using TopLine As New Pen(HelpersXylos.ColorFromHex("#696969"))
                        G.DrawLine(TopLine, 4, 1, Width - 4, 1)
                    End Using

            End Select

            Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#F5F5F5")), TextFont As New Font("Segoe UI", 9)
                G.DrawString(Text, TextFont, TextBrush, MiddlePoint1(Text, TextFont, New Rectangle(0, 0, Width + 2, Height)))
            End Using

        Else

            Gradient = New LinearGradientBrush(New Rectangle(0, 0, Width - 1, Height - 1), HelpersXylos.ColorFromHex("#4C4C4C"), HelpersXylos.ColorFromHex("#3A3A3A"), LinearGradientMode.Vertical)

            G.FillPath(Gradient, HelpersXylos.RoundRect1(New Rectangle(0, 0, Width - 1, Height - 1), 3))

            Using Border As New Pen(HelpersXylos.ColorFromHex("#323232"))
                G.DrawPath(Border, HelpersXylos.RoundRect1(New Rectangle(0, 0, Width - 1, Height - 1), 3))
            End Using

            Using TopLine As New Pen(HelpersXylos.ColorFromHex("#5F5F5F"))
                G.DrawLine(TopLine, 4, 1, Width - 4, 1)
            End Using

            Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#818181")), TextFont As New Font("Segoe UI", 9)
                G.DrawString(Text, TextFont, TextBrush, MiddlePoint1(Text, TextFont, New Rectangle(0, 0, Width + 2, Height)))
            End Using

        End If

    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        State = MouseState1.Over : Invalidate()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        State = MouseState1.None : Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        State = MouseState1.Down : Invalidate()
        MyBase.OnMouseDown(e)
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        State = MouseState1.Over : Invalidate()
        MyBase.OnMouseUp(e)
    End Sub

End Class

Public Class BoosterHeader
    Inherits Control

    Private TextMeasure As SizeF

    Sub New()
        DoubleBuffered = True
        Font = New Font("Segoe UI", 10)
        ForeColor = HelpersXylos.ColorFromHex("#C0C0C0")
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        G.Clear(Parent.BackColor)

        Using Line As New Pen(HelpersXylos.ColorFromHex("#5C5C5C"))
            G.DrawLine(Line, 0, 6, Width - 1, 6)
        End Using

        Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#D4D4D4")), TextFont As New Font("Segoe UI", 10), ParentFill As New SolidBrush(Parent.BackColor)
            TextMeasure = G.MeasureString(Text, TextFont)
            G.FillRectangle(ParentFill, New Rectangle(14, -4, CInt(TextMeasure.Width + 8), CInt(TextMeasure.Height + 4)))
            G.DrawString(Text, TextFont, TextBrush, New Point(20, -4))
        End Using

        MyBase.OnPaint(e)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        Size = New Size(Width, 14)
        MyBase.OnResize(e)
    End Sub

End Class

Public Class BoosterToolTip
    Inherits ToolTip

    Public Property BorderColor As Color = Color.SpringGreen

    Public Sub New()
        OwnerDraw = True
        Me.ForeColor = Color.White
        BackColor = Color.FromArgb(24, 24, 24) ' HelpersXylos.ColorFromHex("#242424")
        AddHandler Draw, AddressOf OnDraw
    End Sub

    Private Sub OnDraw(sender As Object, e As DrawToolTipEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        G.Clear(Me.BackColor) ' HelpersXylos.ColorFromHex("#242424")

        Using Border As New Pen(BorderColor) ' HelpersXylos.ColorFromHex("#343434")
            G.DrawRectangle(Border, New Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1))
        End Using

        If ToolTipIcon = ToolTipIcon.None Then

            Using TextFont As New Font("Segoe UI", 9), TextBrush As New SolidBrush(Me.ForeColor) 'HelpersXylos.ColorFromHex("#B6B6B6")
                G.DrawString(e.ToolTipText, TextFont, TextBrush, New PointF(e.Bounds.X + 4, e.Bounds.Y + 1))
            End Using

        Else

            Select Case ToolTipIcon

                Case ToolTipIcon.Info

                    Using TextFont As New Font("Segoe UI", 9, FontStyle.Bold), TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#7FD88B"))
                        G.DrawString("Information", TextFont, TextBrush, New PointF(e.Bounds.X + 4, e.Bounds.Y + 2))
                    End Using

                Case ToolTipIcon.Warning

                    Using TextFont As New Font("Segoe UI", 9, FontStyle.Bold), TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#D8C67F"))
                        G.DrawString("Warning", TextFont, TextBrush, New PointF(e.Bounds.X + 4, e.Bounds.Y + 2))
                    End Using

                Case ToolTipIcon.Error

                    Using TextFont As New Font("Segoe UI", 9, FontStyle.Bold), TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#D87F7F"))
                        G.DrawString("Error", TextFont, TextBrush, New PointF(e.Bounds.X + 4, e.Bounds.Y + 2))
                    End Using

            End Select

            Using TextFont As New Font("Segoe UI", 9), TextBrush As New SolidBrush(Me.ForeColor) ' HelpersXylos.ColorFromHex("#B6B6B6")
                G.DrawString(e.ToolTipText, TextFont, TextBrush, New PointF(e.Bounds.X + 4, e.Bounds.Y + 15))
            End Using

        End If

    End Sub

End Class

<DefaultEvent("TextChanged")>
Public Class BoosterTextBox
    Inherits Control

    Private WithEvents T As TextBox
    Private State As MouseState1

    Public Shadows Property Text As String
        Get
            Return T.Text
        End Get
        Set(value As String)
            MyBase.Text = value
            T.Text = value
            Invalidate()
        End Set
    End Property

    Public Shadows Property Enabled As Boolean
        Get
            Return T.Enabled
        End Get
        Set(value As Boolean)
            T.Enabled = value
            Invalidate()
        End Set
    End Property

    Public Property UseSystemPasswordChar As Boolean
        Get
            Return T.UseSystemPasswordChar
        End Get
        Set(value As Boolean)
            T.UseSystemPasswordChar = value
            Invalidate()
        End Set
    End Property

    Public Property MultiLine() As Boolean
        Get
            Return T.Multiline
        End Get
        Set(ByVal value As Boolean)
            T.Multiline = value
            Size = New Size(T.Width + 2, T.Height + 2)
            Invalidate()
        End Set
    End Property

    Public Shadows Property [ReadOnly]() As Boolean
        Get
            Return T.ReadOnly
        End Get
        Set(ByVal value As Boolean)
            T.ReadOnly = value
            Invalidate()
        End Set
    End Property

    Sub New()
        DoubleBuffered = True

        T = New TextBox With {
            .BorderStyle = BorderStyle.None,
            .BackColor = HelpersXylos.ColorFromHex("#242424"),
            .ForeColor = HelpersXylos.ColorFromHex("#B6B6B6"),
            .Location = New Point(1, 1),
            .Multiline = True}

        Controls.Add(T)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        If Enabled Then

            T.BackColor = HelpersXylos.ColorFromHex("#242424")

            Select Case State

                Case MouseState1.Down

                    Using Border As New Pen(HelpersXylos.ColorFromHex("#C8C8C8"))
                        G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
                    End Using

                Case Else

                    Using Border As New Pen(HelpersXylos.ColorFromHex("#5C5C5C"))
                        G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
                    End Using

            End Select

        Else

            T.BackColor = HelpersXylos.ColorFromHex("#282828")

            Using Border As New Pen(HelpersXylos.ColorFromHex("#484848"))
                G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
            End Using

        End If

        MyBase.OnPaint(e)

    End Sub

    Protected Overrides Sub OnEnter(e As EventArgs)
        State = MouseState1.Down : Invalidate()
        MyBase.OnEnter(e)
    End Sub

    Protected Overrides Sub OnLeave(e As EventArgs)
        State = MouseState1.None : Invalidate()
        MyBase.OnLeave(e)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        If MultiLine Then
            T.Size = New Size(Width - 2, Height - 2) : Invalidate()
        Else
            T.Size = New Size(Width - 2, T.Height)
            Size = New Size(Width, T.Height + 2)
        End If
        MyBase.OnResize(e)
    End Sub

    Private Sub TTextChanged() Handles T.TextChanged
        MyBase.OnTextChanged(EventArgs.Empty)
    End Sub

End Class

Public Class BoosterComboBox
    Inherits ComboBox

    Private State As MouseState1
    Private Rect As Rectangle

    Private ItemString As String = String.Empty
    Private FirstItem As String = String.Empty

    Sub New()
        ItemHeight = 20
        DoubleBuffered = True
        BackColor = Color.FromArgb(36, 36, 36)
        DropDownStyle = ComboBoxStyle.DropDownList
        DrawMode = DrawMode.OwnerDrawFixed
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.Opaque Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Parent.BackColor)

        If Enabled Then

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#242424"))
                G.FillRectangle(Fill, New Rectangle(0, 0, Width - 1, Height - 1))
            End Using

            Select Case State

                Case MouseState1.None

                    Using Border As New Pen(HelpersXylos.ColorFromHex("#5C5C5C"))
                        G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
                    End Using

                Case MouseState1.Over

                    Using Border As New Pen(HelpersXylos.ColorFromHex("#C8C8C8"))
                        G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
                    End Using

            End Select

            Using ArrowFont As New Font("Marlett", 12), ArrowBrush As New SolidBrush(HelpersXylos.ColorFromHex("#909090"))
                G.DrawString("6", ArrowFont, ArrowBrush, New Point(Width - 20, 5))
            End Using

            If Not IsNothing(Items) Then

                Try : FirstItem = GetItemText(Items(0)) : Catch : End Try

                If Not SelectedIndex = -1 Then

                    Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#B6B6B6")), TextFont As New Font("Segoe UI", 9)
                        G.DrawString(ItemString, TextFont, TextBrush, New Point(4, 4))
                    End Using

                Else

                    Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#B6B6B6")), TextFont As New Font("Segoe UI", 9)
                        G.DrawString(FirstItem, TextFont, TextBrush, New Point(4, 4))
                    End Using

                End If


            End If

        Else

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#282828")), Border As New Pen(HelpersXylos.ColorFromHex("#484848"))
                G.FillRectangle(Fill, New Rectangle(0, 0, Width - 1, Height - 1))
                G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
            End Using

            Using ArrowFont As New Font("Marlett", 12), ArrowBrush As New SolidBrush(HelpersXylos.ColorFromHex("#707070"))
                G.DrawString("6", ArrowFont, ArrowBrush, New Point(Width - 20, 5))
            End Using

            If Not IsNothing(Items) Then

                Try : FirstItem = GetItemText(Items(0)) : Catch : End Try

                Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#818181")), TextFont As New Font("Segoe UI", 9)
                    G.DrawString(FirstItem, TextFont, TextBrush, New Point(4, 4))
                End Using

            End If

        End If

    End Sub

    Protected Overrides Sub OnDrawItem(e As DrawItemEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        Rect = e.Bounds

        Using Back As New SolidBrush(HelpersXylos.ColorFromHex("#242424"))
            G.FillRectangle(Back, New Rectangle(e.Bounds.X - 4, e.Bounds.Y - 1, e.Bounds.Width + 4, e.Bounds.Height - 1))
        End Using

        If Not e.Index = -1 Then
            ItemString = GetItemText(Items(e.Index))
        End If

        Using ItemsFont As New Font("Segoe UI", 9), Border As New Pen(HelpersXylos.ColorFromHex("#D0D5D9"))

            If (e.State And DrawItemState.Selected) = DrawItemState.Selected Then

                Using HoverItemBrush As New SolidBrush(HelpersXylos.ColorFromHex("#F5F5F5"))
                    G.DrawString(ItemString, New Font("Segoe UI", 9), HoverItemBrush, New Point(Rect.X + 5, Rect.Y + 1))
                End Using

            Else

                Using DefaultItemBrush As New SolidBrush(HelpersXylos.ColorFromHex("#C0C0C0"))
                    G.DrawString(ItemString, New Font("Segoe UI", 9), DefaultItemBrush, New Point(Rect.X + 5, Rect.Y + 1))
                End Using

            End If

        End Using

        e.DrawFocusRectangle()

        MyBase.OnDrawItem(e)

    End Sub

    Protected Overrides Sub OnSelectedItemChanged(ByVal e As EventArgs)
        Invalidate()
        MyBase.OnSelectedItemChanged(e)
    End Sub

    Protected Overrides Sub OnSelectedIndexChanged(e As EventArgs)
        State = MouseState1.None : Invalidate()
        MyBase.OnSelectedIndexChanged(e)
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        State = MouseState1.Over : Invalidate()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        State = MouseState1.None : Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

End Class

Public Class BoosterCheckBox
    Inherits CheckBox

    Private State As MouseState1
    Private Block As Boolean

    Private CheckThread, UncheckThread As Thread
    Private OverFillRect As New Rectangle(1, 1, 14, 14)

    Sub New()
        DoubleBuffered = True
        Font = New Font("Segoe UI", 9)
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.Opaque Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Private Sub CheckAnimation()

        Block = True

        Dim X As Integer = 1
        Dim Rectw As Integer = 15

        While Not OverFillRect.Width = 0
            X += 1
            Rectw -= 1
            OverFillRect = New Rectangle(X, OverFillRect.Y, Rectw, OverFillRect.Height)
            Invalidate()
            Thread.Sleep(30)
        End While

        Block = False

    End Sub

    Private Sub UncheckAnimation()

        Block = True

        Dim X As Integer = 15
        Dim Rectw As Integer = 0

        While Not OverFillRect.Width = 14
            X -= 1
            Rectw += 1
            OverFillRect = New Rectangle(X, OverFillRect.Y, Rectw, OverFillRect.Height)
            Invalidate()
            Thread.Sleep(30)
        End While

        Block = False

    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Parent.BackColor)

        If Enabled Then

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#242424")), Border As New Pen(HelpersXylos.ColorFromHex("#5C5C5C"))
                G.FillRectangle(Fill, New Rectangle(0, 0, 16, 16))
                G.DrawRectangle(Border, New Rectangle(0, 0, 16, 16))
            End Using

            Select Case State

                Case MouseState1.None

                    Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#B6B6B6")), TextFont As New Font("Segoe UI", 9)
                        G.DrawString(Text, TextFont, TextBrush, New Point(25, -1))
                    End Using

                Case MouseState1.Over

                    Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#F5F5F5")), TextFont As New Font("Segoe UI", 9)
                        G.DrawString(Text, TextFont, TextBrush, New Point(25, -1))
                    End Using

            End Select

            Using CheckFont As New Font("Marlett", 12), CheckBrush As New SolidBrush(Color.FromArgb(144, 144, 144))
                G.DrawString("b", CheckFont, CheckBrush, New Point(-2, 1))
            End Using

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#242424"))
                G.SmoothingMode = SmoothingMode.None
                G.FillRectangle(Fill, OverFillRect)
            End Using

        Else

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#282828")), Border As New Pen(HelpersXylos.ColorFromHex("#484848"))
                G.FillRectangle(Fill, New Rectangle(0, 0, 16, 16))
                G.DrawRectangle(Border, New Rectangle(0, 0, 16, 16))
            End Using

            Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#818181")), TextFont As New Font("Segoe UI", 9)
                G.DrawString(Text, TextFont, TextBrush, New Point(25, -1))
            End Using

            Using CheckFont As New Font("Marlett", 12), CheckBrush As New SolidBrush(HelpersXylos.ColorFromHex("#707070"))
                G.DrawString("b", CheckFont, CheckBrush, New Point(-2, 1))
            End Using

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#282828"))
                G.SmoothingMode = SmoothingMode.None
                G.FillRectangle(Fill, OverFillRect)
            End Using

        End If

    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        State = MouseState1.Over : Invalidate()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        State = MouseState1.None : Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnCheckedChanged(e As EventArgs)

        If Checked Then
            CheckThread = New Thread(AddressOf CheckAnimation) With {
                .IsBackground = True}
            CheckThread.Start()
        Else
            UncheckThread = New Thread(AddressOf UncheckAnimation) With {
             .IsBackground = True}
            UncheckThread.Start()
        End If

        If Not Block Then
            MyBase.OnCheckedChanged(e)
        End If

    End Sub

End Class

Public Class BoosterTabControl
    Inherits TabControl

    Private MainRect As Rectangle
    Private OverRect As Rectangle

    Private SubOverIndex As Integer = -1

    Private ReadOnly Property Hovering As Boolean
        Get
            Return Not OverIndex = -1
        End Get
    End Property

    Private Property OverIndex As Integer
        Get
            Return SubOverIndex
        End Get
        Set(value As Integer)
            SubOverIndex = value
            If Not SubOverIndex = -1 Then
                OverRect = GetTabRect(OverIndex)
            End If
            Invalidate()
        End Set
    End Property

    Sub New()
        DoubleBuffered = True
        Font = New Font("Segoe UI", 10)
        ForeColor = HelpersXylos.ColorFromHex("#78797B")
        ItemSize = New Size(40, 170)
        SizeMode = TabSizeMode.Fixed
        Alignment = TabAlignment.Left
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.Opaque Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Protected Overrides Sub CreateHandle()
        For Each Tab As TabPage In TabPages
            Tab.BackColor = Color.FromArgb(24, 24, 24) 'HelpersXylos.ColorFromHex("#424242")
            Tab.ForeColor = HelpersXylos.ColorFromHex("#B6B6B6")
            Tab.Font = New Font("Segoe UI", 9)
        Next
        MyBase.CreateHandle()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        G.Clear(Color.FromArgb(24, 24, 24)) '¡HelpersXylos.ColorFromHex("#333333"))

        Using Border As New Pen(HelpersXylos.ColorFromHex("#292929"))
            G.SmoothingMode = SmoothingMode.None
            G.DrawLine(Border, ItemSize.Height + 3, 4, ItemSize.Height + 3, Height - 5)
        End Using

        For I As Integer = 0 To TabPages.Count - 1

            MainRect = GetTabRect(I)

            If SelectedIndex = I Then

                Using Selection As New SolidBrush(HelpersXylos.ColorFromHex("#424242"))
                    G.FillRectangle(Selection, New Rectangle(MainRect.X - 6, MainRect.Y + 2, MainRect.Width + 8, MainRect.Height - 1))
                End Using

                Using SelectionLeft As New SolidBrush(HelpersXylos.ColorFromHex("#F63333"))
                    G.FillRectangle(SelectionLeft, New Rectangle(MainRect.X - 2, MainRect.Y + 2, 3, MainRect.Height - 1))
                End Using

                Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#F5F5F5")), TextFont As New Font("Segoe UI", 10)
                    G.DrawString(TabPages(I).Text, TextFont, TextBrush, New Point(MainRect.X + 25, MainRect.Y + 11))
                End Using

            Else

                Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#C0C0C0")), TextFont As New Font("Segoe UI", 10)
                    G.DrawString(TabPages(I).Text, TextFont, TextBrush, New Point(MainRect.X + 25, MainRect.Y + 11))
                End Using

            End If

            If Hovering Then

                Using Selection As New SolidBrush(HelpersXylos.ColorFromHex("#383838"))
                    G.FillRectangle(Selection, New Rectangle(OverRect.X - 6, OverRect.Y + 2, OverRect.Width + 8, OverRect.Height - 1))
                End Using

                Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#C0C0C0")), TextFont As New Font("Segoe UI", 10)
                    G.DrawString(TabPages(OverIndex).Text, TextFont, TextBrush, New Point(OverRect.X + 25, OverRect.Y + 11))
                End Using

            End If

        Next

        MyBase.OnPaint(e)

    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        For I As Integer = 0 To TabPages.Count - 1
            If GetTabRect(I).Contains(e.Location) And Not SelectedIndex = I Then
                OverIndex = I
                Exit For
            Else
                OverIndex = -1
            End If
        Next
        MyBase.OnMouseMove(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        OverIndex = -1
        MyBase.OnMouseLeave(e)
    End Sub

End Class

Public Class BoosterRadioButton
    Inherits RadioButton

    Private State As MouseState1

    Private CheckThread, UncheckThread As Thread
    Private EllipseRect As New Rectangle(5, 5, 6, 6)

    Sub New()
        DoubleBuffered = True
        Font = New Font("Segoe UI", 9)
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.Opaque Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Private Sub CheckAnimation()

        Dim X As Integer = 1
        Dim Y As Integer = 1
        Dim EllipseW As Integer = 14
        Dim EllipseH As Integer = 14

        While Not EllipseH = 8

            If X < 4 Then
                X += 1
                Y += 1
            End If

            EllipseW -= 1
            EllipseH -= 1
            EllipseRect = New Rectangle(X, Y, EllipseW, EllipseH)
            Invalidate()
            Thread.Sleep(30)
        End While

    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        MyBase.OnPaint(e)

        G.Clear(Parent.BackColor)

        If Enabled Then

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#242424")), Border As New Pen(HelpersXylos.ColorFromHex("#5C5C5C"))
                G.FillEllipse(Fill, New Rectangle(0, 0, 16, 16))
                G.DrawEllipse(Border, New Rectangle(0, 0, 16, 16))
            End Using

            Select Case State

                Case MouseState1.None

                    Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#B6B6B6")), TextFont As New Font("Segoe UI", 9)
                        G.DrawString(Text, TextFont, TextBrush, New Point(25, -1))
                    End Using

                Case MouseState1.Over

                    Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#F5F5F5")), TextFont As New Font("Segoe UI", 9)
                        G.DrawString(Text, TextFont, TextBrush, New Point(25, -1))
                    End Using

            End Select

            If Checked Then

                Using CheckBrush As New SolidBrush(HelpersXylos.ColorFromHex("#909090"))
                    G.FillEllipse(CheckBrush, EllipseRect)
                End Using

            End If

        Else

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#282828")), Border As New Pen(HelpersXylos.ColorFromHex("#484848"))
                G.FillEllipse(Fill, New Rectangle(0, 0, 16, 16))
                G.DrawEllipse(Border, New Rectangle(0, 0, 16, 16))
            End Using

            Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#818181")), TextFont As New Font("Segoe UI", 9)
                G.DrawString(Text, TextFont, TextBrush, New Point(25, -1))
            End Using

            If Checked Then

                Using CheckBrush As New SolidBrush(HelpersXylos.ColorFromHex("#707070"))
                    G.FillEllipse(CheckBrush, EllipseRect)
                End Using

            End If

        End If

    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        State = MouseState1.Over : Invalidate()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        State = MouseState1.None : Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnCheckedChanged(e As EventArgs)

        If Checked Then
            CheckThread = New Thread(AddressOf CheckAnimation) With {
                .IsBackground = True}
            CheckThread.Start()
        End If

        MyBase.OnCheckedChanged(e)
    End Sub

End Class

Public Class BoosterNumericUpDown
    Inherits NumericUpDown

    Private State As MouseState1
    Public Property AfterValue As String

    Private ValueChangedThread As Thread
    Private TextPoint As New Point(2, 2)
    Private TextFont As New Font("Segoe UI", 10)

    Sub New()
        DoubleBuffered = True
        Font = New Font("Segoe UI", 10)
        Controls(0).Hide()
        Controls(1).Hide()
        ForeColor = HelpersXylos.ColorFromHex("#B6B6B6")
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.Opaque Or ControlStyles.OptimizedDoubleBuffer, True)
    End Sub

    Private Sub ValueChangedAnimation()

        Dim TextSize As Integer = 5

        While Not TextSize = 10
            TextSize += 1
            TextFont = New Font("Segoe UI", TextSize)
            Invalidate()
            Thread.Sleep(30)
        End While

    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)

        G = e.Graphics
        G.SmoothingMode = SmoothingMode.HighQuality
        G.TextRenderingHint = TextRenderingHint.ClearTypeGridFit

        G.Clear(Parent.BackColor)

        MyBase.OnPaint(e)

        If Enabled Then

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#242424"))
                G.FillRectangle(Fill, New Rectangle(0, 0, Width - 1, Height - 1))
            End Using

            Select Case State

                Case MouseState1.None

                    Using Border As New Pen(HelpersXylos.ColorFromHex("#5C5C5C"))
                        G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
                    End Using

                Case MouseState1.Over

                    Using Border As New Pen(HelpersXylos.ColorFromHex("#C8C8C8"))
                        G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
                    End Using

            End Select

            Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#B6B6B6"))
                G.DrawString(Value & AfterValue, TextFont, TextBrush, TextPoint)
            End Using

            Using ArrowFont As New Font("Marlett", 10), ArrowBrush As New SolidBrush(HelpersXylos.ColorFromHex("#909090"))
                G.DrawString("5", ArrowFont, ArrowBrush, New Point(Width - 18, 2))
                G.DrawString("6", ArrowFont, ArrowBrush, New Point(Width - 18, 10))
            End Using

        Else

            Using Fill As New SolidBrush(HelpersXylos.ColorFromHex("#282828")), Border As New Pen(HelpersXylos.ColorFromHex("#484848"))
                G.FillRectangle(Fill, New Rectangle(0, 0, Width - 1, Height - 1))
                G.DrawRectangle(Border, New Rectangle(0, 0, Width - 1, Height - 1))
            End Using

            Using TextBrush As New SolidBrush(HelpersXylos.ColorFromHex("#818181"))
                G.DrawString(Value & AfterValue, TextFont, TextBrush, TextPoint)
            End Using

            Using ArrowFont As New Font("Marlett", 10), ArrowBrush As New SolidBrush(HelpersXylos.ColorFromHex("#707070"))
                G.DrawString("5", ArrowFont, ArrowBrush, New Point(Width - 18, 2))
                G.DrawString("6", ArrowFont, ArrowBrush, New Point(Width - 18, 10))
            End Using

        End If

    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)

        If e.X > Width - 16 AndAlso e.Y < 11 Then

            If Not Value + Increment > Maximum Then
                Value += Increment
            Else
                Value = Maximum
            End If

        ElseIf e.X > Width - 16 AndAlso e.Y > 13 Then
            If Not Value - Increment < Minimum Then
                Value -= Increment
            Else
                Value = Minimum
            End If
        End If

        Invalidate()

        MyBase.OnMouseUp(e)
    End Sub

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        State = MouseState1.Over : Invalidate()
        MyBase.OnMouseEnter(e)
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        State = MouseState1.None : Invalidate()
        MyBase.OnMouseLeave(e)
    End Sub

    Protected Overrides Sub OnValueChanged(e As EventArgs)
        ValueChangedThread = New Thread(AddressOf ValueChangedAnimation) With {
            .IsBackground = True}
        ValueChangedThread.Start()
        MyBase.OnValueChanged(e)
    End Sub

End Class

