Public Class PresetManagerForm
    Private parentForm As ProjectDesigner

    Public Sub New(parent As ProjectDesigner)
        InitializeComponent()
        parentForm = parent
    End Sub

    Private Sub PresetManagerForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        RefreshPresetsList()
    End Sub

    Private Sub RefreshPresetsList()
        Try
            ListBoxPresets.Items.Clear()
            Dim presets As List(Of String) = PresetManager.GetAvailablePresets()

            For Each preset As String In presets
                ListBoxPresets.Items.Add(preset)
            Next

            UpdateButtonStates()
        Catch ex As Exception
            MessageBox.Show($"Error al cargar presets: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub UpdateButtonStates()
        Dim hasSelection As Boolean = ListBoxPresets.SelectedIndex >= 0
        BtnLoad.Enabled = hasSelection
        BtnDelete.Enabled = hasSelection
        BtnExport.Enabled = hasSelection
        BtnInfo.Enabled = hasSelection
    End Sub

    Private Sub ListBoxPresets_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxPresets.SelectedIndexChanged
        UpdateButtonStates()

        If ListBoxPresets.SelectedIndex >= 0 Then
            ShowPresetPreview(ListBoxPresets.SelectedItem.ToString())
        Else
            LblPreviewInfo.Text = "Selecciona un preset para ver información"
        End If
    End Sub

    Private Sub ShowPresetPreview(presetName As String)
        Try
            Dim preset As ProtectionPreset = PresetManager.LoadPreset(presetName)
            If preset IsNot Nothing Then
                Dim info As String = $"Nombre: {preset.Name}" & vbCrLf &
                                   $"Descripción: {preset.Description}" & vbCrLf &
                                   $"Creado: {preset.Created:dd/MM/yyyy HH:mm}"

                LblPreviewInfo.Text = info
            End If
        Catch ex As Exception
            LblPreviewInfo.Text = "Error al cargar información del preset"
        End Try
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs) Handles BtnSave.Click
        If parentForm IsNot Nothing Then
            parentForm.SaveCurrentConfigurationAsPreset()
            RefreshPresetsList()
        End If
    End Sub

    Private Sub BtnLoad_Click(sender As Object, e As EventArgs) Handles BtnLoad.Click
        If ListBoxPresets.SelectedIndex >= 0 AndAlso parentForm IsNot Nothing Then
            Dim presetName As String = ListBoxPresets.SelectedItem.ToString()
            parentForm.LoadSelectedPreset(presetName)
            Me.Close()
        End If
    End Sub

    Private Sub BtnDelete_Click(sender As Object, e As EventArgs) Handles BtnDelete.Click
        If ListBoxPresets.SelectedIndex >= 0 AndAlso parentForm IsNot Nothing Then
            Dim presetName As String = ListBoxPresets.SelectedItem.ToString()
            parentForm.DeleteSelectedPreset(presetName)
            RefreshPresetsList()
        End If
    End Sub

    Private Sub BtnExport_Click(sender As Object, e As EventArgs) Handles BtnExport.Click
        If ListBoxPresets.SelectedIndex >= 0 AndAlso parentForm IsNot Nothing Then
            Dim presetName As String = ListBoxPresets.SelectedItem.ToString()
            parentForm.ExportPreset(presetName)
        End If
    End Sub

    Private Sub BtnImport_Click(sender As Object, e As EventArgs) Handles BtnImport.Click
        If parentForm IsNot Nothing Then
            parentForm.ImportPreset()
            RefreshPresetsList()
        End If
    End Sub

    Private Sub BtnInfo_Click(sender As Object, e As EventArgs) Handles BtnInfo.Click
        If ListBoxPresets.SelectedIndex >= 0 AndAlso parentForm IsNot Nothing Then
            Dim presetName As String = ListBoxPresets.SelectedItem.ToString()
            parentForm.ShowPresetInfo(presetName)
        End If
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As EventArgs) Handles BtnClose.Click
        Me.Close()
    End Sub

    Private Sub BtnRefresh_Click(sender As Object, e As EventArgs) Handles BtnRefresh.Click
        RefreshPresetsList()
    End Sub

End Class