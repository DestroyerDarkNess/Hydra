<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class PresetManagerForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.ListBoxPresets = New System.Windows.Forms.ListBox()
        Me.BtnSave = New System.Windows.Forms.Button()
        Me.BtnLoad = New System.Windows.Forms.Button()
        Me.BtnDelete = New System.Windows.Forms.Button()
        Me.BtnExport = New System.Windows.Forms.Button()
        Me.BtnImport = New System.Windows.Forms.Button()
        Me.BtnInfo = New System.Windows.Forms.Button()
        Me.BtnClose = New System.Windows.Forms.Button()
        Me.BtnRefresh = New System.Windows.Forms.Button()
        Me.LblPreviewInfo = New System.Windows.Forms.Label()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'ListBoxPresets
        '
        Me.ListBoxPresets.FormattingEnabled = True
        Me.ListBoxPresets.Location = New System.Drawing.Point(12, 32)
        Me.ListBoxPresets.Name = "ListBoxPresets"
        Me.ListBoxPresets.Size = New System.Drawing.Size(250, 290)
        Me.ListBoxPresets.TabIndex = 0
        '
        'BtnSave
        '
        Me.BtnSave.Location = New System.Drawing.Point(12, 340)
        Me.BtnSave.Name = "BtnSave"
        Me.BtnSave.Size = New System.Drawing.Size(75, 30)
        Me.BtnSave.TabIndex = 1
        Me.BtnSave.Text = "Guardar"
        Me.BtnSave.UseVisualStyleBackColor = True
        '
        'BtnLoad
        '
        Me.BtnLoad.Location = New System.Drawing.Point(95, 340)
        Me.BtnLoad.Name = "BtnLoad"
        Me.BtnLoad.Size = New System.Drawing.Size(75, 30)
        Me.BtnLoad.TabIndex = 2
        Me.BtnLoad.Text = "Cargar"
        Me.BtnLoad.UseVisualStyleBackColor = True
        '
        'BtnDelete
        '
        Me.BtnDelete.Location = New System.Drawing.Point(187, 340)
        Me.BtnDelete.Name = "BtnDelete"
        Me.BtnDelete.Size = New System.Drawing.Size(75, 30)
        Me.BtnDelete.TabIndex = 3
        Me.BtnDelete.Text = "Eliminar"
        Me.BtnDelete.UseVisualStyleBackColor = True
        '
        'BtnExport
        '
        Me.BtnExport.Location = New System.Drawing.Point(12, 380)
        Me.BtnExport.Name = "BtnExport"
        Me.BtnExport.Size = New System.Drawing.Size(75, 30)
        Me.BtnExport.TabIndex = 4
        Me.BtnExport.Text = "Exportar"
        Me.BtnExport.UseVisualStyleBackColor = True
        '
        'BtnImport
        '
        Me.BtnImport.Location = New System.Drawing.Point(95, 380)
        Me.BtnImport.Name = "BtnImport"
        Me.BtnImport.Size = New System.Drawing.Size(75, 30)
        Me.BtnImport.TabIndex = 5
        Me.BtnImport.Text = "Importar"
        Me.BtnImport.UseVisualStyleBackColor = True
        '
        'BtnInfo
        '
        Me.BtnInfo.Location = New System.Drawing.Point(187, 380)
        Me.BtnInfo.Name = "BtnInfo"
        Me.BtnInfo.Size = New System.Drawing.Size(75, 30)
        Me.BtnInfo.TabIndex = 6
        Me.BtnInfo.Text = "Info"
        Me.BtnInfo.UseVisualStyleBackColor = True
        '
        'BtnClose
        '
        Me.BtnClose.Location = New System.Drawing.Point(500, 380)
        Me.BtnClose.Name = "BtnClose"
        Me.BtnClose.Size = New System.Drawing.Size(75, 30)
        Me.BtnClose.TabIndex = 7
        Me.BtnClose.Text = "Cerrar"
        Me.BtnClose.UseVisualStyleBackColor = True
        '
        'BtnRefresh
        '
        Me.BtnRefresh.Location = New System.Drawing.Point(12, 420)
        Me.BtnRefresh.Name = "BtnRefresh"
        Me.BtnRefresh.Size = New System.Drawing.Size(75, 30)
        Me.BtnRefresh.TabIndex = 8
        Me.BtnRefresh.Text = "Actualizar"
        Me.BtnRefresh.UseVisualStyleBackColor = True
        '
        'LblPreviewInfo
        '
        Me.LblPreviewInfo.AutoSize = False
        Me.LblPreviewInfo.Location = New System.Drawing.Point(15, 30)
        Me.LblPreviewInfo.Name = "LblPreviewInfo"
        Me.LblPreviewInfo.Size = New System.Drawing.Size(280, 120)
        Me.LblPreviewInfo.TabIndex = 9
        Me.LblPreviewInfo.Text = "Selecciona un preset para ver información"
        'Me.LblPreviewInfo.VerticalAlignment = ContentAlignment.Top
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.ListBoxPresets)
        Me.GroupBox1.Location = New System.Drawing.Point(12, 12)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(274, 322)
        Me.GroupBox1.TabIndex = 10
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Presets Disponibles"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.LblPreviewInfo)
        Me.GroupBox2.Location = New System.Drawing.Point(300, 12)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(310, 165)
        Me.GroupBox2.TabIndex = 11
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Vista Previa"
        '
        'PresetManagerForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(624, 461)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.BtnRefresh)
        Me.Controls.Add(Me.BtnClose)
        Me.Controls.Add(Me.BtnInfo)
        Me.Controls.Add(Me.BtnImport)
        Me.Controls.Add(Me.BtnExport)
        Me.Controls.Add(Me.BtnDelete)
        Me.Controls.Add(Me.BtnLoad)
        Me.Controls.Add(Me.BtnSave)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "PresetManagerForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Gestor de Presets de Protección"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox2.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents ListBoxPresets As ListBox
    Friend WithEvents BtnSave As Button
    Friend WithEvents BtnLoad As Button
    Friend WithEvents BtnDelete As Button
    Friend WithEvents BtnExport As Button
    Friend WithEvents BtnImport As Button
    Friend WithEvents BtnInfo As Button
    Friend WithEvents BtnClose As Button
    Friend WithEvents BtnRefresh As Button
    Friend WithEvents LblPreviewInfo As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
End Class 