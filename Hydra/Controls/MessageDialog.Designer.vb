<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MessageDialog
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MessageDialog))
        Me.Guna2Panel1 = New Guna.UI2.WinForms.Guna2Panel()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.StatusLabel = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Me.Guna2Button1 = New Guna.UI2.WinForms.Guna2Button()
        Me.Guna2Button3 = New Guna.UI2.WinForms.Guna2Button()
        Me.Guna2Panel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Guna2Panel1
        '
        Me.Guna2Panel1.BorderColor = System.Drawing.Color.Lime
        Me.Guna2Panel1.BorderThickness = 1
        Me.Guna2Panel1.Controls.Add(Me.Label2)
        Me.Guna2Panel1.Controls.Add(Me.StatusLabel)
        Me.Guna2Panel1.Controls.Add(Me.Guna2Button1)
        Me.Guna2Panel1.Controls.Add(Me.Guna2Button3)
        Me.Guna2Panel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Guna2Panel1.Location = New System.Drawing.Point(0, 0)
        Me.Guna2Panel1.Name = "Guna2Panel1"
        Me.Guna2Panel1.Size = New System.Drawing.Size(284, 121)
        Me.Guna2Panel1.TabIndex = 0
        '
        'Label2
        '
        Me.Label2.BackColor = System.Drawing.Color.Lime
        Me.Label2.Dock = System.Windows.Forms.DockStyle.Top
        Me.Label2.Font = New System.Drawing.Font("Segoe UI Black", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.White
        Me.Label2.Location = New System.Drawing.Point(0, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(284, 17)
        Me.Label2.TabIndex = 39
        Me.Label2.Text = "DIH Pro"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'StatusLabel
        '
        Me.StatusLabel.AutoSize = False
        Me.StatusLabel.BackColor = System.Drawing.Color.Transparent
        Me.StatusLabel.Font = New System.Drawing.Font("Segoe UI Semilight", 9.25!)
        Me.StatusLabel.ForeColor = System.Drawing.Color.WhiteSmoke
        Me.StatusLabel.IsContextMenuEnabled = False
        Me.StatusLabel.IsSelectionEnabled = False
        Me.StatusLabel.Location = New System.Drawing.Point(3, 20)
        Me.StatusLabel.Name = "StatusLabel"
        Me.StatusLabel.Size = New System.Drawing.Size(278, 66)
        Me.StatusLabel.TabIndex = 38
        Me.StatusLabel.Text = Nothing
        Me.StatusLabel.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias
        Me.StatusLabel.UseGdiPlusTextRendering = True
        '
        'Guna2Button1
        '
        Me.Guna2Button1.BorderColor = System.Drawing.Color.White
        Me.Guna2Button1.BorderRadius = 4
        Me.Guna2Button1.CheckedState.BorderColor = System.Drawing.Color.SpringGreen
        Me.Guna2Button1.CheckedState.FillColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(127, Byte), Integer))
        Me.Guna2Button1.CheckedState.Image = CType(resources.GetObject("resource.Image"), System.Drawing.Image)
        Me.Guna2Button1.FillColor = System.Drawing.Color.FromArgb(CType(CType(80, Byte), Integer), CType(CType(34, Byte), Integer), CType(CType(41, Byte), Integer), CType(CType(56, Byte), Integer))
        Me.Guna2Button1.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.Guna2Button1.ForeColor = System.Drawing.Color.White
        Me.Guna2Button1.ImageSize = New System.Drawing.Size(20, 19)
        Me.Guna2Button1.Location = New System.Drawing.Point(203, 92)
        Me.Guna2Button1.Name = "Guna2Button1"
        Me.Guna2Button1.Size = New System.Drawing.Size(69, 26)
        Me.Guna2Button1.TabIndex = 37
        Me.Guna2Button1.Text = "Cancel"
        '
        'Guna2Button3
        '
        Me.Guna2Button3.BorderColor = System.Drawing.Color.White
        Me.Guna2Button3.BorderRadius = 4
        Me.Guna2Button3.CheckedState.BorderColor = System.Drawing.Color.SpringGreen
        Me.Guna2Button3.CheckedState.FillColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(0, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(127, Byte), Integer))
        Me.Guna2Button3.CheckedState.Image = CType(resources.GetObject("resource.Image1"), System.Drawing.Image)
        Me.Guna2Button3.FillColor = System.Drawing.Color.FromArgb(CType(CType(80, Byte), Integer), CType(CType(34, Byte), Integer), CType(CType(41, Byte), Integer), CType(CType(56, Byte), Integer))
        Me.Guna2Button3.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.Guna2Button3.ForeColor = System.Drawing.Color.White
        Me.Guna2Button3.ImageSize = New System.Drawing.Size(20, 19)
        Me.Guna2Button3.Location = New System.Drawing.Point(140, 92)
        Me.Guna2Button3.Name = "Guna2Button3"
        Me.Guna2Button3.Size = New System.Drawing.Size(57, 26)
        Me.Guna2Button3.TabIndex = 36
        Me.Guna2Button3.Text = "Ok"
        '
        'MessageDialog
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(284, 121)
        Me.Controls.Add(Me.Guna2Panel1)
        Me.ForeColor = System.Drawing.Color.White
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "MessageDialog"
        Me.Opacity = 0.8R
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "MessageDialog"
        Me.Guna2Panel1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Guna2Panel1 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents Guna2Button3 As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents Guna2Button1 As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents StatusLabel As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Label2 As Label
End Class
