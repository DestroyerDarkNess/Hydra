Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports dnlib.DotNet
Imports dnlib.DotNet.Emit
Imports dnlib.DotNet.Writer
Imports Guna.UI2.WinForms
Imports HydraEngine.Core
Imports HydraEngine.Protection
Imports HydraEngine.Protection.JIT
Imports HydraEngine.Protection.VM
Imports HydraEngine.References
Imports HydraEngine.Runtimes.Anti
Imports Vestris.ResourceLib

Public Class ProjectDesigner

    Public Property IsNetCore As Boolean = False
    Public Property AssemblyBytes As Byte() = Nothing
    Public Property Assembly As ModuleDefMD = Nothing
    Public Property assemblyResolver As AssemblyResolver = Nothing

    Public Property TreeViewMethodManager As MethodTreeLoader = Nothing

    Public Property Packers As List(Of HydraEngine.Models.Pack) = GetPackers()

    Public Sub New()
        SetStyle(ControlStyles.SupportsTransparentBackColor Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint, True)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.BackColor = Color.Transparent
        'Panel1.BackColor = Color.Transparent
        Panel2.BackColor = Color.Transparent
        'TabPage1.BackColor = Color.Transparent
    End Sub

    Private Sub ProjectDesigner_Load(sender As Object, e As EventArgs) Handles Me.Load
        InfoButton.Checked = True
    End Sub

    Private Sub ProjectDesigner_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        RaiseUI()
    End Sub

#Region " Methods "

    Public ManifestResource As Vestris.ResourceLib.ManifestResource = New Vestris.ResourceLib.ManifestResource()
    Public VersionResource As Vestris.ResourceLib.VersionResource = New Vestris.ResourceLib.VersionResource()
    Private FilePath As String = String.Empty
    Private WorkingDir As String = String.Empty

    Public Function LoadFile(ByVal PE_Path As String) As Boolean
        Try
            If IO.File.Exists(PE_Path) = False Then
                Me.Close()
                Me.Dispose()
                Return False
            End If

            AssemblyBytes = IO.File.ReadAllBytes(PE_Path)

            Assembly = HydraEngine.Core.Utils.LoadModule(AssemblyBytes, assemblyResolver)

            If Assembly Is Nothing Then
                If PE_Path.ToLower.EndsWith(".exe") Then
                    PE_Path = PE_Path.ToLower.Replace(".exe", ".dll")
                End If
                AssemblyBytes = IO.File.ReadAllBytes(PE_Path)
                Assembly = HydraEngine.Core.Utils.LoadModule(AssemblyBytes, assemblyResolver)
                IsNetCore = True
            End If

            If Assembly Is Nothing Then
                Throw New BadImageFormatException
            End If

            Guna2TextBox1.Text = PE_Path
            FilePath = IO.Path.Combine(IO.Path.GetTempPath, IO.Path.GetFileNameWithoutExtension(PE_Path) & "_Backup.temp")
            WorkingDir = IO.Path.GetDirectoryName(PE_Path)

            If Assembly.IsILOnly = False Then
                Me.Close()
                Return False
            End If

            If Assembly.EntryPoint Is Nothing Then
                'PackButton.Enabled = False
                'UsePacker.Checked = False
                XylosNotice2.Visible = True
                Guna2Button4.Visible = True
                LogInLabel22.Visible = True
                XylosNotice9.Visible = True
                Guna2TextBox4.Visible = True
                SetupContextMenu()
            End If

            Try
                IO.File.WriteAllBytes(FilePath, AssemblyBytes)
            Catch ex As Exception : End Try

            OutputTextBox.Text = IO.Path.Combine(WorkingDir, IO.Path.GetFileNameWithoutExtension(PE_Path) & "_HailHydra" & IO.Path.GetExtension(PE_Path))

            Try
                Guna2Panel1.BackgroundImage = Icon.ExtractAssociatedIcon(PE_Path).ToBitmap
            Catch ex As Exception
                Guna2Button1.Enabled = False
                Guna2Panel1.Enabled = False
            End Try

            LoadPackers(Assembly.EntryPoint Is Nothing)

            Try
                ManifestResource.LoadFrom(PE_Path)
                VersionResource.LoadFrom(PE_Path)

                Dim ManifestStr As String = ManifestResource.Manifest.OuterXml
                '  Dim VersionStr As String = VersionResource.x
            Catch ex As Exception
                ManifestTab.Enabled = False
                VersionTab.Enabled = False
                Guna2Panel2.Enabled = False
            End Try

            TreeViewMethodManager = New MethodTreeLoader(TreeView1, Assembly)
            TreeViewMethodManager.ExcludeCompilerGenerated = True
            TreeViewMethodManager.HighlightUserStaticMethods = True

            'TreeVewMethodManager.LoadMethods()

            Dim progress = New Progress(Of MethodTreeLoader.ProgressEventArgs)(
                                                                            Sub(args)
                                                                                Guna2ProgressBar3.Value = args.PercentComplete
                                                                                LogInLabel20.Text = args.CurrentOperation
                                                                            End Sub)

            'AddHandler TreeVewMethodManager.ProgressChanged, Sub(s, args)
            '                                                     Guna2ProgressBar3.Value = args.PercentComplete
            '                                                     LogInLabel20.Text = $"{args.CurrentOperation} ({args.PercentComplete}%)"
            '                                                 End Sub

            Dim thread As New Thread(Async Sub()
                                         Await TreeViewMethodManager.LoadMethodsAsync(progress)
                                         Me.BeginInvoke(Sub()
                                                            Guna2Panel4.Visible = False
                                                            TreeView1.Visible = True
                                                        End Sub)
                                     End Sub)
            thread.Priority = ThreadPriority.Highest
            thread.Start()

            Guna2CheckBox4.Checked = TreeViewMethodManager.All
            Guna2CheckBox5.Checked = TreeViewMethodManager.ExcludeConstructors
            Guna2CheckBox6.Checked = TreeViewMethodManager.ExcludeRedMethods
            Guna2CheckBox7.Checked = TreeViewMethodManager.ExcludeUnsafeMethods

            Dim assemblyRefs As List(Of HydraEngine.Core.DLLInfo) = HydraEngine.Core.Utils.GetUniqueLibsToMerged(Assembly, WorkingDir)

            LoadDlls(assemblyRefs)
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Loading Error")
            Me.Close()
            Return False
        End Try

        LoadPresetsComboBox()
        Return True
        'Else

        '    MessageDialog.ShowAsync("PE Invalid, please select a .NET binary")
        '    DLLMergeButton.Enabled = False
        '    ShiledButton.Enabled = False
        '    ShiledButton.Enabled = False
        '    JumpCrackButton.Enabled = False
        '    Return True
        'End If

    End Function

    Private Sub LoadPackers(ByVal isDLL As Boolean)
        PackerSelect.Items.Clear()

        For Each Pack As HydraEngine.Models.Pack In Packers
            If isDLL = True AndAlso Pack.IsCompatibleWithDLL = False Then Continue For
            PackerSelect.Items.Add(Pack.Id)
        Next

        PackerSelect.SelectedIndex = 0
    End Sub

    Private Sub LoadDlls(ByVal DLLPathEx As List(Of HydraEngine.Core.DLLInfo))

        For Each dllPath As HydraEngine.Core.DLLInfo In DLLPathEx

            If IO.File.Exists(dllPath.Path) = True Then
                Dim DLLControl As DLLItem = New DLLItem With {.DllPath = dllPath.Path, .Info = dllPath.Info}
                FlowLayoutPanel1.Controls.Add(DLLControl)
            End If

        Next

    End Sub

    Private Function GetPackers() As List(Of HydraEngine.Models.Pack)
        Dim Result As List(Of HydraEngine.Models.Pack) = New List(Of HydraEngine.Models.Pack)

        Result.Add(New HydraEngine.Protection.Packer.OrigamiPack)
        Result.Add(New HydraEngine.Protection.Packer.ILPacker)
        Result.Add(New HydraEngine.Protection.Packer.Native)
        Result.Add(New HydraEngine.Protection.Packer.NativeRC)

        Return Result
    End Function

    Private Sub Guna2ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Guna2ComboBox1.SelectedIndexChanged
        For Each Items As Control In FlowLayoutPanel1.Controls
            If TypeOf Items Is DLLItem Then
                Dim Dlli As DLLItem = DirectCast(Items, DLLItem)
                Dlli.Guna2ComboBox1.SelectedIndex = Guna2ComboBox1.SelectedIndex
            End If
        Next
    End Sub

    Private Sub Guna2Button2_Click(sender As Object, e As EventArgs) Handles Guna2Button2.Click
        Dim FilesSelected As List(Of String) = Core.Helpers.Utils.OpenFile("DLL Files|*.dll", True)

        If FilesSelected IsNot Nothing Then
            For Each Dll As String In FilesSelected
                Dim enabledControls As List(Of Control) = FlowLayoutPanel1.Controls.Cast(Of Control)().Where(Function(control) String.Equals(DirectCast(control, DLLItem).DllPath, Dll)).ToList()
                If enabledControls.Count = 0 Then
                    Dim DLLControl As DLLItem = New DLLItem With {.DllPath = Dll, .Info = Dll}
                    FlowLayoutPanel1.Controls.Add(DLLControl)
                End If
            Next
        End If
    End Sub

    Private Sub Guna2CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox3.CheckedChanged
        For Each Items As Control In FlowLayoutPanel1.Controls
            If TypeOf Items Is DLLItem Then
                Dim Dlli As DLLItem = DirectCast(Items, DLLItem)
                Dlli.Guna2CheckBox3.Checked = Guna2CheckBox3.Checked
            End If
        Next
    End Sub

    Private Sub Reset()
        If IO.File.Exists(FilePath) Then
            AssemblyBytes = IO.File.ReadAllBytes(FilePath)
            Assembly = HydraEngine.Core.Utils.LoadModule(AssemblyBytes, assemblyResolver)
        End If

        Me?.BeginInvoke(Sub()
                            RaiseUI()
                        End Sub)

    End Sub

#End Region

#Region " Tab1 "

    Private Sub Guna2Panel1_Click(sender As Object, e As EventArgs) Handles Guna2Panel1.Click
        ChangeIcon()
    End Sub

    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs) Handles Guna2Button1.Click
        ChangeIcon()
    End Sub

    Dim ChangeAppIcon As Boolean = False

    Private Sub ChangeIcon()
        Dim FilesSelected As List(Of String) = Core.Helpers.Utils.OpenFile("Image files|*.jpg;*.jpge;*.png;*.bmp;*.ico")

        If FilesSelected IsNot Nothing Then
            Dim IconFile As String = FilesSelected.FirstOrDefault

            If IO.File.Exists(IconFile) Then
                Guna2Panel1.BackgroundImage = Image.FromFile(IconFile)
                ChangeAppIcon = True
            End If
        End If
    End Sub

#End Region

#Region " UI "

    Private Sub Guna2Button12_Click(sender As Object, e As EventArgs) Handles Guna2Button12.Click
        Try
            Dim CurrentFile As String = OutputTextBox.Text
            Dim Extension As String = IO.Path.GetExtension(CurrentFile).ToLower

            Dim filter As String = "Executable (*.exe)|*.exe|Library (*.dll)|*.dll" '= $"PE File|*{Extension}"

            Dim NewSavePath As String = Core.Helpers.Utils.SaveFile(System.IO.Path.GetFileName(CurrentFile), filter, Path.GetDirectoryName(CurrentFile))

            If String.IsNullOrEmpty(NewSavePath) = False Then
                OutputTextBox.Text = NewSavePath
            End If
        Catch ex As Exception : End Try
    End Sub

    Private Sub Guna2CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox2.CheckedChanged
        RaiseUI()
    End Sub

    Private Sub Guna2ProgressBar1_VisibleChanged(sender As Object, e As EventArgs) Handles Guna2ProgressBar1.VisibleChanged
        RaiseUI()
    End Sub

    Private Sub UsePacker_CheckedChanged(sender As Object, e As EventArgs) Handles UsePacker.CheckedChanged
        RaiseUI()
    End Sub

    Private Sub Guna2CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox1.CheckedChanged
        For Each contrl As Control In Guna2GroupBox4.Controls
            If TypeOf contrl Is Guna2CheckBox Then
                DirectCast(contrl, Guna2CheckBox).Checked = Guna2CheckBox1.Checked
            End If
        Next
    End Sub

    Private Sub NamespaceCheck_CheckedChanged(sender As Object, e As EventArgs) Handles NamespaceCheck.CheckedChanged
        RaiseUI()
    End Sub

    Private Sub ModuleCheck_CheckedChanged(sender As Object, e As EventArgs) Handles ModuleCheck.CheckedChanged
        RaiseUI()
    End Sub

    Private Sub Guna2ProgressBar2_VisibleChanged(sender As Object, e As EventArgs) Handles Guna2ProgressBar2.VisibleChanged
        RaiseUI()
    End Sub

    Private Sub PESectionCustom_CheckedChanged(sender As Object, e As EventArgs) Handles PESectionCustom.CheckedChanged
        RaiseUI()
    End Sub

    Private Sub PESectionRenamerBase_CheckedChanged(sender As Object, e As EventArgs) Handles PESectionRenamerBase.CheckedChanged
        RaiseUI()
    End Sub

    Private Sub PESectionPreserve_CheckedChanged(sender As Object, e As EventArgs) Handles PESectionPreserve.CheckedChanged
        RaiseUI()
    End Sub

    Private Sub ILVMCheck_CheckedChanged(sender As Object, e As EventArgs) Handles ILVMCheck.CheckedChanged
        RaiseUI()
    End Sub

    Public Sub RaiseUI()
        Try
            Guna2ComboBox3.Enabled = Guna2CheckBox2.Checked
            Guna2Button3.Visible = Guna2CheckBox2.Checked
            Guna2TextBox3.Enabled = Guna2CheckBox2.Checked

            Guna2ProgressBar2.Visible = Guna2ProgressBar1.Visible
            PackerSelect.Enabled = UsePacker.Checked
            NamespaceCheck_Empty.Enabled = NamespaceCheck.Checked
            ModuleCheck_Invisible.Enabled = ModuleCheck.Checked
            Guna2Separator2.Visible = Guna2ProgressBar2.Visible
            PESectionCustomText.Enabled = PESectionCustom.Checked

            LogInLabel4.Enabled = Not PESectionPreserve.Checked
            PESectionExclusion.Enabled = Not PESectionPreserve.Checked
            VMComboSelect.Enabled = ILVMCheck.Checked
            ProtectVMCheck.Enabled = ILVMCheck.Checked
        Catch ex As Exception : End Try
    End Sub

    Private Sub Menu_CheckedChanged(sender As Object, e As EventArgs) Handles VMButton.CheckedChanged, InfoButton.CheckedChanged, JumpCrackButton.CheckedChanged, DLLMergeButton.CheckedChanged, ShiledButton.CheckedChanged, PackButton.CheckedChanged, CodeButton.CheckedChanged, ExtraFeaturesButton.CheckedChanged
        Dim ButtonObject As Guna.UI2.WinForms.Guna2Button = DirectCast(sender, Guna.UI2.WinForms.Guna2Button)

        If ButtonObject Is InfoButton Then

            TabControl1.SelectedTab = TabPage1

        ElseIf ButtonObject Is VMButton Then

            TabControl1.SelectedTab = TabPage8

        ElseIf ButtonObject Is JumpCrackButton Then

            TabControl1.SelectedTab = TabPage7

        ElseIf ButtonObject Is DLLMergeButton Then

            TabControl1.SelectedTab = TabPage2

        ElseIf ButtonObject Is ShiledButton Then

            TabControl1.SelectedTab = TabPage3

        ElseIf ButtonObject Is PackButton Then

            TabControl1.SelectedTab = TabPage4

        ElseIf ButtonObject Is ExtraFeaturesButton Then

            TabControl1.SelectedTab = TabPage5

        ElseIf ButtonObject Is CodeButton Then

            TabControl1.SelectedTab = TabPage6

        End If

    End Sub

    Public Property BaseChars As String = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ0123456789АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"
    Private Property BaseMode As HydraEngine.Protection.Renamer.RenamerPhase.RenameMode = HydraEngine.Protection.Renamer.RenamerPhase.RenameMode.Ascii

    Private m_flowing As String = ".̃̏̂͑̽̿̄ͯ҉̬̬̭͉͙.ͮͦ͌̑̄ͤͥͫ̕͏͉̖̪̪̤͈̝̠͓̥̹̥̺͉ͅ.̶̸̘̘̙̭̻͖̪͍̖̰͍͇̟̯̖̃͒ͧ̈́ͧ͛͑ͣ̂̌͑̽̎̔̚͢.͙̙͓͔̻̺̪͙̼̺͙̠̭̣̯̫͔̭ͥͦͫ͜͟͞.̩͈̤̼̬̬̼̘͎̻̼̠̼͓̝̯̰̌̆̋̃͗̃̅͂̂̈́̔͒̑̕͜.̧̭̫̭̮͙̣̺̳̦̝̦͍͚̟̯̟̽̂ͨ͑̈̀͟͠.̡̣̖͚̮͓͇͔͈̱̯̞͓̙̞͕͚́ͩ̾ͯ̍̏̿̆ͫ̆͛̑́͝ͅ.̛̊ͨ̆͂͌ͭ͏̸͓͕͕͓̗͙͇̤͍̦͕̥̘͇.̡͔͇͍̦͚̲͔̯̪̙̘͓͚̬̲͔̼͕̽̃̉ͫ̓̑ͫ̉ͫ̒̊͜.͎̹̫͕ͯ̐͌̐͒͛̐̎̏ͨͮ͂̒̀̚͠.̶̷̨̝̣͖͇̲̯͇̰͈̙͉͙͚͉̄͑͗̏̒ͪ̏ͮ͌͗ͬͪͥͭ̊͋͞.̜̟͕̺̣͕̥͚͔͓̠̞̳̭̠ͪ̽ͭ͒ͮ͘͝.̛̝̦͎͚̈̉̈́̈͛ͯ͑ͫ̊ͮͬ̆ͣ͂ͥ.̸͇̮͙͈͇̱͈͕̜̬̻̮ͫ͊ͭ̏͑̔͐̑ͬ̾̂ͩ͆ͫ̀.ͤ͂ͩ̀͑̒͏̷́͏͓̮̙͈̮̳̲̭̺̟̱̞͍̜̥͜.̶̷̣̮͍͇͈̝̞͓̦͐ͤͦͤͦͭ̆͒̓̀ͫ̅̐̚͡.̵̢͇̯͕͕̤̥̘͍͂̆̊ͮ̆̋̿ͧ͊ͩ̑͜.̸̧̗̜̼͖̲̟̹̞̈ͭ̊̔ͪ̐ͤ͆̇̔ͫͮ̀́.̏̅̃̓ͭ͏͇̲͎̹̖͙͎̯̥͡.̢̰͕̭̲͖͇͒́̾͋ͬ̅̈͑ͥͅͅ.͊̒̃͒́͜͏̢͉̲̹̼̥̥͖̘̼̹͈͉.̸̷̶̜̞͖̪̻̦͕͕̼̮̳͙̯̹̩̗̓͌̑ͭ̏͂̾͂̒ͭ̍̀̚.̵̡̘͕͚̳͐ͦͥ̉͘͢ͅ.̶̸̨̨͍̳̣̱͓̫̫̱̖̣͔̅ͧ̂́ͯ̓͋͋͂̾͑̈́̇̑̎̑ͭ̍͜.̴̵̘̩͍͖̻̦̣͕̗̖͔̘͓͗̈͛͂́̾ͫ͛̄͆ͤ̑͘͘.̡̛̮͇̫̮͔̲͕̫̹̘̞̱̾̈ͬ̆ͦ̈́͂̀̌̈́̆͋͆͋́.̶̴̨̩̻̮̹͔̞̻͖̭̻̲̉͆̓ͨͥ̈́̈́ͤ̅͑̆̑̔̔̍̀͘͝.̃̏̂͑̽̿̄ͯ҉̬̬̭͉͙.ͮͦ͌̑̄ͤͥͫ̕͏͉̖̪̪̤͈̝̠͓̥̹̥̺͉ͅ.̶̸̘̘̙̭̻͖̪͍̖̰͍͇̟̯̖̃͒ͧ̈́ͧ͛͑ͣ̂̌͑̽̎̔̚͢.͙̙͓͔̻̺̪͙̼̺͙̠̭̣̯̫͔̭ͥͦͫ͜͟͞.̩͈̤̼̬̬̼̘͎̻̼̠̼͓̝̯̰̌̆̋̃͗̃̅͂̂̈́̔͒̑̕͜.̧̭̫̭̮͙̣̺̳̦̝̦͍͚̟̯̟̽̂ͨ͑̈̀͟͠.̡̣̖͚̮͓͇͔͈̱̯̞͓̙̞͕͚́ͩ̾ͯ̍̏̿̆ͫ̆͛̑́͝ͅ.̛̊ͨ̆͂͌ͭ͏̸͓͕͕͓̗͙͇̤͍̦͕̥̘͇.̡͔͇͍̦͚̲͔̯̪̙̘͓͚̬̲͔̼͕̽̃̉ͫ̓̑ͫ̉ͫ̒̊͜.͎̹̫͕ͯ̐͌̐͒͛̐̎̏ͨͮ͂̒̀̚͠.̶̷̨̝̣͖͇̲̯͇̰͈̙͉͙͚͉̄͑͗̏̒ͪ̏ͮ͌͗ͬͪͥͭ̊͋͞.̜̟͕̺̣͕̥͚͔͓̠̞̳̭̠ͪ̽ͭ͒ͮ͘͝.̛̝̦͎͚̈̉̈́̈͛ͯ͑ͫ̊ͮͬ̆ͣ͂ͥ.̸͇̮͙͈͇̱͈͕̜̬̻̮ͫ͊ͭ̏͑̔͐̑ͬ̾̂ͩ͆ͫ̀.ͤ͂ͩ̀͑̒͏̷́͏͓̮̙͈̮̳̲̭̺̟̱̞͍̜̥͜.̶̷̣̮͍͇͈̝̞͓̦͐ͤͦͤͦͭ̆͒̓̀ͫ̅̐̚͡.̵̢͇̯͕͕̤̥̘͍͂̆̊ͮ̆̋̿ͧ͊ͩ̑͜.̸̧̗̜̼͖̲̟̹̞̈ͭ̊̔ͪ̐ͤ͆̇̔ͫͮ̀́.̏̅̃̓ͭ͏͇̲͎̹̖͙͎̯̥͡.̢̰͕̭̲͖͇͒́̾͋ͬ̅̈͑ͥͅͅ.͊̒̃͒́͜͏̢͉̲̹̼̥̥͖̘̼̹͈͉.̸̷̶̜̞͖̪̻̦͕͕̼̮̳͙̯̹̩̗̓͌̑ͭ̏͂̾͂̒ͭ̍̀̚.̵̡̘͕͚̳͐ͦͥ̉͘͢ͅ.̶̸̨̨͍̳̣̱͓̫̫̱̖̣͔̅ͧ̂́ͯ̓͋͋͂̾͑̈́̇̑̎̑ͭ̍͜.̴̵̘̩͍͖̻̦̣͕̗̖͔̘͓͗̈͛͂́̾ͫ͛̄͆ͤ̑͘͘.̡̛̮͇̫̮͔̲͕̫̹̘̞̱̾̈ͬ̆ͦ̈́͂̀̌̈́̆͋͆͋́.̶̴̨̩̻̮̹͔̞̻͖̭̻̲̉͆̓ͨͥ̈́̈́ͤ̅͑̆̑̔̔̍̀͘͝ḩ̷̸͎̞̬͚͙́͒̃̿̑ส็็็็็็็็็็็็็็็็็็็i͇̠̱̽͛ͣͯͭ̐͐ͩͪ̀͒̿̍̆̌ͣ̕͞ţ̈́̄ͦ͑͐ͤ̇ͯ̚͜͢͏̺͎̰̯̰̳̣̺͉͉̻̯̱͉̱̳̠̫l̢̮̝̰̖̲̯͉̱͉̤̗̯͇ͫ͋͑͋͊́͑͠e̛̼͉̝̯̼͚͇̜̹̬̼͚̥̝̟̩̮̎̾ͧ͟͝ͅr̷͎̣͙͇̦̱̺͚̬͍͎̗̺͍͈͍̔̃̆ͬ̃͌ͦ͗ͧ̓͋̓͟͟͡ͅ ̵̩̼͙̣̦͕̃ͨͧ̂ͭ͂̀͜ḣ͚͖͉͓̫̲̦͓́̆̈ͯ͒͂ͫ͛ͣ̓ͫ̄́́̕͜͜͜ą̴̢̺̼͎̩͓̱͍̯͓̻̖͓̯̿ͩͩͦ̕ͅt͂ͫ̔͋͆̀ͩͨ͂̎̓ͧ̿̈́̓̏̃ͯ̈͘͘҉͚̬̝͙̟̗̰̹̱̗ ̵̠̤̼̬̩͔̲̖̎̍̈̌̾̎̋̂̓ͬ̒ͫ̽ͭ́̕͠n̛̲͙̻̤̮̥̠͇͇͖͎̘̠̲ͥͣ̋͛ͨ̀i̧͓͖͈̭͔͉̼ͪͫ̓͂̔̿͠ͅč̨̆̉͂̑͞͏̻̠̖̼̹̻̹̯͇͙̰̪̯hͭ͌ͦ̉̊͐͂҉̸͇̹̹̬̖͕̱͕͕̠̗̀͘͝tͤͫͫͧ̈́͂͛ͭ̉ͧ͛ͫ̚҉̴͚̯̭̲̫̦͖̮̭͖̗͎̳̟̀͘s̸ͧ͗̌͊ͨ̐̅̇͟҉͈̤̘͉̤̯̝͈͚ ͤ͆͆ͨ̓҉̤̣̩̠̩̯̩̱͕̹͜͝f̡̞͔̮͖̩͔̀̆̅̓̈ͪͥ͋͊̉ͪ̇̉̃̔͋̃̈́͟͠͝ä̵̸̺̖͓͖̳̬̲̲͎͎͔̈͋͑͋ͦͅͅl̻̟̲̞̘͚̤͎͉̯̫̹̜̥̳͈̙ͧ͋̇ͫ͋̎ͯ̋͂ͮ̈͂̾ͯ̎̊̾ͯ͘͢ͅs̴̡͍̖̖͕̱̫̤̣͛ͩͤ͆̅ͣ͐̿ͣ͐̔ͨ̄ͫͩ̄̍͘̕͜c̵̷̨͇̰̰̼̝̝̼̤͎̯̺̰͕̤̤͇ͧͦ̄̇̓ͩ̎͂̊ͯ͋͋̋̀ͬͧ͗̔̚͝͡ͅͅh͓͙̩̭̬̠̜͇̗̮̐ͥͧͭ̆͆̔ͬ́̄͊ͮ͡͝͠ ̩̹̥̯̲͉͔̟͕͎̪͔̱̬̌ͭ͗̔̏̊̚͡͞g̴̴̫͖̥̲̦͉̩̲̪̹̙̘̩̣̯̜̱͌ͩ͑̆̿̏̽ͤ͂ͩ́͢͠e̤̳͈̹͉̹̪̥̜̲͙͕͍̟̱̱̳͗̽ͤ̈́̽̽̊͢͡ḿ͈̮͓͇̞̯͍̦͖̟͔̫͈̏̑̋̂̒ͬ͌̌̓̄͢͞ą̶̴͈̳̥͙͚͓͉̟̬̤͋͒ͫ̓̿́̒͐͒͘͡c̡̞̪̞̣̦̖̙̬̜̜̋̿͐̇̓̋̃͆̚h̨̛̝͓͚̱͍͕̝̬̯̩̓̽̉͌͊̇͐̒̈͋͋̌ͩ̋ͭt̶̄́͑̐҉҉̟̭̟͓̜̩͖̲͔̀ ̷̧̡̦̞̝̯̥͍̻͚̠̞̣̯͎̇̓̇̐̓́͝h̡̺̳͇̤̬̻̮̭͇̿̑̔̽ͮ̉̏̃͂̄̌̍͒̑̇ͪ̑͡͞ĭ̵͔͙̞̻̰̻̬̖͇̩͔̪̮̩̘̔ͪͥ̈́͋͞t̸̝̪̹̲̤̜̓͌ͤ̍ͫͧ͋͋ͣ͆̈́ͩͯ̒ͮ̊ͧ̕͢͜ͅl̈́ͦ̈̌͆ͧ͏̷̛̮̼̬͓̞̻̣̼͙ͅe̗͚̺̰͎͖̥̙̻͕̮͕̱͇̓̑ͫ͋ͭͯ̍ͨ̌͗̊̔̒̈́̽̕͜͝ͅṛ̶̷̟̹̥̹̬̖͖͇̬̭̲̬̠̮ͣͧ̓̓̈́̅͢ ̨̛̘̲͉̮̲̹̳͔̗̣̣̗̱̱̘͚ͦ͋̀̍̃̑ͣ̍͒̇ͭ̆ͧ͒͒̋̕h̷͔̠͈̙̝̻̺͔͕̤ͯ̃̎̋͑ͫ̎̾̃̿̀̄́̀͞a̍̏͛̔͆͗͒̆̐́̈ͥ̃͌͝҉̧҉̷̪̳̻͓̘̜̘͔̘̞̱̫͈̹ͅť̷̢̫̰̦̫̯̮̟͇̍͊̒̅̀̐͊ͯ́̒̅ͤͅ ̵̮̤̳̺̤̼̝̉̑̎ͧ̏̀̽̽́n̴̴̯̹̮̣͉̹̝̑͑̐̿ͮ̈̆̔́̏ͥ͋ͨ̒͘͘͟i̲̬͓̯̋͑̂̋͋͡c͑̒̈́ͫ̓̎͆̃̃ͬͯ͜͏͓̲̝̺̥̘͍͚̕ḩ̜͖̤̼͇̳̳̭̻͖̳̙͑͌̑̓͒ͦ͂ͮ̅̅͌̈ͭͭ̄ͅṫ̴͎̺̭̺̞̺̮̼̣͔͎͔̗̱̭̄̆̓͋̔͝s̛͓͇̮̹̙̫̮̦̪̜̋ͪ̊̊ͮ̎͌̂̂̿̽̔̉̓̍̔͆̕ ̛̽͌͋ͩ̃̈̍ͨ̅ͦ̀̏̍̓̑̍̊ͧ͘͏̪͔͇ͅf̢̰̲̺͙̝̣͕̭̝̙͍ͧ̌ͤͮ͛͋ͭ́̓̽̔̈́̂ͤ̉̆ͩ̚͘ͅa̧̡ͪ̊̊ͩ̈͠͏҉̠̬̝̻͙̰̖̻̼̖̘̠̺̝l̸̷͊͒ͨ̄̓̂҉̥̩̮̳̯̠̻͎̹͈̟̠̫̮̫̠̖̥͙s̶͒̓̇͑͗̍̿̐ͮ҉͇͖͓̣͉̗̰̯͎̖͎̱c̶͇̭̣͍̞̝͓͇̫̯̜̫̞͉̑ͤͧ̎̒̈ͯͣͥ̍ͪ̌̎̒́h̵̢̼̮̖͎̭̭͇͚̮͙͙͇̗̤̝̺͚̐̏͋ͬ̋̿̎ͭ̂̾̂̓ͪ̏̋̀ ̵̨̛̱̖̫͇̱͈̞̭̱́̔ͪ̋̎̎ͭͨ̈̿ͤ̎̿͟ͅg̉͂͛̍̓ͫ̇̿̎͐̓̏͏͏̴̤̗̙͖͙̱̺͎̖̩͝ę̵̞͖̭̳͔̻͉̯̻̯̣̈́́̈̊̽̾͗ͣ̃͊ͬ̔ṃ̶̳̫̲̩̺͍̝̰̻̱̖̦̪̘̠̏ͫ̎ͤ̓ͣ̾͊̑͒͗̋͂̉̈́͜͞a̰̯͔̗̠͖̣̬̖͐ͬ̏͐ͬ͊̂̍̇ͣͩ́͠͞c̨̡̬̥̟̩̠̬̟̪͖̙̮̺̩͍̮̝ͥ̌̉ͣ͊̈͌̓̈̉̈͐̿̈ͬ̀h̛̬͚̝͉̮̝͉̥̺̩̼̞̙̖͎̮̳̒͂̋̋ͬ̓͋̂̊̚̕t͒ͮ̿ͤͬ̎ͭ͌̅̂̾̐̉ͦ͌ͧͯ͋̈͏̮̮̰̻̕͜͞ ̸͎̥̞̟͙͚͙̥ͬ̈́̋̔̿ͣͨͧͫ͒̿ͬͫͧ̊͢͜͠ḫ̝̘̤̰͐͆ͤ̑̒͛ͩ́͞iͤ̓͑̊̏ͥ̀͘͞҉̨̗̬͉̜̘̜t̑ͨ͋̾ͦ̋̊ͤ̔̒̑̿̓́͏̵̛̱͚̖͓̲̕l̶͍̳̳͎͓̀ͭ̎̉̌̓̊̌̍̍̀̕e̵̡͇͈͉̥̼̼̺͎͉̦ͮ̾̒̃ͫ̃͒̃̓́̅͆ͬ͗ͨ̿ͥ̂̚͝͡ŗ̡̊͋̾ͩ̽͆̋̈́͐͊̂ͮͨ̉̏ͨ͐͐̆̕͞͏͍͍̰̻̖̥ ͨ́̿̃̇ͯ̾̕͢҉̩͇̟̪̥̬͍̲͈͔͔͍̼̭͇̣h̷̻̝̫̪͚̦͙͉͎̥̦̳͉̖̃ͥ̅͗͢ḁ̛̪̗̮̦̳̪̭̞̗̠̟̈́̌͐̈́ͦ̄̉̎ͬ͆̒̉̕͟t̶̛͔͍͔͎̫̞̖͓̰̒̇̆ͯ̀ͥ̈́̏̓̀ͮͥ̍̀ ̨ͥ̄̓ͩ̿̃̿̊̈̔͒͗͊ͭ̽ͥͥ͐҉̶̸̲̻̱̩̖̪̹͈̙̩͎̲̘̙n"

    Private Sub Guna2ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Guna2ComboBox2.SelectedIndexChanged
        BaseMode = HydraEngine.Protection.Renamer.RenamerPhase.RenameMode.Ascii
        Select Case Guna2ComboBox2.SelectedIndex
            Case 0
                BaseChars = RandomString("ABCDEFGHIJKLMNÑOPQRSTUVWXYZ", Guna2TrackBar1.Value)
            Case 1
                BaseChars = RandomString("0123456789", Guna2TrackBar1.Value)
            Case 2
                BaseChars = RandomString("ABCDEFGHIJKLMNÑOPQRSTUVWXYZ0123456789", Guna2TrackBar1.Value)
            Case 3
                BaseChars = RandomString("阿贝色德饿艾弗日阿什伊鸡卡艾勒艾马艾娜哦佩苦艾和艾丝特玉维独布勒维伊克斯伊格黑克贼德", Guna2TrackBar1.Value)
            Case 4
                BaseChars = RandomString("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ", Guna2TrackBar1.Value)
            Case 5
                BaseChars = RandomString("れづれなるまゝに日暮らし硯にむかひて心にうりゆくよな事を、こはかとなく書きつくればあやうこそものぐるほけれ。", Guna2TrackBar1.Value)
            Case 6
                BaseChars = RandomString("αβγδεζηθικλµνξοπρστυϕχψω", Guna2TrackBar1.Value)
            Case 7
                BaseChars = RandomString("☹☺☻☼☽☾☿♀♁♂♔♕♖♗♘♙♚♛♜♝♞♟♠♡♢♣♤♥♦♧♩♪♫♬♭♮♯♻♼♿⚐⚑⚒⚓⚔⚕⚖⚠⚢⚣⚤⚥⚦⚧⚨⚩⛄⛅⚽✔✓✕✖✗✘✝✞✟❗❓❤☸", Guna2TrackBar1.Value)
            Case 8
                BaseChars = m_flowing
            Case 9
                BaseChars = RandomString(Guna2TrackBar1.Value)
            Case 10
                BaseMode = HydraEngine.Protection.Renamer.RenamerPhase.RenameMode.Key
            Case 11
                BaseMode = HydraEngine.Protection.Renamer.RenamerPhase.RenameMode.Normal
            Case 12
                BaseMode = HydraEngine.Protection.Renamer.RenamerPhase.RenameMode.Invisible
        End Select

        BoosterToolTip1.SetToolTip(LogInLabel13, BaseChars)
    End Sub

    Private Sub Guna2TrackBar1_Scroll(sender As Object, e As ScrollEventArgs) Handles Guna2TrackBar1.Scroll
        LogInLabel17.Text = Guna2TrackBar1.Value
    End Sub

    Private random As Random = New Random()

    Public Function RandomString(ByVal length As Integer) As String
        Dim chars As String = "日本書紀العالمحالعجلة林氏家族การดำน้ำดูปะการังसंस्कृतम्संस्कृतावाक्" & "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" & "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ" & "αβγδεζηθικλµνξοπρστυϕχψω" & "れづれなるまゝに日暮らし硯にむかひて心にうりゆくよな事を、こはかとなく書きつくればあやうこそものぐるほけれ。"
        ' chars += m_flowing.ToString
        Return New String(Enumerable.Repeat(chars, length).[Select](Function(s) s(random.[Next](s.Length))).ToArray())
    End Function

    Public Function RandomString(ByVal Chars As String, ByVal length As Integer) As String
        Return New String(Enumerable.Repeat(Chars, length).[Select](Function(s) s(random.[Next](s.Length))).ToArray())
    End Function

#End Region

#Region " Protect "

    Private Async Sub BuildButton_Click(sender As Object, e As EventArgs) Handles BuildButton.Click
        'AntiTamperEx = True
        'Dim AsmDef As ModuleDefMD = ModuleDefMD.Load(Guna2TextBox1.Text)
        'Dim Pack = New HydraEngine.Protection.CodeEncryption.AntiTamperNormal
        'Dim NativeBuild As Boolean = Await Pack.Execute(AsmDef)

        'Dim writerOptions As New ModuleWriterOptions(AsmDef)
        'With writerOptions
        '    .Logger = DummyLogger.NoThrowInstance
        '    '.MetadataOptions = New MetadataOptions With {
        '    '    .Flags = MetadataFlags.KeepOldMaxStack
        '    '}
        '    .WritePdb = False
        'End With
        ''writerOptions.Cor20HeaderOptions.Flags = dnlib.DotNet.MD.ComImageFlags.ILOnly
        'AddHandler writerOptions.WriterEvent, AddressOf AssemblyWriterEvent
        'AsmDef.Write(OutputTextBox.Text, writerOptions)
        'MsgBox("Packed: " & NativeBuild)
        'Exit Sub

        'Dim AsmDef As ModuleDefMD = ModuleDefMD.Load(Guna2TextBox1.Text)

        'Dim Renamer = New HydraEngine.Protection.Renamer.RenamerPhase With {.tag = Guna2TextBox9.Text, .Mode = BaseMode, .BaseChars = BaseChars, .Length = Guna2TrackBar1.Value}
        ''Renamer.Resources = ResourcesCheck.Checked
        'Renamer.Namespace = NamespaceCheck.Checked
        ''Renamer.NamespaceEmpty = NamespaceCheck_Empty.Checked
        'Renamer.ClassName = ClassName.Checked
        ''Renamer.Methods = MethodsCheck.Checked
        ''Renamer.Properties = PropertiesCheck.Checked
        ''Renamer.Fields = FieldsCheck.Checked
        ''Renamer.Events = EventsCheck.Checked
        ''Renamer.ModuleRenaming = ModuleCheck.Checked
        ''Renamer.ModuleInvisible = ModuleCheck_Invisible.Checked
        'Dim NativeBuild As Boolean = Await Renamer.Execute(AsmDef)

        ''Dim writerOptions As New ModuleWriterOptions(AsmDef)
        ''writerOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll
        ''writerOptions.Cor20HeaderOptions.Flags = dnlib.DotNet.MD.ComImageFlags.ILOnly
        ''writerOptions.MetadataLogger = DummyLogger.NoThrowInstance
        ''AddHandler writerOptions.WriterEvent, AddressOf AssemblyWriterEvent
        'AsmDef.Write(OutputTextBox.Text)
        ''MsgBox("Packed: " & NativeBuild)
        'Exit Sub

        Console.Clear()
        Dim Protections As List(Of HydraEngine.Models.Protection) = MakeConfig()
        Build(Assembly, assemblyResolver, Protections)
    End Sub

    Public Function MakeConfig() As List(Of HydraEngine.Models.Protection)
        Dim Result As List(Of HydraEngine.Models.Protection) = New List(Of HydraEngine.Models.Protection)
        Dim Tag As String = Guna2TextBox2.Text
        Dim VMSelected = VMComboSelect.SelectedIndex

        If String.IsNullOrWhiteSpace(Tag) Then
            Tag = "HailHydra"
        End If

        'Result.Add(New HydraEngine.Protection.Misc.Watermark)

        If Renamer.Checked = True And RenamerEngineCombo.SelectedIndex = 1 Then
            'Dim Renamer = New HydraEngine.Protection.Renamer.RenamerPhase With {.tag = Guna2TextBox9.Text, .Mode = BaseMode, .BaseChars = BaseChars, .Length = Guna2TrackBar1.Value}

            Dim Renamer = New HydraEngine.Protection.Renamer.AsmResolver_Renamer With {.tag = Guna2TextBox9.Text, .Mode = BaseMode, .BaseChars = BaseChars, .Length = Guna2TrackBar1.Value}
            Renamer.Resources = ResourcesCheck.Checked
            Renamer.Namespace = NamespaceCheck.Checked
            Renamer.NamespaceEmpty = NamespaceCheck_Empty.Checked
            Renamer.ClassName = ClassName.Checked
            Renamer.Methods = MethodsCheck.Checked
            Renamer.Properties = PropertiesCheck.Checked
            Renamer.Fields = FieldsCheck.Checked
            Renamer.Events = EventsCheck.Checked
            Renamer.ModuleRenaming = ModuleCheck.Checked
            Renamer.ModuleInvisible = ModuleCheck_Invisible.Checked
            'Renamer.ApplyCompilerGeneratedAttribute = Not ILVMCheck.Checked
            Renamer.UnsafeRenamer = UnsafeMode.Checked
            Result.Add(Renamer)

        End If

        If ResourceEncryptionCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Renamer.ResourceEncryption)
        End If

        If ImportProtection.Checked Then
            Result.Add(New HydraEngine.Protection.Import.ImportProtection)
        End If

        If SUFconfusionCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Misc.SUFconfusion)
        End If

        If ProxyClassesObject.Checked Then
            Result.Add(New HydraEngine.Protection.Proxy.ProxyClass With {.Unsafe = ProxyClassesObjectUnsafeCheck.Checked})
        End If

        If ProxyMethods.Checked Then
            Result.Add(New HydraEngine.Protection.Proxy.ProxyMeth)
        End If

        If MoveVariables.Checked = True Then
            Result.Add(New HydraEngine.Protection.Proxy.ProxyVariable)
        End If

        If L2F.Checked = True Then
            Result.Add(New HydraEngine.Protection.LocalF.L2F With {.tag = Tag})
        End If

        If EntryPointMover.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.EntryPointMover)
        End If

        If Method2DelegateCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.MethodToDelegate With {.DynamicEntryPoint = Method2DynamicEntryPoint.Checked})
        End If

        If ReduceMetadata.Checked = True Then
            Result.Add(New HydraEngine.Protection.Misc.ReduceMetadataOptimization)
        End If

        If NopAttack.Checked = True Then
            Result.Add(New HydraEngine.Protection.Dnspy.NopAttack)
        End If

        If StringEncryption.Checked = True Then
            Result.Add(New HydraEngine.Protection.String.StringEncryption)
        End If

        If ProxyStrings.Checked Then
            Result.Add(New HydraEngine.Protection.Proxy.ProxyString With {.BaseChars = BaseChars, .DynamicInstructions = Dynamic_STRINGS.Checked})
        End If

        If ReplaceObfuscationCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.String.ReplaceObfuscation)
        End If

        If IntConfusion.Checked = True Then
            Result.Add(New HydraEngine.Protection.INT.AddIntPhase)
        End If

        If StringsHider.Checked = True Then
            Result.Add(New HydraEngine.Protection.String.StringsHider With {.tag = Tag})
        End If

        If Calli.Checked = True Then
            Result.Add(New HydraEngine.Protection.Calli.CallToCalli With {.BaseChars = BaseChars})
        End If

        If ProxyInt.Checked = True Then
            Result.Add(New HydraEngine.Protection.Proxy.ProxyInt With {.BaseChars = BaseChars, .DynamicInstructions = Dynamic_INT.Checked})
        End If

        If MutationV2Check.Checked = True Then
            Result.Add(New HydraEngine.Protection.Mutations.Mutatorv2 With {.UnsafeMutation = MutationV2Check_Unsafe.Checked})
        End If

        If EncodeIntergers.Checked = True Then
            Result.Add(New HydraEngine.Protection.INT.IntEncoding)
        End If

        If Renamer.Checked = True And RenamerEngineCombo.SelectedIndex = 0 Then
            'Dim Renamer = New HydraEngine.Protection.Renamer.RenamerPhase With {.tag = Guna2TextBox9.Text, .Mode = BaseMode, .BaseChars = BaseChars, .Length = Guna2TrackBar1.Value}

            Dim Renamer = New HydraEngine.Protection.Renamer.Renamer With {.tag = Guna2TextBox9.Text, .BaseChars = BaseChars, .Length = Guna2TrackBar1.Value}
            Renamer.Resources = ResourcesCheck.Checked
            Renamer.Namespace = NamespaceCheck.Checked
            Renamer.NamespaceEmpty = NamespaceCheck_Empty.Checked
            Renamer.ClassName = ClassName.Checked
            Renamer.Methods = MethodsCheck.Checked
            Renamer.Properties = PropertiesCheck.Checked
            Renamer.Fields = FieldsCheck.Checked
            Renamer.Events = EventsCheck.Checked
            Renamer.ModuleRenaming = ModuleCheck.Checked
            Renamer.ModuleInvisible = ModuleCheck_Invisible.Checked
            Renamer.ApplyCompilerGeneratedAttribute = Not ILVMCheck.Checked
            Result.Add(Renamer)

        End If

        If Renamer.Checked = True And CodeOptimizerCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.CodeOptimizer.OptimizeCode)
        End If

        If FakeObfuscation.Checked = True Then
            Result.Add(New HydraEngine.Protection.Misc.FakeObfuscation)
        End If

        If AddJunkCode.Checked = True Then
            Result.Add(New HydraEngine.Protection.Misc.JunkCode With {.tag = Tag, .BaseChars = BaseChars, .number = Math.Round(Guna2NumericUpDown1.Value)})
        End If

        If ControlFlow.Checked Then
            Result.Add(New HydraEngine.Protection.ControlFlow.ControlFlow With {.StrongMode = ControlFlowStrongModeCheck.Checked})
        End If

        If SugarControlFlowCheck.Checked Then
            Result.Add(New HydraEngine.Protection.ControlFlow.Sugar_ControlFlow) 'With {.StrongMode = ControlFlowStrongModeCheck.Checked}
        End If

        If SugarControlFlowCheck.Checked Then
            Result.Add(New HydraEngine.Protection.ControlFlow.KroksControlFlow) 'With {.StrongMode = ControlFlowStrongModeCheck.Checked}
        End If

        If EXGuardControlFlowCheck.Checked Then
            Result.Add(New HydraEngine.Protection.ControlFlow.EXGuard_ControlFlow) 'With {.StrongMode = ControlFlowStrongModeCheck.Checked}
        End If

        If ProxyReferences.Checked Then
            Result.Add(New HydraEngine.Protection.Proxy.ProxyReferences With {.Unsafe = ProxyReferencesUnsafeCheck.Checked})
        End If

        If Mutator.Checked = True Then
            Result.Add(New HydraEngine.Protection.Mutations.MutationProt)
            'Result.Add(New HydraEngine.Protection.Mutations.Mutator)
            If MutationCheck_Unsafe.Checked Then Result.Add(New HydraEngine.Protection.Mutations.Melting)
        End If

        If AntiDecompilerCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Decompiler.AntiDecompiler)
        End If

        If InvalidOpcodes.Checked = True Then
            Result.Add(New HydraEngine.Protection.Invalid.InvalidOpcodes)
        End If

        If InvalidMD.Checked = True Then
            Result.Add(New HydraEngine.Protection.Invalid.InvalidMDPhase)
        End If

        If StackUnfConfusion.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.StackUnfConfusion)
        End If

        If HideMethods.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.HideMethods)
        End If

        If AntiProxyCheck.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiProxy)
        End If

        If ExeptionManager.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Exceptions.ExpMan)
        End If

        If ElevationEscale.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.AdministratorRights.RuntimeSInj)
        End If

        If AntiDebug.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiDebug)
        End If

        If JitFuckerCheck.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.JitFucker)
        End If

        If AntiDump.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiDump)
        End If

        If ExtremeADCheck.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.ExtremeAD)
        End If

        If AntiHTTPDebugCheck.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiHTTPDebug)
        End If

        If AntiInvoke.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiInvoke)
        End If

        If AntiTamper.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiTamper)
        End If

        If Antide4dot.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiDe4dot)
        End If

        If AntiMalicious.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.Antimanything)
        End If

        If AntiILDasm.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiILDasm)
        End If

        If AntiAttachCheck.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.AntiAttach)
        End If

        If ThreadHiderCheck.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.ThreadsHider)
        End If

        If BypassAmsiCheck.Checked = True Then
            Result.Add(New HydraEngine.Runtimes.Anti.BypassAmsi)
        End If

        If DynamicMethodsCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.DynamicCode)
        End If

        If ResourceCompressAndEncryptCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Renamer.ResourceCompressEncryption)
        End If

        If ILVMCheck.Checked And VMSelected = 0 Then
            Result.Add(New HydraEngine.Protection.VM.Virtualizer)
        End If

        If ILVMCheck.Checked And VMSelected = 1 Then
            Result.Add(New HydraEngine.Protection.VM.EXGuard)
        End If

        If CctorHider.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.cctorHider)
        End If

        If DynamicCctorCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.IL2Dynamic)
        End If

        If CctorL2FCheck.Checked = True Then
            Result.Add(New HydraEngine.Protection.Method.cctorL2F)
        End If

        If MethodError.Checked = True Then
            Result.Add(New HydraEngine.Protection.CodeEncryption.AntiTamperNormal)
            'Result.Add(New HydraEngine.Protection.Method.MethodError)
        End If

        'Result.Add(New HydraEngine.Protection.INT.UnmanagedInteger)

        Return Result
    End Function

    Public PreserveAll As Boolean = False
    Public AntiTamperEx As Boolean = False
    Public InvalidMetaData As Boolean = False
    'Public VMEnabled As Boolean = False

    Public Sub Build(ByVal Asm As ModuleDef, ByVal AsmResolver As AssemblyResolver, ByVal ProtectConfig As List(Of HydraEngine.Models.Protection))
        LogTextBox.Text = ""
        BuildButton.Enabled = False
        Guna2ProgressBar1.Value = 0
        Guna2ProgressBar1.Visible = True

        PreserveAll = PreserveAllCheck.Checked

        Dim VMEnabled As Boolean = ILVMCheck.Checked
        'If VMEnabled = True Then PreserveAll = True

        AntiTamperEx = MethodError.Checked
        InvalidMetaData = InvalidMetaDataCheck.Checked

        Dim DynMethods As Boolean = DynamicMethodsCheck.Checked

        Dim SingPE As Boolean = Guna2CheckBox2.Checked
        Dim CloneCert As Boolean = Not (Guna2ComboBox3.SelectedIndex = 0)
        Dim Cert As String = Guna2TextBox3.Text

        If Guna2CheckBox2.Checked Then SingPE = File.Exists(Cert)

        Dim ProtectVMRuntime As Boolean = ProtectVMCheck.Checked
        Dim ProtectAntiDump As Boolean = ProtectAntiDumpCheck.Checked

        Dim selectedText As String = PackerSelect.Items(PackerSelect.SelectedIndex).ToString()
        Dim selectedPack As HydraEngine.Models.Pack = Packers.FirstOrDefault(Function(p) p.Id = selectedText)

        Dim TempPath As String = IO.Path.Combine(IO.Path.GetTempPath, "HydraTempBuild")

        If IO.Directory.Exists(TempPath) Then
            IO.Directory.Delete(TempPath, True)
        End If

        IO.Directory.CreateDirectory(TempPath)

        Dim AsmDef As ModuleDef = Asm

        If UsePacker.Checked = False Then
            selectedPack = Nothing
        ElseIf AsmDef.EntryPoint Is Nothing Then
            If EntryPointToken > 0 Then
                Dim Method As MethodDef = AsmDef.ResolveToken(EntryPointToken)
                If Method IsNot Nothing Then AsmDef.EntryPoint = Method
            End If
        End If

        Dim AsmRef As AssemblyResolver = AsmResolver
        Dim OriginalPath As String = Guna2TextBox1.Text
        Dim AsmMap As AssemblyMap = New AssemblyMap
        AsmMap.Update(AsmDef)

        If selectedPack IsNot Nothing Then selectedPack.assemblyMap = AsmMap

        Dim BackupPath As String = IO.Path.Combine(TempPath, IO.Path.GetFileNameWithoutExtension(OutputTextBox.Text) & IO.Path.GetExtension(OriginalPath))
        Dim PackedPath As String = IO.Path.Combine(TempPath, IO.Path.GetFileNameWithoutExtension(OriginalPath) & "_Hydra" & IO.Path.GetExtension(OriginalPath))
        Dim TempPreOuputPath As String = IO.Path.Combine(TempPath, IO.Path.GetFileNameWithoutExtension(OriginalPath) & "_PreOuput" & IO.Path.GetExtension(OriginalPath))

        Dim Ouput As String = OutputTextBox.Text
        Dim IconPath As String = IO.Path.Combine(IO.Path.GetTempPath, "Icontemp.ico")
        Try
            If File.Exists(IconPath) Then File.Delete(IconPath)
            If Guna2Panel1.BackgroundImage IsNot Nothing Then
                Using fs As New FileStream(IconPath, FileMode.Create, FileAccess.Write)
                    Hydra.Core.Helpers.Utils.ConvertToIcon(Guna2Panel1.BackgroundImage, fs, 48, True)
                End Using
            End If
        Catch ex As Exception : End Try

        Try
            If IO.File.Exists(Ouput) Then
                IO.File.Delete(Ouput)
            End If
        Catch ex As Exception
            Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
            Writelog("It is possible that the file is being used by another process, or is running, try to delete it manually and/or close the process with the task manager.")
            Dim Stack As String = ex.StackTrace
            If String.IsNullOrEmpty(Stack) = False Then
                Writelog("Source :")
                Writelog(Stack)
                Writelog("")
            End If
            Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
            BuildButton.Enabled = True
            Guna2ProgressBar1.Visible = False
            Reset()
            Exit Sub
        End Try

        Dim JitPath As String = IO.Path.Combine(IO.Path.GetDirectoryName(Ouput), IO.Path.GetFileNameWithoutExtension(Ouput) & "_JIT" & IO.Path.GetExtension(Ouput))
        Dim DllEmbed As Boolean = DLLEmbeder.Checked

        Dim DllsToMerged As List(Of String) = New List(Of String)
        Dim DllsToEmbed As List(Of String) = New List(Of String)
        Dim DllsToEmbedLibz As List(Of String) = New List(Of String)
        Dim DllsToEmbedResources As List(Of String) = New List(Of String)
        Dim Embed As List(Of String) = Nothing
        Dim Libz As Boolean = False

        Dim Runtime As FileInfo = New FileInfo("Runtime.dll")

        If DllEmbed = True Then
            Dim enabledControls As List(Of Control) = FlowLayoutPanel1.Controls.Cast(Of Control)().Where(Function(control) DirectCast(control, DLLItem).IsEnabled).ToList()

            If enabledControls.Count = 0 Then
                DllEmbed = False
            Else

                DllsToMerged.AddRange(enabledControls.Where(Function(control) DirectCast(control, DLLItem).Merged).ToList().Select(Function(control) DirectCast(control, DLLItem).DllPath).ToList())
                DllsToEmbed.AddRange(enabledControls.Where(Function(control) DirectCast(control, DLLItem).Embed).ToList().Select(Function(control) DirectCast(control, DLLItem).DllPath).ToList())
                DllsToEmbedLibz.AddRange(enabledControls.Where(Function(control) DirectCast(control, DLLItem).Libz).ToList().Select(Function(control) DirectCast(control, DLLItem).DllPath).ToList())
                DllsToEmbedResources.AddRange(enabledControls.Where(Function(control) DirectCast(control, DLLItem).Resources).ToList().Select(Function(control) DirectCast(control, DLLItem).DllPath).ToList())

            End If
        End If

        Dim OldKind As ModuleKind = AsmDef.Kind
        Dim UnmmanagedStr = UnmanagedStringCheck.Checked
        Dim ExportEntry As Boolean = ExportEntryPoint.Checked
        Dim DontRenameSection As Boolean = PESectionPreserve.Checked
        Dim CustomRenameSection As Boolean = PESectionCustom.Checked
        Dim CustomSectionName As String = String.Empty
        If CustomRenameSection Then CustomSectionName = PESectionCustomText.Text
        Dim SectionExclusion As String = PESectionExclusion.Text
        Dim OptimizeCode = CodeOptimizerCheck.Checked
        Dim AplyJitHook As Boolean = JITHookCheck.Checked
        Dim VMStrigns As Boolean = VirtualizeStringsVM.Checked
        Dim VM_Selected_Methods As List(Of UInteger) = Nothing
        Dim AntiTamp As Boolean = AntiTamper.Checked

        If TreeViewMethodManager IsNot Nothing Then VM_Selected_Methods = TreeViewMethodManager.GetSelectedMethodTokens()

        Dim ExitMethod As String = ""

        Select Case AppClosesMethod.SelectedIndex
            Case 0
                ExitMethod = "crash"
            Case 1
                ExitMethod = "system"
            Case 2
                ExitMethod = "message"
        End Select

        Dim thread As New Thread(Async Sub()

                                     AsmDef.Write(BackupPath)

                                     'If VMEnabled And File.Exists(VMRuntimePath) = True Then

                                     'End If

                                     'If ExportEntry = True Then
                                     '    Try

                                     '        Dim MainMethod As MethodDef = AsmDef.EntryPoint

                                     '        Dim TypeCl As TypeDef = AsmDef.GetTypes().FirstOrDefault(Function(t) t.Name = "Program")
                                     '        MainMethod = TypeCl.Methods.FirstOrDefault(Function(m) m.Name = "Main")
                                     '        Writelog("Entry: " & TypeCl.Namespace.String & "." & MainMethod.Name.String)

                                     '        MainMethod.ExportInfo = New MethodExportInfo()
                                     '        MainMethod.IsUnmanagedExport = True
                                     '        Dim opts As ModuleWriterOptions = New ModuleWriterOptions(AsmDef)
                                     '        'opts.ModuleKind = ModuleKind.Dll
                                     '        'opts.PEHeadersOptions.Characteristics = Characteristics.Dll
                                     '        opts.Cor20HeaderOptions.Flags = 0
                                     '        AsmDef.Write(Ouput, opts)
                                     '        'Core.Helpers.Utils.Sleep(3)
                                     '        'AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)
                                     '        Core.Helpers.Utils.Sleep(1)
                                     '        Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"PreBuild.EntryPoint.Exporter", "EntryPoint Exporter", ""}))
                                     '        Exit Sub
                                     '    Catch ex As Exception
                                     '        Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                     '        Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"EntryPoint Exporter", "EntryPoint/Export", ex.Message}))
                                     '        Dim Stack As String = ex.StackTrace
                                     '        If String.IsNullOrEmpty(Stack) = False Then
                                     '            Writelog("Source :")
                                     '            Writelog(Stack)
                                     '            Writelog("")
                                     '        End If
                                     '        Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                     '    End Try
                                     'End If

                                     Try
                                         resolveModule(AsmDef)
                                         simplifyModule(AsmDef)
                                     Catch ex As Exception
                                         Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                         Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Primary.Optimizer", "Resolve/Simplify", ex.Message}))
                                         Dim Stack As String = ex.StackTrace
                                         If String.IsNullOrEmpty(Stack) = False Then
                                             Writelog("Source :")
                                             Writelog(Stack)
                                             Writelog("")
                                         End If
                                         Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                     End Try

                                     Dim ProtectInt As Integer = 0
                                     Dim ProtectErrors As Integer = 0

                                     'If DllEmbed = True Then

                                     '    If Not DllsToMerged.Count = 0 Then

                                     '        Guna2ProgressBar1.Maximum = DllsToMerged.Count + 1
                                     '        Guna2ProgressBar1.Value += 1

                                     '        Try
                                     '            If IO.File.Exists(PackedPath) = True Then
                                     '                IO.File.Delete(PackedPath)
                                     '            End If

                                     '            Dim Merger As ILMerger = New ILMerger()
                                     '            Dim Merge As Boolean = Merger.MergeAssemblies(OriginalPath, DllsToMerged, PackedPath)

                                     '            Core.Helpers.Utils.Sleep(3)

                                     '            If Merge = True Then
                                     '                If IO.File.Exists(PackedPath) = True Then
                                     '                    AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(PackedPath), AsmRef)
                                     '                    Core.Helpers.Utils.Sleep(3)
                                     '                    If AsmDef Is Nothing Then
                                     '                        AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)
                                     '                        Throw New Exception("ILMerge Failed!")
                                     '                    Else
                                     '                        Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.ILMerge", "ILMerger", ""}))
                                     '                    End If
                                     '                Else
                                     '                    Throw New Exception("ILMerge Failed!")
                                     '                End If
                                     '            Else
                                     '                Throw Merger.Errors
                                     '            End If

                                     '            Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                     '        Catch ex As Exception
                                     '            Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                     '            Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.ILMerge", "ILMerger", ex.Message}))
                                     '            Dim Stack As String = ex.StackTrace
                                     '            If String.IsNullOrEmpty(Stack) = False Then
                                     '                Writelog("Source :")
                                     '                Writelog(Stack)
                                     '                Writelog("")
                                     '            End If
                                     '            Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                     '        End Try

                                     '    End If

                                     '    Core.Helpers.Utils.Sleep(2)

                                     '    If Not DllsToEmbedLibz.Count = 0 Then
                                     '        Guna2ProgressBar1.Maximum = DllsToEmbedLibz.Count + 1
                                     '        Guna2ProgressBar1.Value += 1

                                     '        Try

                                     '            If IO.File.Exists(PackedPath) = True Then
                                     '                IO.File.Copy(PackedPath, BackupPath)
                                     '                IO.File.Delete(PackedPath)
                                     '            Else
                                     '                Try
                                     '                    AsmDef.Write(BackupPath)
                                     '                Catch ex As Exception : End Try
                                     '            End If

                                     '            Core.Helpers.Utils.Sleep(3)

                                     '            Dim Merger As LibzWrapper = New LibzWrapper()
                                     '            Dim Embbed As Boolean = Merger.MergeAssemblies(BackupPath, DllsToEmbedLibz)

                                     '            Core.Helpers.Utils.Sleep(3)

                                     '            If Embbed = True Then
                                     '                Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.Libz", "Libz", ""}))
                                     '                If IO.File.Exists(BackupPath) Then
                                     '                    AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)
                                     '                End If
                                     '            Else
                                     '                Throw New Exception("Libz Failed!")
                                     '            End If

                                     '            Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                     '        Catch ex As Exception
                                     '            Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                     '            Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.Libz", "Libz", ex.Message}))
                                     '            Dim Stack As String = ex.StackTrace
                                     '            If String.IsNullOrEmpty(Stack) = False Then
                                     '                Writelog("Source :")
                                     '                Writelog(Stack)
                                     '                Writelog("")
                                     '            End If
                                     '            Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                     '        End Try

                                     '    End If

                                     '    If Not DllsToEmbed.Count = 0 Then
                                     '        Guna2ProgressBar1.Maximum = DllsToEmbed.Count + 1
                                     '        Guna2ProgressBar1.Value += 1

                                     '        Try

                                     '            Try
                                     '                AsmDef.Write(BackupPath)
                                     '            Catch ex As Exception : End Try

                                     '            Core.Helpers.Utils.Sleep(3)

                                     '            Dim thisMod As ModuleDefMD = ModuleDefMD.Load(My.Resources.ModuleLoader)
                                     '            Dim MergerInstance As HydraEngine.References.DllEmbbeder = New HydraEngine.References.DllEmbbeder
                                     '            MergerInstance.InjectDependencyClasses(thisMod, AsmDef)
                                     '            Dim Embbed As Boolean = MergerInstance.ProcessAssembly(AsmDef, DllsToEmbed)

                                     '            Core.Helpers.Utils.Sleep(3)

                                     '            If Embbed = True Then
                                     '                Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.AssemblyEmbed", "Assembly Embed", ""}))
                                     '            Else
                                     '                Throw New Exception("AssemblyEmbed Failed!")
                                     '            End If

                                     '            Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                     '        Catch ex As Exception
                                     '            AsmDef = Nothing
                                     '            Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                     '            Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.AssemblyEmbed", "Assembly Embed", ex.Message}))
                                     '            Dim Stack As String = ex.StackTrace
                                     '            If String.IsNullOrEmpty(Stack) = False Then
                                     '                Writelog("Source :")
                                     '                Writelog(Stack)
                                     '                Writelog("")
                                     '            End If
                                     '            Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                     '        End Try

                                     '    End If

                                     '    If Not DllsToEmbedResources.Count = 0 Then
                                     '        Guna2ProgressBar1.Maximum = DllsToEmbed.Count + 1
                                     '        Guna2ProgressBar1.Value += 1

                                     '        Try

                                     '            Try
                                     '                AsmDef.Write(BackupPath)
                                     '            Catch ex As Exception : End Try

                                     '            Core.Helpers.Utils.Sleep(3)

                                     '            Dim Embeder As Embeder = New Embeder()
                                     '            Dim Merge As Boolean = Embeder.MergeAssemblies(AsmDef, DllsToEmbedResources)

                                     '            Core.Helpers.Utils.Sleep(3)

                                     '            If Merge = True Then
                                     '                Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.AssemblyEmbed", "Assembly Embed", ""}))
                                     '            Else
                                     '                Throw New Exception("AssemblyEmbed Failed!")
                                     '            End If

                                     '            Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                     '        Catch ex As Exception
                                     '            AsmDef = Nothing
                                     '            Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                     '            Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.AssemblyEmbed", "Assembly Embed", ex.Message}))
                                     '            Dim Stack As String = ex.StackTrace
                                     '            If String.IsNullOrEmpty(Stack) = False Then
                                     '                Writelog("Source :")
                                     '                Writelog(Stack)
                                     '                Writelog("")
                                     '            End If
                                     '            Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                     '        End Try

                                     '    End If

                                     '    Core.Helpers.Utils.Sleep(2)

                                     'End If

                                     Me.BeginInvoke(Sub()
                                                        Guna2ProgressBar1.Value = 0
                                                        Guna2ProgressBar1.Maximum = ProtectConfig.Count + 3
                                                        Guna2ProgressBar1.Value += 1
                                                    End Sub)

                                     If AsmDef Is Nothing Then AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)

                                     Core.Helpers.Utils.Sleep(2)

                                     For Each Protection As HydraEngine.Models.Protection In ProtectConfig
                                         Protection.ExitMethod = ExitMethod
                                         Protection.IsNetCoreApp = IsNetCore
                                         Try
                                             AsmDef.Write(BackupPath)
                                         Catch ex As Exception : End Try

                                         Core.Helpers.Utils.Sleep(1)

                                         If TypeOf Protection Is Virtualizer Then
                                             DirectCast(Protection, Virtualizer).ProtectRuntime = ProtectVMRuntime
                                             DirectCast(Protection, Virtualizer).VMStrings = VMStrigns
                                             If TreeViewMethodManager IsNot Nothing Then
                                                 DirectCast(Protection, Virtualizer).SelectedMethods = MethodTreeLoader.ResolveMethodsFromTokens(AsmDef, VM_Selected_Methods)
                                             End If
                                         ElseIf TypeOf Protection Is EXGuard Then
                                             DirectCast(Protection, EXGuard).Protect = ProtectVMRuntime
                                             DirectCast(Protection, EXGuard).ouput = IO.Path.GetDirectoryName(Ouput)
                                             DirectCast(Protection, EXGuard).VMStrings = VMStrigns
                                             If TreeViewMethodManager IsNot Nothing Then
                                                 DirectCast(Protection, EXGuard).SelectedMethods = MethodTreeLoader.ResolveMethodsFromTokens(AsmDef, VM_Selected_Methods)
                                             End If
                                         ElseIf TypeOf Protection Is ExtremeAD Then
                                             DirectCast(Protection, ExtremeAD).Protect = ProtectAntiDump
                                         End If

                                         Dim ProtectResult As Boolean = Await Protection.Execute(AsmDef)

                                         If ProtectResult Then
                                             If Protection.CompatibleWithMap Then AsmMap.Update(AsmDef)
                                             If Protection.ManualReload Then
                                                 Dim TempAsm As Byte() = Protection.TempModule.ToArray()
                                                 AsmDef = HydraEngine.Core.Utils.LoadModule(TempAsm, AsmRef)
                                                 Protection.TempModule.Dispose()
                                             End If
                                             If String.IsNullOrWhiteSpace(Protection.Id) = False Then Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {Protection.Id, Protection.Name, Protection.Description}))
                                             ProtectInt += 1
                                         Else

                                             If IO.File.Exists(BackupPath) Then
                                                 AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)
                                             End If

                                             Core.Helpers.Utils.Sleep(1)

                                             Writelog("")
                                             Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                             Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {Protection.Id, Protection.Name, Protection.Errors?.Message}))
                                             Dim Stack As String = Protection.Errors?.StackTrace
                                             If String.IsNullOrEmpty(Stack) = False Then
                                                 Writelog("Source :")
                                                 Writelog(Stack)
                                                 Writelog("")
                                             End If
                                             Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                             Writelog("")

                                             ProtectErrors += 1
                                         End If

                                         Guna2ProgressBar1.Value += 1
                                     Next

                                     Me.BeginInvoke(Sub()
                                                        Guna2ProgressBar1.Value = 0
                                                        Guna2ProgressBar1.Maximum = ProtectConfig.Count + 3
                                                        Guna2ProgressBar1.Value += 1
                                                    End Sub)

                                     If DllEmbed = True Then

                                         If Not DllsToMerged.Count = 0 Then

                                             Guna2ProgressBar1.Maximum = DllsToMerged.Count + 1
                                             Guna2ProgressBar1.Value += 1

                                             Try
                                                 If IO.File.Exists(PackedPath) = True Then
                                                     IO.File.Delete(PackedPath)
                                                 End If

                                                 Dim Merger As ILRepack = New ILRepack()
                                                 Dim Merge As Boolean = Merger.MergeAssemblies(OriginalPath, DllsToMerged, PackedPath)

                                                 Core.Helpers.Utils.Sleep(3)

                                                 If Merge = True Then
                                                     If IO.File.Exists(PackedPath) = True Then
                                                         AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(PackedPath), AsmRef)
                                                         Core.Helpers.Utils.Sleep(3)
                                                         If AsmDef Is Nothing Then
                                                             AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)
                                                             Throw New Exception("ILMerge Failed!")
                                                         Else
                                                             Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.ILMerge", "ILMerger", ""}))
                                                         End If
                                                     Else
                                                         Throw New Exception("ILMerge Failed!")
                                                     End If
                                                 Else
                                                     Throw Merger.Errors
                                                 End If

                                                 Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                             Catch ex As Exception
                                                 Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                                 Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.ILMerge", "ILMerger", ex.Message}))
                                                 Dim Stack As String = ex.StackTrace
                                                 If String.IsNullOrEmpty(Stack) = False Then
                                                     Writelog("Source :")
                                                     Writelog(Stack)
                                                     Writelog("")
                                                 End If
                                                 Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                             End Try

                                         End If

                                         Core.Helpers.Utils.Sleep(2)

                                         If Not DllsToEmbedLibz.Count = 0 Then
                                             Guna2ProgressBar1.Maximum = DllsToEmbedLibz.Count + 1
                                             Guna2ProgressBar1.Value += 1

                                             Try

                                                 If IO.File.Exists(PackedPath) = True Then
                                                     IO.File.Copy(PackedPath, BackupPath)
                                                     IO.File.Delete(PackedPath)
                                                 Else
                                                     Try
                                                         AsmDef.Write(BackupPath)
                                                     Catch ex As Exception : End Try
                                                 End If

                                                 Core.Helpers.Utils.Sleep(3)

                                                 Dim Merger As LibzWrapper = New LibzWrapper()
                                                 Dim Embbed As Boolean = Merger.MergeAssemblies(BackupPath, DllsToEmbedLibz)

                                                 Core.Helpers.Utils.Sleep(3)

                                                 If Embbed = True Then
                                                     Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.Libz", "Libz", ""}))
                                                     If IO.File.Exists(BackupPath) Then
                                                         AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)
                                                     End If
                                                 Else
                                                     Throw New Exception("Libz Failed!")
                                                 End If

                                                 Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                             Catch ex As Exception
                                                 Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                                 Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.Libz", "Libz", ex.Message}))
                                                 Dim Stack As String = ex.StackTrace
                                                 If String.IsNullOrEmpty(Stack) = False Then
                                                     Writelog("Source :")
                                                     Writelog(Stack)
                                                     Writelog("")
                                                 End If
                                                 Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                             End Try

                                         End If

                                         If Not DllsToEmbed.Count = 0 Then
                                             Guna2ProgressBar1.Maximum = DllsToEmbed.Count + 1
                                             Guna2ProgressBar1.Value += 1

                                             Try

                                                 Try
                                                     AsmDef.Write(BackupPath)
                                                 Catch ex As Exception : End Try

                                                 Core.Helpers.Utils.Sleep(3)

                                                 Dim thisMod As ModuleDefMD = ModuleDefMD.Load(My.Resources.ModuleLoader)
                                                 Dim MergerInstance As HydraEngine.References.DllEmbbeder = New HydraEngine.References.DllEmbbeder
                                                 MergerInstance.InjectDependencyClasses(thisMod, AsmDef)
                                                 Dim Embbed As Boolean = MergerInstance.ProcessAssembly(AsmDef, DllsToEmbed)

                                                 Core.Helpers.Utils.Sleep(3)

                                                 If Embbed = True Then
                                                     Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.AssemblyEmbed", "Assembly Embed", ""}))
                                                 Else
                                                     Throw New Exception("AssemblyEmbed Failed!")
                                                 End If

                                                 Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                             Catch ex As Exception
                                                 AsmDef = Nothing
                                                 Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                                 Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.AssemblyEmbed", "Assembly Embed", ex.Message}))
                                                 Dim Stack As String = ex.StackTrace
                                                 If String.IsNullOrEmpty(Stack) = False Then
                                                     Writelog("Source :")
                                                     Writelog(Stack)
                                                     Writelog("")
                                                 End If
                                                 Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                             End Try

                                         End If

                                         If Not DllsToEmbedResources.Count = 0 Then
                                             Guna2ProgressBar1.Maximum = DllsToEmbed.Count + 1
                                             Guna2ProgressBar1.Value += 1

                                             Try

                                                 Try
                                                     AsmDef.Write(BackupPath)
                                                 Catch ex As Exception : End Try

                                                 Core.Helpers.Utils.Sleep(3)

                                                 Dim Embeder As Embeder = New Embeder()
                                                 Dim Merge As Boolean = Embeder.MergeAssemblies(AsmDef, DllsToEmbedResources)

                                                 Core.Helpers.Utils.Sleep(3)

                                                 If Merge = True Then
                                                     Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"Reference.AssemblyEmbed", "Assembly Embed", ""}))
                                                 Else
                                                     Throw New Exception("AssemblyEmbed Failed!")
                                                 End If

                                                 Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum
                                             Catch ex As Exception
                                                 AsmDef = Nothing
                                                 Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                                 Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"Reference.AssemblyEmbed", "Assembly Embed", ex.Message}))
                                                 Dim Stack As String = ex.StackTrace
                                                 If String.IsNullOrEmpty(Stack) = False Then
                                                     Writelog("Source :")
                                                     Writelog(Stack)
                                                     Writelog("")
                                                 End If
                                                 Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                             End Try

                                         End If

                                         Core.Helpers.Utils.Sleep(2)

                                     End If

                                     Try
                                         If AsmDef Is Nothing Then AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)

                                         If OptimizeCode Then
                                             For Each Type In AsmDef.Types
                                                 For Each MethodsDef In Type.Methods
                                                     If MethodsDef.HasBody Then
                                                         MethodsDef.Body.SimplifyBranches()
                                                         MethodsDef.Body.OptimizeBranches()
                                                     End If
                                                 Next
                                             Next
                                         End If
                                     Catch ex As Exception : End Try

                                     'Dim TestProtection As Misc.TestProtection = New Misc.TestProtection() With {.Ouput = Ouput}
                                     'Dim TestProtectionResult As Boolean = Await TestProtection.Execute(AsmDef)

                                     'If TestProtectionResult Then
                                     '    Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"TestProtectiond", "TestProtection", ""}))
                                     'Else
                                     '    Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"TestProtectiond", "TestProtection", TestProtection.Errors}))
                                     'End If

                                     Try

                                         If Guna2ProgressBar1.Value < Guna2ProgressBar1.Maximum Then Guna2ProgressBar1.Value += 1

                                         If AsmDef Is Nothing Then AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)

                                         Dim writerOptions As New ModuleWriterOptions(AsmDef)
                                         With writerOptions
                                             .Logger = DummyLogger.NoThrowInstance
                                             '.MetadataOptions = New MetadataOptions With {
                                             '    .Flags = MetadataFlags.KeepOldMaxStack
                                             '}
                                             .WritePdb = False
                                         End With

                                         'writerOptions.Cor20HeaderOptions.Flags += dnlib.DotNet.MD.ComImageFlags.ILOnly
                                         AddHandler writerOptions.WriterEvent, AddressOf AssemblyWriterEvent

                                         If DllsToEmbed.Count = 0 Then
                                             If PreserveAll = True Then
                                                 writerOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll
                                             Else
                                                 writerOptions.MetadataOptions.Flags = MetadataFlags.AlwaysCreateGuidHeap Or MetadataFlags.AlwaysCreateStringsHeap Or MetadataFlags.AlwaysCreateUSHeap Or MetadataFlags.AlwaysCreateBlobHeap Or MetadataFlags.PreserveAllMethodRids
                                             End If
                                         End If

                                         If AplyJitHook = True Then
                                             Try
                                                 Try
                                                     AsmDef.Write(BackupPath)

                                                     RemoveHandler writerOptions.WriterEvent, AddressOf AssemblyWriterEvent

                                                     While (IO.File.Exists(BackupPath) = False)
                                                         Core.Helpers.Utils.Sleep(3)
                                                     End While
                                                 Catch ex As Exception : End Try

                                                 If File.Exists(BackupPath) = False Then

                                                     Writelog("[Danger] JIT Protection Corrupted, re-protecting... The protected assembly may not function correctly.")

                                                     For Each Type In AsmDef.Types
                                                         For Each MethodsDef In Type.Methods
                                                             If MethodsDef.HasBody Then
                                                                 MethodsDef.Body.SimplifyBranches()
                                                                 MethodsDef.Body.OptimizeBranches()
                                                             End If
                                                         Next
                                                     Next

                                                     AddHandler writerOptions.WriterEvent, AddressOf AssemblyWriterEvent
                                                     Core.Helpers.Utils.Sleep(2)
                                                     AsmDef.Write(BackupPath)

                                                     RemoveHandler writerOptions.WriterEvent, AddressOf AssemblyWriterEvent
                                                     Core.Helpers.Utils.Sleep(3)

                                                 End If

                                                 Dim AsmEX As ModuleDefMD = ModuleDefMD.Load(BackupPath)

                                                 Dim JIT As JIT.Protection = New JIT.Protection(AsmEX)
                                                 Dim resultjit As Byte() = JIT.Protect()

                                                 Try
                                                     File.WriteAllBytes(TempPreOuputPath, resultjit)

                                                     AsmDef = HydraEngine.Core.Utils.LoadModule(File.ReadAllBytes(JitPath), AsmRef)
                                                 Catch ex As Exception : End Try

                                                 Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {"JIT.Hook", "JIT Protection", "Protects Methodos against assembly decompilation."}))
                                             Catch ex As Exception

                                                 If IO.File.Exists(BackupPath) Then
                                                     IO.File.Copy(BackupPath, TempPreOuputPath)
                                                     AsmDef = HydraEngine.Core.Utils.LoadModule(IO.File.ReadAllBytes(BackupPath), AsmRef)
                                                 End If

                                                 Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                                 Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {"JIT.Hook", "JIT Protection", ex.Message}))
                                                 Dim Stack As String = ex.StackTrace
                                                 If String.IsNullOrEmpty(Stack) = False Then
                                                     Writelog("Source :")
                                                     Writelog(Stack)
                                                     Writelog("")
                                                 End If
                                                 Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                             End Try
                                         Else
                                             'OptimizeModule(AsmDef)

                                             If VMEnabled Or DynMethods Then
                                                 writerOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll
                                                 writerOptions.Cor20HeaderOptions.Flags = dnlib.DotNet.MD.ComImageFlags.ILOnly
                                                 writerOptions.MetadataLogger = DummyLogger.NoThrowInstance
                                             End If

                                             AsmDef.Write(TempPreOuputPath, writerOptions)

                                             If UnmmanagedStr Then

                                                 Dim UnmanagedString = New HydraEngine.Protection.String.UnmanagedString
                                                 Dim UnmmanagedResult = Await UnmanagedString.Execute(TempPreOuputPath)
                                                 If UnmmanagedResult = True And UnmanagedString.TempModule IsNot Nothing Then
                                                     File.WriteAllBytes(TempPreOuputPath, UnmanagedString.TempModule.ToArray)
                                                     UnmanagedString.TempModule.Dispose()
                                                 End If

                                             End If

                                             'RemoveHandler writerOptions.WriterEvent, AddressOf AssemblyWriterEvent
                                         End If

                                         Writelog("")

                                         Writelog(String.Format("{0} Protections have been applied successfully.", {ProtectInt}))
                                         Writelog(String.Format("{0} Protections have failed.", {ProtectErrors}))

                                         Writelog("")
                                         Writelog("")

                                         If Guna2ProgressBar1.Value < Guna2ProgressBar1.Maximum Then Guna2ProgressBar1.Value += 1

                                         If selectedPack IsNot Nothing Then

                                             'If ChangeAppIcon = False Then
                                             ChangeAppIcon = selectedPack.UpdateResurces
                                             'End If

                                             Core.Helpers.Utils.Sleep(3)

                                             Writelog("Packaging .NET assembly , Selected Pack: " & selectedPack.Id)

                                             Dim ProtectResult As Boolean = Await selectedPack.Execute(TempPreOuputPath, Ouput)

                                             If ProtectResult Then
                                                 Writelog(String.Format("[{0}] {1} It was applied satisfactorily. ({2})", {selectedPack.Id, selectedPack.Name, selectedPack.Description}))
                                                 ProtectInt += 1
                                             Else
                                                 Console.WriteLine(String.Format("[{0}] {1} Could not apply, Error: {2}", {selectedPack.Id, selectedPack.Name, selectedPack.Errors?.Message}))
                                                 If AplyJitHook = False Then

                                                     AsmDef.Write(Ouput, writerOptions)

                                                     If UnmmanagedStr Then

                                                         Dim UnmanagedString = New HydraEngine.Protection.String.UnmanagedString
                                                         Dim UnmmanagedResult = Await UnmanagedString.Execute(Ouput)
                                                         If UnmmanagedResult = True And UnmanagedString.TempModule IsNot Nothing Then
                                                             File.WriteAllBytes(Ouput, UnmanagedString.TempModule.ToArray)
                                                             UnmanagedString.TempModule.Dispose()
                                                         End If

                                                     End If
                                                 Else
                                                     IO.File.Copy(TempPreOuputPath, Ouput)
                                                 End If

                                                 Core.Helpers.Utils.Sleep(1)

                                                 Writelog("")
                                                 Writelog("Error ---------------------------------------------->>>>>>>>>>>>>>>>>>")
                                                 Writelog(String.Format("[{0}] {1} Could not apply, Error: {2}", {selectedPack.Id, selectedPack.Name, selectedPack.Errors?.Message}))
                                                 Dim Stack As String = selectedPack.Errors?.StackTrace
                                                 If String.IsNullOrEmpty(Stack) = False Then
                                                     Writelog("Source :")
                                                     Writelog(Stack)
                                                     Writelog("")
                                                 End If
                                                 Writelog("<<<<<<<<<<<<<<---------------------------------------------- End Error")
                                                 Writelog("")

                                                 ProtectErrors += 1
                                             End If
                                         Else

                                             IO.File.Copy(TempPreOuputPath, Ouput)

                                         End If

                                         Guna2ProgressBar1.Value = Guna2ProgressBar1.Maximum

                                         Writelog(String.Format("File Output: {0}", {Ouput}))

                                         Try
                                             If ChangeAppIcon = True And Ouput.ToLower.EndsWith(".exe") Then
                                                 HydraEngine.Core.Utils.ChangeIcon(Ouput, IconPath)
                                             End If

                                             If SingPE Then

                                                 Dim OutName As String = Path.GetFileNameWithoutExtension(Ouput)

                                                 If CloneCert Then
                                                     HydraEngine.Certificate.Sigthief.CloneCert(Cert, Ouput, Ouput.Replace(OutName, OutName + "_Signed"))
                                                 End If

                                                 Writelog(String.Format("File Signed: {0}", {Ouput.Replace(OutName, OutName + "_Signed")}))
                                             End If

                                             If DontRenameSection = False Then
                                                 Dim SectionRenamer As HydraEngine.Protection.Header.SectionObfuscation = New HydraEngine.Protection.Header.SectionObfuscation With {.tag = CustomSectionName, .Mode = BaseMode, .BaseChars = BaseChars}

                                                 If String.IsNullOrEmpty(SectionExclusion) = False Then
                                                     Dim SectionBlackList As String() = SectionExclusion.Split("|")
                                                     If SectionBlackList.Count = 0 Then
                                                         SectionRenamer.blacklist.Add(SectionExclusion)
                                                     Else
                                                         SectionRenamer.blacklist.AddRange(SectionBlackList)
                                                     End If
                                                 End If

                                                 SectionRenamer.Protect(Ouput)
                                             End If

                                             'Packer.BitDotNet.ProtectAssembly(Ouput)
                                         Catch ex As Exception : End Try
                                     Catch ex As Exception
                                         Writelog(String.Format("Error saving: {0}", {ex.Message}))
                                     End Try

                                     Try
                                         If AntiTamp Then
                                             If File.Exists(Ouput) = True Then
                                                 HydraEngine.Runtimes.Anti.AntiTamper.Sha256(Ouput)
                                             End If

                                             Dim OutName As String = Path.GetFileNameWithoutExtension(Ouput)
                                             Dim SignedOut As String = Ouput.Replace(OutName, OutName + "_Signed")
                                             If File.Exists(SignedOut) = True Then
                                                 HydraEngine.Runtimes.Anti.AntiTamper.Sha256(SignedOut)
                                             End If
                                         End If
                                     Catch ex As Exception : End Try

                                     Try
                                         Me.BeginInvoke(Sub()
                                                            BuildButton.Enabled = True
                                                            Guna2ProgressBar1.Visible = False
                                                            Guna2ProgressBar2.Visible = False
                                                            Reset()
                                                            'Try
                                                            '    If IO.Directory.Exists(TempPath) Then
                                                            '        IO.Directory.Delete(TempPath, True)
                                                            '    End If
                                                            'Catch ex As Exception : End Try
                                                        End Sub)
                                     Catch ex As Exception : End Try

                                 End Sub)
        thread.Priority = ThreadPriority.Highest
        thread.Start()

    End Sub

    Public IsConsoleMode As Boolean = False

    Private Sub Writelog(ByVal Msg As String, Optional ByVal ForeC As Color = Nothing)
        Try
            If IsConsoleMode Then
                Console.WriteLine(Msg)
            Else
                Me.BeginInvoke(Sub()
                                   If ForeC = Nothing Then ForeC = Color.White
                                   LogTextBox.ForeColor = ForeC
                                   LogTextBox.Text += Msg & vbNewLine
                               End Sub)
            End If
        Catch ex As Exception : End Try
    End Sub

#End Region

#Region " PostProtect "

    Private Sub AssemblyWriterEvent(ByVal sender As Object, ByVal e As ModuleWriterEventArgs)
        Select Case e.Event
            Case ModuleWriterEvent.MDOnAllTablesSorted
                If InvalidMetaData = True Then
                    Dim writer As ModuleWriterBase = DirectCast(sender, ModuleWriterBase)
                    HydraEngine.Protection.Invalid.InvalidMDWritter.MDOnAllTablesSorted(writer)
                End If
            Case ModuleWriterEvent.MDEndCreateTables
                If AntiTamperEx Then
                    Dim antiTamperNormal As New HydraEngine.Protection.CodeEncryption.AntiTamperNormal()
                    antiTamperNormal.CreateSections(e.Writer)
                End If
                If InvalidMetaData = True Then
                    Dim writer As ModuleWriterBase = DirectCast(sender, ModuleWriterBase)
                    HydraEngine.Protection.Invalid.InvalidMDWritter.MDEndCreateTables(writer, e)
                End If
            Case ModuleWriterEvent.BeginStrongNameSign
                If AntiTamperEx Then
                    Dim antiTamperNormal As New HydraEngine.Protection.CodeEncryption.AntiTamperNormal()
                    antiTamperNormal.EncryptSection(e.Writer)
                End If
            Case ModuleWriterEvent.PESectionsCreated
                If InvalidMetaData = True Then
                    HydraEngine.Protection.Invalid.InvalidMDWritter.PESectionsCreated(e)
                End If
        End Select
    End Sub

    Private Sub resolveModule(ByVal ModuleAsm As ModuleDefMD)
        For Each type As TypeDef In ModuleAsm.GetTypes()

            For Each method As MethodDef In type.Methods
                If method.Body IsNot Nothing Then Resolver(method)
            Next
        Next
    End Sub

    Private Sub Resolver(ByVal method As MethodDef)
        Dim instr As IList(Of Instruction) = method.Body.Instructions

        For i As Integer = 0 To instr.Count - 1
            Dim operand As Object = instr(i).Operand

            If operand IsNot Nothing AndAlso instr(i).OpCode Is OpCodes.[Call] AndAlso operand.ToString().Contains("System.Convert::ToInt32") AndAlso instr(i - 1).OpCode Is OpCodes.Ldc_R4 Then
                Dim num As Integer = Convert.ToInt32(instr(i - 1).Operand)
                instr(i - 1).OpCode = OpCodes.Nop
                instr(i).OpCode = OpCodes.Ldc_I4
                instr(i).Operand = num
            End If
        Next
    End Sub

    Private Sub simplifyModule(ByVal ModuleAsm As ModuleDefMD)
        For Each type As TypeDef In ModuleAsm.GetTypes()
            For Each method As MethodDef In type.Methods
                method.Body?.Instructions.SimplifyMacros(method.Body.Variables, method.Parameters)
            Next
        Next
    End Sub

    Private Sub OptimizeModule(ByVal ModuleAsm As ModuleDefMD)
        For Each type As TypeDef In ModuleAsm.GetTypes()
            For Each method As MethodDef In type.Methods
                method.Body?.Instructions.OptimizeMacros()
            Next
        Next
    End Sub

#End Region

#Region " Signed Code "

    Private Sub Guna2ComboBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles Guna2ComboBox3.SelectedIndexChanged
        If Guna2ComboBox3.SelectedIndex = 0 Then
            LogInLabel5.Text = "Certificate"
        Else
            LogInLabel5.Text = "Clone From"
        End If
    End Sub

    Private Sub Guna2Button3_Click(sender As Object, e As EventArgs) Handles Guna2Button3.Click
        Dim Extension As String = ""

        If Guna2ComboBox3.SelectedIndex = 0 Then
            Extension = "Certificate|*.pfx"
        Else
            Extension = "PE File|*.exe"
        End If

        Dim FilesSelected As List(Of String) = Core.Helpers.Utils.OpenFile(Extension)

        If FilesSelected IsNot Nothing Then
            Dim FileDir As String = FilesSelected.FirstOrDefault
            If IO.File.Exists(FileDir) Then Guna2TextBox3.Text = FileDir
        End If
    End Sub

    Private Sub Guna2CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox4.CheckedChanged
        If TreeViewMethodManager IsNot Nothing Then TreeViewMethodManager.All = Guna2CheckBox4.Checked
    End Sub

    Private Sub Guna2CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox5.CheckedChanged
        If TreeViewMethodManager IsNot Nothing Then TreeViewMethodManager.ExcludeConstructors = Guna2CheckBox5.Checked
    End Sub

    Private Sub Guna2CheckBox6_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox6.CheckedChanged
        If TreeViewMethodManager IsNot Nothing Then TreeViewMethodManager.ExcludeRedMethods = Guna2CheckBox6.Checked
    End Sub

    Private Sub Guna2CheckBox7_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox7.CheckedChanged
        If TreeViewMethodManager IsNot Nothing Then TreeViewMethodManager.ExcludeUnsafeMethods = Guna2CheckBox7.Checked
    End Sub

    Private Sub Guna2Button4_Click(sender As Object, e As EventArgs) Handles Guna2Button4.Click
        VMButton.Checked = True
    End Sub

#End Region

#Region " Select EntryPoint DLL "

    Public Property EntryPointToken As UInteger = 0
    Public Property EntryPointMethod As MethodDef = Nothing

    Private Sub SetupContextMenu()
        AddHandler setEntryPoint.Click, AddressOf SetEntryPointMenuItem_Click
        AddHandler LogInContextMenu2.Opening, AddressOf TreeContextMenu_Opening
    End Sub

    Private Sub TreeContextMenu_Opening(sender As Object, e As CancelEventArgs)

        Dim setEntryPointItem = LogInContextMenu2

        Dim hitTest = TreeView1.HitTest(TreeView1.PointToClient(Cursor.Position))
        Dim selectedNode = hitTest.Node

        If selectedNode?.Tag IsNot Nothing AndAlso IsMethodNode(selectedNode) Then
            Dim method = GetMethodFromNode(selectedNode)

            If method IsNot Nothing Then
                Dim isUserStaticMethod As Boolean = method.IsStatic
                setEntryPointItem.Visible = isUserStaticMethod

                If isUserStaticMethod Then
                    Dim isCurrentEntryPoint As Boolean = False

                    '((EntryPointToken IsNot 0) AndAlso
                    '                     EntryPointMethod.MDToken.Raw = method.MDToken.Raw)

                    If EntryPointMethod IsNot Nothing AndAlso EntryPointMethod.MDToken.Raw = method.MDToken.Raw Then isCurrentEntryPoint = True

                    setEntryPointItem.Text = If(isCurrentEntryPoint,
                    "✓ Current EntryPoint",
                    "Set As EntryPoint")

                    'setEntryPointItem.Enabled = Not isCurrentEntryPoint
                End If
            Else
                setEntryPointItem.Visible = False
            End If
        Else
            setEntryPointItem.Visible = False
        End If

    End Sub

    Private Sub SetEntryPointMenuItem_Click(sender As Object, e As EventArgs)
        Dim selectedNode As TreeNode = Nothing

        If TreeView1.SelectedNode IsNot Nothing Then
            If IsMethodNode(TreeView1.SelectedNode) Then
                Dim selectedMethod = GetMethodFromNode(TreeView1.SelectedNode)
                If selectedMethod IsNot Nothing AndAlso selectedMethod.IsStatic Then
                    selectedNode = TreeView1.SelectedNode
                End If
            End If
        End If

        If selectedNode Is Nothing Then
            Dim hitTest = TreeView1.HitTest(TreeView1.PointToClient(Cursor.Position))
            selectedNode = hitTest.Node
        End If

        If selectedNode?.Tag IsNot Nothing AndAlso IsMethodNode(selectedNode) Then
            Dim method = GetMethodFromNode(selectedNode)
            If method IsNot Nothing AndAlso method.IsStatic Then
                EntryPointToken = method.MDToken.Raw
                EntryPointMethod = method
                Guna2TextBox4.Text = selectedNode.Text
            End If
        End If
    End Sub

    Private Function IsMethodNode(node As TreeNode) As Boolean
        If node.Tag Is Nothing Then Return False

        Dim nodeInfoType = node.Tag.GetType()
        Dim typeProperty = nodeInfoType.GetProperty("Type")

        If typeProperty IsNot Nothing Then
            Dim nodeType = typeProperty.GetValue(node.Tag)
            Return nodeType.ToString() = "Method"
        End If

        Return False
    End Function

    Private Function GetMethodFromNode(node As TreeNode) As MethodDef
        If node.Tag Is Nothing Then Return Nothing

        Dim nodeInfoType = node.Tag.GetType()
        Dim methodProperty = nodeInfoType.GetProperty("Method")

        Return TryCast(methodProperty?.GetValue(node.Tag), MethodDef)
    End Function

#End Region

#Region " Presets Manager "

    ''' <summary>
    ''' Carga la lista de presets disponibles en el ComboBox
    ''' </summary>
    Private Sub LoadPresetsComboBox()
        Try
            If Core.Instances.MainInstance Is Nothing Then Exit Sub
            ' Buscar si existe un ComboBox para presets, si no, crearlo dinámicamente
            Dim presetsCombo As LogInComboBox = Nothing

            ' Buscar en los controles existentes
            For Each ctrl As Control In Core.Instances.MainInstance.Guna2Panel1.Controls
                If TypeOf ctrl Is LogInComboBox AndAlso ctrl.Name = "PresetsComboBox" Then
                    presetsCombo = DirectCast(ctrl, LogInComboBox)
                    Exit For
                End If
            Next

            ' Si no existe, no cargar presets todavía
            If presetsCombo IsNot Nothing Then
                presetsCombo.Visible = True
                presetsCombo.Invalidate()
                presetsCombo.Items.Clear()
                presetsCombo.Items.Add("-- Select Preset --")

                Dim presets As List(Of String) = PresetManager.GetAvailablePresets()
                For Each preset As String In presets
                    presetsCombo.Items.Add(preset)
                Next

                presetsCombo.SelectedIndex = 0
            End If
        Catch ex As Exception
            Console.WriteLine("Error loading presets: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Guarda la configuración actual como un nuevo preset
    ''' </summary>
    Public Sub SaveCurrentConfigurationAsPreset()
        Try
            ' Crear formulario de diálogo para obtener nombre y descripción
            Dim inputForm As New Form()
            inputForm.Text = "Save Preset"
            inputForm.Size = New Size(400, 220)
            inputForm.StartPosition = FormStartPosition.CenterParent
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog
            inputForm.MaximizeBox = False
            inputForm.MinimizeBox = False

            Dim lblName As New Label()
            lblName.Text = "Preset Name:"
            lblName.Location = New Point(10, 15)
            lblName.Size = New Size(120, 20)

            Dim txtName As New TextBox()
            txtName.Location = New Point(140, 12)
            txtName.Size = New Size(230, 25)
            txtName.Text = "Preset_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")

            Dim lblDescription As New Label()
            lblDescription.Text = "Description:"
            lblDescription.Location = New Point(10, 50)
            lblDescription.Size = New Size(120, 20)

            Dim txtDescription As New TextBox()
            txtDescription.Location = New Point(140, 47)
            txtDescription.Size = New Size(230, 60)
            txtDescription.Multiline = True
            txtDescription.Text = "Preset created on " & DateTime.Now.ToString("dd/MM/yyyy HH:mm")

            Dim btnSave As New Button()
            btnSave.Text = "Save"
            btnSave.Location = New Point(215, 125)
            btnSave.Size = New Size(75, 30)
            btnSave.DialogResult = DialogResult.OK

            Dim btnCancel As New Button()
            btnCancel.Text = "Cancel"
            btnCancel.Location = New Point(295, 125)
            btnCancel.Size = New Size(75, 30)
            btnCancel.DialogResult = DialogResult.Cancel

            inputForm.Controls.AddRange({lblName, txtName, lblDescription, txtDescription, btnSave, btnCancel})

            If inputForm.ShowDialog() = DialogResult.OK AndAlso Not String.IsNullOrWhiteSpace(txtName.Text) Then
                Dim preset As ProtectionPreset = PresetManager.CreatePresetFromForm(Me)
                preset.Name = txtName.Text.Trim()
                preset.Description = txtDescription.Text.Trim()
                preset.Created = DateTime.Now

                If PresetManager.SavePreset(preset, txtName.Text.Trim()) Then
                    MessageBox.Show("Preset saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadPresetsComboBox()
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error saving preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Carga un preset seleccionado
    ''' </summary>
    Public Sub LoadSelectedPreset(presetName As String)
        Try
            If String.IsNullOrWhiteSpace(presetName) OrElse presetName = "-- Select Preset --" Then
                Return
            End If

            Dim preset As ProtectionPreset = PresetManager.LoadPreset(presetName)
            If preset IsNot Nothing Then
                PresetManager.ApplyPresetToForm(preset, Me)
                'MessageBox.Show($"Preset '{preset.Name}' aplicado exitosamente.", "Preset Aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Elimina un preset seleccionado
    ''' </summary>
    Public Sub DeleteSelectedPreset(presetName As String)
        Try
            If String.IsNullOrWhiteSpace(presetName) OrElse presetName = "-- Select Preset --" Then
                MessageBox.Show("Select a valid preset to delete.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim result As DialogResult = MessageBox.Show($"Are you sure you want to delete the preset '{presetName}'?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                If PresetManager.DeletePreset(presetName) Then
                    MessageBox.Show("Preset deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadPresetsComboBox()
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error deleting preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Exporta un preset a un archivo
    ''' </summary>
    Public Sub ExportPreset(presetName As String)
        Try
            If String.IsNullOrWhiteSpace(presetName) OrElse presetName = "-- Select Preset --" Then
                MessageBox.Show("Select a valid preset to export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim preset As ProtectionPreset = PresetManager.LoadPreset(presetName)
            If preset IsNot Nothing Then
                Dim saveDialog As New SaveFileDialog()
                saveDialog.Filter = "JSON Files|*.json"
                saveDialog.Title = "Export Preset"
                saveDialog.FileName = presetName & ".json"

                If saveDialog.ShowDialog() = DialogResult.OK Then
                    If PresetManager.ExportPreset(preset, saveDialog.FileName) Then
                        MessageBox.Show("Preset exported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End If
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error exporting preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Importa un preset desde un archivo
    ''' </summary>
    Public Sub ImportPreset()
        Try
            Dim openDialog As New OpenFileDialog()
            openDialog.Filter = "JSON Files|*.json"
            openDialog.Title = "Import Preset"

            If openDialog.ShowDialog() = DialogResult.OK Then
                Dim preset As ProtectionPreset = PresetManager.ImportPreset(openDialog.FileName)
                If preset IsNot Nothing Then
                    ' Pedir nuevo nombre si es necesario
                    Dim inputForm As New Form()
                    inputForm.Text = "Import Preset"
                    inputForm.Size = New Size(350, 180)
                    inputForm.StartPosition = FormStartPosition.CenterParent
                    inputForm.FormBorderStyle = FormBorderStyle.FixedDialog
                    inputForm.MaximizeBox = False
                    inputForm.MinimizeBox = False

                    Dim lblName As New Label()
                    lblName.Text = "Preset Name:"
                    lblName.Location = New Point(10, 15)
                    lblName.Size = New Size(120, 20)

                    Dim txtName As New TextBox()
                    txtName.Location = New Point(10, 40)
                    txtName.Size = New Size(310, 25)
                    txtName.Text = preset.Name

                    Dim lblInfo As New Label()
                    lblInfo.Text = $"Description: {preset.Description}" & vbCrLf & $"Created: {preset.Created:dd/MM/yyyy HH:mm}"
                    lblInfo.Location = New Point(10, 75)
                    lblInfo.Size = New Size(310, 40)
                    lblInfo.AutoSize = False

                    Dim btnImport As New Button()
                    btnImport.Text = "Import"
                    btnImport.Location = New Point(165, 125)
                    btnImport.Size = New Size(75, 30)
                    btnImport.DialogResult = DialogResult.OK

                    Dim btnCancel As New Button()
                    btnCancel.Text = "Cancel"
                    btnCancel.Location = New Point(245, 125)
                    btnCancel.Size = New Size(75, 30)
                    btnCancel.DialogResult = DialogResult.Cancel

                    inputForm.Controls.AddRange({lblName, txtName, lblInfo, btnImport, btnCancel})

                    If inputForm.ShowDialog() = DialogResult.OK AndAlso Not String.IsNullOrWhiteSpace(txtName.Text) Then
                        preset.Name = txtName.Text.Trim()

                        If PresetManager.SavePreset(preset, txtName.Text.Trim()) Then
                            MessageBox.Show("Preset imported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            LoadPresetsComboBox()
                        End If
                    End If
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error importing preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Abre el formulario de gestión de presets
    ''' </summary>
    Public Sub OpenPresetManager()
        Try
            Dim presetManager As New PresetManagerForm(Me)
            presetManager.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show($"Error opening preset manager: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Muestra información detallada de un preset
    ''' </summary>
    Public Sub ShowPresetInfo(presetName As String)
        Try
            If String.IsNullOrWhiteSpace(presetName) OrElse presetName = "-- Select Preset --" Then
                MessageBox.Show("Select a valid preset to view information.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim preset As ProtectionPreset = PresetManager.LoadPreset(presetName)
            If preset IsNot Nothing Then
                Dim info As String = $"Name: {preset.Name}" & vbCrLf &
                                   $"Description: {preset.Description}" & vbCrLf &
                                   $"Version: {preset.Version}" & vbCrLf &
                                   $"Created: {preset.Created:dd/MM/yyyy HH:mm:ss}" & vbCrLf & vbCrLf &
                                   "Enabled Protections:" & vbCrLf

                ' Contar protecciones habilitadas
                Dim enabledCount As Integer = 0

                If preset.Renamer.Enabled Then
                    info &= "• Renamer" & vbCrLf
                    enabledCount += 1
                End If

                If preset.Protections.StringEncryption Then
                    info &= "• String Encryption" & vbCrLf
                    enabledCount += 1
                End If

                If preset.Protections.ControlFlow Then
                    info &= "• Control Flow" & vbCrLf
                    enabledCount += 1
                End If

                If preset.VM.Enabled Then
                    info &= "• Virtualization" & vbCrLf
                    enabledCount += 1
                End If

                If preset.Packer.UsePacker Then
                    info &= "• Packer" & vbCrLf
                    enabledCount += 1
                End If

                If enabledCount = 0 Then
                    info &= "• No protections enabled" & vbCrLf
                End If

                MessageBox.Show(info, $"Preset Information - {preset.Name}", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error showing preset information: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

#End Region

End Class