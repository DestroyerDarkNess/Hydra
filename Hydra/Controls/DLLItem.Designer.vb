<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class DLLItem
    Inherits System.Windows.Forms.UserControl

    'UserControl reemplaza a Dispose para limpiar la lista de componentes.
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

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Guna2Panel5 = New Guna.UI2.WinForms.Guna2Panel()
        Me.LogInLabel3 = New Hydra.LogInLabel()
        Me.Guna2ComboBox1 = New Guna.UI2.WinForms.Guna2ComboBox()
        Me.Guna2CheckBox3 = New Guna.UI2.WinForms.Guna2CheckBox()
        Me.LogInLabel2 = New Hydra.LogInLabel()
        Me.Guna2Panel5.SuspendLayout()
        Me.SuspendLayout()
        '
        'Guna2Panel5
        '
        Me.Guna2Panel5.BorderColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.Guna2Panel5.BorderThickness = 1
        Me.Guna2Panel5.Controls.Add(Me.LogInLabel3)
        Me.Guna2Panel5.Controls.Add(Me.Guna2ComboBox1)
        Me.Guna2Panel5.Controls.Add(Me.Guna2CheckBox3)
        Me.Guna2Panel5.Controls.Add(Me.LogInLabel2)
        Me.Guna2Panel5.Location = New System.Drawing.Point(0, 0)
        Me.Guna2Panel5.Name = "Guna2Panel5"
        Me.Guna2Panel5.Size = New System.Drawing.Size(654, 25)
        Me.Guna2Panel5.TabIndex = 3
        '
        'LogInLabel3
        '
        Me.LogInLabel3.BackColor = System.Drawing.Color.Transparent
        Me.LogInLabel3.Font = New System.Drawing.Font("Consolas", 6.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LogInLabel3.FontColour = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.LogInLabel3.ForeColor = System.Drawing.Color.Gray
        Me.LogInLabel3.Location = New System.Drawing.Point(235, 0)
        Me.LogInLabel3.Name = "LogInLabel3"
        Me.LogInLabel3.Size = New System.Drawing.Size(271, 25)
        Me.LogInLabel3.TabIndex = 64
        Me.LogInLabel3.Text = "Guna.UI"
        Me.LogInLabel3.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'Guna2ComboBox1
        '
        Me.Guna2ComboBox1.BackColor = System.Drawing.Color.Transparent
        Me.Guna2ComboBox1.BorderColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.Guna2ComboBox1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed
        Me.Guna2ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.Guna2ComboBox1.FillColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(29, Byte), Integer), CType(CType(34, Byte), Integer))
        Me.Guna2ComboBox1.FocusedColor = System.Drawing.Color.Empty
        Me.Guna2ComboBox1.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.Guna2ComboBox1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.Guna2ComboBox1.FormattingEnabled = True
        Me.Guna2ComboBox1.ItemHeight = 25
        Me.Guna2ComboBox1.Items.AddRange(New Object() {"Libz", "IL Embed", "IL Merge", "Resources"})
        Me.Guna2ComboBox1.Location = New System.Drawing.Point(512, -4)
        Me.Guna2ComboBox1.Name = "Guna2ComboBox1"
        Me.Guna2ComboBox1.Size = New System.Drawing.Size(142, 31)
        Me.Guna2ComboBox1.StartIndex = 0
        Me.Guna2ComboBox1.TabIndex = 63
        '
        'Guna2CheckBox3
        '
        Me.Guna2CheckBox3.Animated = True
        Me.Guna2CheckBox3.AutoSize = True
        Me.Guna2CheckBox3.Checked = True
        Me.Guna2CheckBox3.CheckedState.BorderColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.Guna2CheckBox3.CheckedState.BorderRadius = 0
        Me.Guna2CheckBox3.CheckedState.BorderThickness = 1
        Me.Guna2CheckBox3.CheckedState.FillColor = System.Drawing.Color.FromArgb(CType(CType(21, Byte), Integer), CType(CType(21, Byte), Integer), CType(CType(21, Byte), Integer))
        Me.Guna2CheckBox3.CheckState = System.Windows.Forms.CheckState.Checked
        Me.Guna2CheckBox3.Location = New System.Drawing.Point(13, 6)
        Me.Guna2CheckBox3.Name = "Guna2CheckBox3"
        Me.Guna2CheckBox3.Size = New System.Drawing.Size(15, 14)
        Me.Guna2CheckBox3.TabIndex = 62
        Me.Guna2CheckBox3.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer), CType(CType(64, Byte), Integer))
        Me.Guna2CheckBox3.UncheckedState.BorderRadius = 0
        Me.Guna2CheckBox3.UncheckedState.BorderThickness = 1
        Me.Guna2CheckBox3.UncheckedState.FillColor = System.Drawing.Color.FromArgb(CType(CType(21, Byte), Integer), CType(CType(21, Byte), Integer), CType(CType(21, Byte), Integer))
        Me.Guna2CheckBox3.UseVisualStyleBackColor = True
        '
        'LogInLabel2
        '
        Me.LogInLabel2.BackColor = System.Drawing.Color.Transparent
        Me.LogInLabel2.Font = New System.Drawing.Font("Consolas", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LogInLabel2.FontColour = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.LogInLabel2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.LogInLabel2.Location = New System.Drawing.Point(34, 4)
        Me.LogInLabel2.Name = "LogInLabel2"
        Me.LogInLabel2.Size = New System.Drawing.Size(195, 16)
        Me.LogInLabel2.TabIndex = 14
        Me.LogInLabel2.Text = "Guna.UI"
        Me.LogInLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'DLLItem
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Transparent
        Me.Controls.Add(Me.Guna2Panel5)
        Me.ForeColor = System.Drawing.Color.White
        Me.Name = "DLLItem"
        Me.Size = New System.Drawing.Size(654, 25)
        Me.Guna2Panel5.ResumeLayout(False)
        Me.Guna2Panel5.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Guna2Panel5 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents LogInLabel3 As LogInLabel
    Friend WithEvents Guna2ComboBox1 As Guna.UI2.WinForms.Guna2ComboBox
    Friend WithEvents Guna2CheckBox3 As Guna.UI2.WinForms.Guna2CheckBox
    Friend WithEvents LogInLabel2 As LogInLabel
End Class
