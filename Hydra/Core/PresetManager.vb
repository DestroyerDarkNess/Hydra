Imports System.IO
Imports Newtonsoft.Json

Public Class PresetManager
    Private Shared ReadOnly PresetsDirectory As String = Path.Combine(Application.StartupPath, "HydraPresets")

    Shared Sub New()
        ' Crear directorio de presets si no existe
        If Not Directory.Exists(PresetsDirectory) Then
            Directory.CreateDirectory(PresetsDirectory)
            CreateDefaultPresets()
        End If
    End Sub

    ''' <summary>
    ''' Guarda un preset en un archivo JSON
    ''' </summary>
    Public Shared Function SavePreset(preset As ProtectionPreset, filename As String) As Boolean
        Try
            If Not filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then
                filename &= ".json"
            End If

            Dim filePath As String = Path.Combine(PresetsDirectory, filename)
            Dim json As String = JsonConvert.SerializeObject(preset, Formatting.Indented)

            File.WriteAllText(filePath, json)
            Return True
        Catch ex As Exception
            MessageBox.Show($"Error al guardar preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Carga un preset desde un archivo JSON
    ''' </summary>
    Public Shared Function LoadPreset(filename As String) As ProtectionPreset
        Try
            If Not filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then
                filename &= ".json"
            End If

            Dim filePath As String = Path.Combine(PresetsDirectory, filename)

            If Not File.Exists(filePath) Then
                Throw New FileNotFoundException($"El archivo de preset no existe: {filename}")
            End If

            Dim json As String = File.ReadAllText(filePath)
            Return JsonConvert.DeserializeObject(Of ProtectionPreset)(json)
        Catch ex As Exception
            MessageBox.Show($"Error al cargar preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Obtiene lista de presets disponibles
    ''' </summary>
    Public Shared Function GetAvailablePresets() As List(Of String)
        Try
            Dim presets As New List(Of String)

            If Directory.Exists(PresetsDirectory) Then
                Dim files() As String = Directory.GetFiles(PresetsDirectory, "*.json")
                For Each file As String In files
                    presets.Add(Path.GetFileNameWithoutExtension(file))
                Next
            End If

            Return presets
        Catch ex As Exception
            Return New List(Of String)
        End Try
    End Function

    ''' <summary>
    ''' Elimina un preset
    ''' </summary>
    Public Shared Function DeletePreset(filename As String) As Boolean
        Try
            If Not filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase) Then
                filename &= ".json"
            End If

            Dim filePath As String = Path.Combine(PresetsDirectory, filename)

            If File.Exists(filePath) Then
                File.Delete(filePath)
                Return True
            End If

            Return False
        Catch ex As Exception
            MessageBox.Show($"Error al eliminar preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Crea un preset desde la configuración actual del formulario
    ''' </summary>
    Public Shared Function CreatePresetFromForm(form As ProjectDesigner) As ProtectionPreset
        Dim preset As New ProtectionPreset()

        ' Configuraciones del Renamer
        With preset.Renamer
            .Enabled = form.Renamer.Checked
            .Engine = form.RenamerEngineCombo.SelectedIndex
            .Mode = form.Guna2ComboBox2.SelectedIndex
            .BaseChars = form.BaseChars
            .Length = form.Guna2TrackBar1.Value
            .Tag = form.Guna2TextBox9.Text
            .Resources = form.ResourcesCheck.Checked
            .NamespaceEx = form.NamespaceCheck.Checked
            .NamespaceEmpty = form.NamespaceCheck_Empty.Checked
            .ClassName = form.ClassName.Checked
            .Methods = form.MethodsCheck.Checked
            .Properties = form.PropertiesCheck.Checked
            .Fields = form.FieldsCheck.Checked
            .Events = form.EventsCheck.Checked
            .ModuleRenaming = form.ModuleCheck.Checked
            .ModuleInvisible = form.ModuleCheck_Invisible.Checked
            .UnsafeMode = form.UnsafeMode.Checked
            .ResourceEncryption = form.ResourceEncryptionCheck.Checked
            .ResourceCompressEncrypt = form.ResourceCompressAndEncryptCheck.Checked
        End With

        ' Configuraciones de Protección
        With preset.Protections
            .ImportProtection = form.ImportProtection.Checked
            .SUFConfusion = form.SUFconfusionCheck.Checked
            .ProxyClasses = form.ProxyClassesObject.Checked
            .ProxyClassesUnsafe = form.ProxyClassesObjectUnsafeCheck.Checked
            .ProxyMethods = form.ProxyMethods.Checked
            .MoveVariables = form.MoveVariables.Checked
            .L2F = form.L2F.Checked
            .EntryPointMover = form.EntryPointMover.Checked
            .Method2Delegate = form.Method2DelegateCheck.Checked
            .Method2DynamicEntry = form.Method2DynamicEntryPoint.Checked
            .ReduceMetadata = form.ReduceMetadata.Checked
            .NopAttack = form.NopAttack.Checked
            .StringEncryption = form.StringEncryption.Checked
            .ProxyStrings = form.ProxyStrings.Checked
            .DynamicStrings = form.Dynamic_STRINGS.Checked
            .ReplaceObfuscation = form.ReplaceObfuscationCheck.Checked
            .IntConfusion = form.IntConfusion.Checked
            .StringsHider = form.StringsHider.Checked
            .Calli = form.Calli.Checked
            .ProxyInt = form.ProxyInt.Checked
            .DynamicInt = form.Dynamic_INT.Checked
            .MutationV2 = form.MutationV2Check.Checked
            .MutationV2Unsafe = form.MutationV2Check_Unsafe.Checked
            .EncodeIntegers = form.EncodeIntergers.Checked
            .FakeObfuscation = form.FakeObfuscation.Checked
            .AddJunkCode = form.AddJunkCode.Checked
            .JunkCodeNumber = form.Guna2NumericUpDown1.Value
            .ControlFlow = form.ControlFlow.Checked
            .ControlFlowStrong = form.ControlFlowStrongModeCheck.Checked
            .SugarControlFlow = form.SugarControlFlowCheck.Checked
            .EXGuardControlFlow = form.EXGuardControlFlowCheck.Checked
            .KroksControlFlow = form.KroksCheck.Checked
            .ProxyReferences = form.ProxyReferences.Checked
            .ProxyReferencesUnsafe = form.ProxyReferencesUnsafeCheck.Checked
            .Mutator = form.Mutator.Checked
            .MutatorUnsafe = form.MutationCheck_Unsafe.Checked
            .AntiDecompiler = form.AntiDecompilerCheck.Checked
            .InvalidOpcodes = form.InvalidOpcodes.Checked
            .InvalidMD = form.InvalidMD.Checked
            .StackUnfConfusion = form.StackUnfConfusion.Checked
            .HideMethods = form.HideMethods.Checked
            .DynamicMethods = form.DynamicMethodsCheck.Checked
            .CctorHider = form.CctorHider.Checked
            .DynamicCctor = form.DynamicCctorCheck.Checked
            .CctorL2F = form.CctorL2FCheck.Checked
            .MethodError = form.MethodError.Checked
            .CodeOptimizer = form.CodeOptimizerCheck.Checked
        End With

        ' Configuraciones del Packer
        With preset.Packer
            .UsePacker = form.UsePacker.Checked
            .SelectedPacker = form.PackerSelect.SelectedIndex
        End With

        ' Configuraciones de Salida
        With preset.Output
            .PreserveAll = form.PreserveAllCheck.Checked
            .InvalidMetadata = form.InvalidMetaDataCheck.Checked
            .UnmanagedString = form.UnmanagedStringCheck.Checked
            .ExportEntryPoint = form.ExportEntryPoint.Checked
            .PESectionPreserve = form.PESectionPreserve.Checked
            .PESectionCustom = form.PESectionCustom.Checked
            .PESectionCustomText = form.PESectionCustomText.Text
            .PESectionExclusion = form.PESectionExclusion.Text
            .JITHook = form.JITHookCheck.Checked
            .SignPE = form.Guna2CheckBox2.Checked
            .CertMode = form.Guna2ComboBox3.SelectedIndex
            .CertPath = form.Guna2TextBox3.Text
            .AppClosesMethod = form.AppClosesMethod.SelectedIndex
        End With

        ' Configuraciones de VM
        With preset.VM
            .Enabled = form.ILVMCheck.Checked
            .SelectedVM = form.VMComboSelect.SelectedIndex
            .ProtectRuntime = form.ProtectVMCheck.Checked
            .VirtualizeStrings = form.VirtualizeStringsVM.Checked
            .SelectAll = form.Guna2CheckBox4.Checked
            .ExcludeConstructors = form.Guna2CheckBox5.Checked
            .ExcludeRedMethods = form.Guna2CheckBox6.Checked
            .ExcludeUnsafeMethods = form.Guna2CheckBox7.Checked
        End With

        ' Configuraciones Anti
        With preset.Anti
            .AntiProxy = form.AntiProxyCheck.Checked
            .ExceptionManager = form.ExeptionManager.Checked
            .ElevationEscale = form.ElevationEscale.Checked
            .AntiDebug = form.AntiDebug.Checked
            .JitFucker = form.JitFuckerCheck.Checked
            .AntiDump = form.AntiDump.Checked
            .ProtectAntiDump = form.ProtectAntiDumpCheck.Checked
            .ExtremeAD = form.ExtremeADCheck.Checked
            .AntiHTTPDebug = form.AntiHTTPDebugCheck.Checked
            .AntiInvoke = form.AntiInvoke.Checked
            .AntiTamper = form.AntiTamper.Checked
            .Antide4dot = form.Antide4dot.Checked
            .AntiMalicious = form.AntiMalicious.Checked
            .AntiILDasm = form.AntiILDasm.Checked
            .AntiAttach = form.AntiAttachCheck.Checked
            .ThreadHider = form.ThreadHiderCheck.Checked
            .BypassAmsi = form.BypassAmsiCheck.Checked
        End With

        ' Configuraciones de DLL
        With preset.DLL
            .DLLEmbeder = form.DLLEmbeder.Checked
            .SelectAllDlls = form.Guna2CheckBox3.Checked
            .MergeMode = form.Guna2ComboBox1.SelectedIndex
        End With

        ' Configuraciones de EntryPoint
        With preset.EntryPoint
            .HasCustomEntryPoint = (form.EntryPointToken > 0 AndAlso form.EntryPointMethod IsNot Nothing)
            .EntryPointToken = form.EntryPointToken
            .EntryPointMethodName = If(form.EntryPointMethod IsNot Nothing, form.EntryPointMethod.Name.String, "")
            .EntryPointTypeName = If(form.EntryPointMethod IsNot Nothing, form.EntryPointMethod.DeclaringType.FullName, "")
            .AssemblyName = If(form.Assembly IsNot Nothing, form.Assembly.Name.String, "")
            .AssemblyFullName = If(form.Assembly IsNot Nothing, form.Assembly.FullName, "")
        End With

        Return preset
    End Function

    ''' <summary>
    ''' Aplica un preset al formulario
    ''' </summary>
    Public Shared Sub ApplyPresetToForm(preset As ProtectionPreset, form As ProjectDesigner)
        Try
            ' Configuraciones del Renamer
            With preset.Renamer
                form.Renamer.Checked = .Enabled
                form.RenamerEngineCombo.SelectedIndex = .Engine
                form.Guna2ComboBox2.SelectedIndex = .Mode
                form.BaseChars = .BaseChars
                form.Guna2TrackBar1.Value = .Length
                form.Guna2TextBox9.Text = .Tag
                form.ResourcesCheck.Checked = .Resources
                form.NamespaceCheck.Checked = .NamespaceEx
                form.NamespaceCheck_Empty.Checked = .NamespaceEmpty
                form.ClassName.Checked = .ClassName
                form.MethodsCheck.Checked = .Methods
                form.PropertiesCheck.Checked = .Properties
                form.FieldsCheck.Checked = .Fields
                form.EventsCheck.Checked = .Events
                form.ModuleCheck.Checked = .ModuleRenaming
                form.ModuleCheck_Invisible.Checked = .ModuleInvisible
                form.UnsafeMode.Checked = .UnsafeMode
                form.ResourceEncryptionCheck.Checked = .ResourceEncryption
                form.ResourceCompressAndEncryptCheck.Checked = .ResourceCompressEncrypt
            End With

            ' Configuraciones de Protección
            With preset.Protections
                form.ImportProtection.Checked = .ImportProtection
                form.SUFconfusionCheck.Checked = .SUFConfusion
                form.ProxyClassesObject.Checked = .ProxyClasses
                form.ProxyClassesObjectUnsafeCheck.Checked = .ProxyClassesUnsafe
                form.ProxyMethods.Checked = .ProxyMethods
                form.MoveVariables.Checked = .MoveVariables
                form.L2F.Checked = .L2F
                form.EntryPointMover.Checked = .EntryPointMover
                form.Method2DelegateCheck.Checked = .Method2Delegate
                form.Method2DynamicEntryPoint.Checked = .Method2DynamicEntry
                form.ReduceMetadata.Checked = .ReduceMetadata
                form.NopAttack.Checked = .NopAttack
                form.StringEncryption.Checked = .StringEncryption
                form.ProxyStrings.Checked = .ProxyStrings
                form.Dynamic_STRINGS.Checked = .DynamicStrings
                form.ReplaceObfuscationCheck.Checked = .ReplaceObfuscation
                form.IntConfusion.Checked = .IntConfusion
                form.StringsHider.Checked = .StringsHider
                form.Calli.Checked = .Calli
                form.ProxyInt.Checked = .ProxyInt
                form.Dynamic_INT.Checked = .DynamicInt
                form.MutationV2Check.Checked = .MutationV2
                form.MutationV2Check_Unsafe.Checked = .MutationV2Unsafe
                form.EncodeIntergers.Checked = .EncodeIntegers
                form.FakeObfuscation.Checked = .FakeObfuscation
                form.AddJunkCode.Checked = .AddJunkCode
                form.Guna2NumericUpDown1.Value = .JunkCodeNumber
                form.ControlFlow.Checked = .ControlFlow
                form.ControlFlowStrongModeCheck.Checked = .ControlFlowStrong
                form.SugarControlFlowCheck.Checked = .SugarControlFlow
                form.EXGuardControlFlowCheck.Checked = .EXGuardControlFlow
                form.KroksCheck.Checked = .KroksControlFlow
                form.ProxyReferences.Checked = .ProxyReferences
                form.ProxyReferencesUnsafeCheck.Checked = .ProxyReferencesUnsafe
                form.Mutator.Checked = .Mutator
                form.MutationCheck_Unsafe.Checked = .MutatorUnsafe
                form.AntiDecompilerCheck.Checked = .AntiDecompiler
                form.InvalidOpcodes.Checked = .InvalidOpcodes
                form.InvalidMD.Checked = .InvalidMD
                form.StackUnfConfusion.Checked = .StackUnfConfusion
                form.HideMethods.Checked = .HideMethods
                form.DynamicMethodsCheck.Checked = .DynamicMethods
                form.CctorHider.Checked = .CctorHider
                form.DynamicCctorCheck.Checked = .DynamicCctor
                form.CctorL2FCheck.Checked = .CctorL2F
                form.MethodError.Checked = .MethodError
                form.CodeOptimizerCheck.Checked = .CodeOptimizer
            End With

            ' Configuraciones del Packer
            With preset.Packer
                form.UsePacker.Checked = .UsePacker
                If form.PackerSelect.Items.Count > .SelectedPacker Then
                    form.PackerSelect.SelectedIndex = .SelectedPacker
                End If
            End With

            ' Configuraciones de Salida
            With preset.Output
                form.PreserveAllCheck.Checked = .PreserveAll
                form.InvalidMetaDataCheck.Checked = .InvalidMetadata
                form.UnmanagedStringCheck.Checked = .UnmanagedString
                form.ExportEntryPoint.Checked = .ExportEntryPoint
                form.PESectionPreserve.Checked = .PESectionPreserve
                form.PESectionCustom.Checked = .PESectionCustom
                form.PESectionCustomText.Text = .PESectionCustomText
                form.PESectionExclusion.Text = .PESectionExclusion
                form.JITHookCheck.Checked = .JITHook
                form.Guna2CheckBox2.Checked = .SignPE
                form.Guna2ComboBox3.SelectedIndex = .CertMode
                form.Guna2TextBox3.Text = .CertPath
                form.AppClosesMethod.SelectedIndex = .AppClosesMethod
            End With

            ' Configuraciones de VM
            With preset.VM
                form.ILVMCheck.Checked = .Enabled
                form.VMComboSelect.SelectedIndex = .SelectedVM
                form.ProtectVMCheck.Checked = .ProtectRuntime
                form.VirtualizeStringsVM.Checked = .VirtualizeStrings
                form.Guna2CheckBox4.Checked = .SelectAll
                form.Guna2CheckBox5.Checked = .ExcludeConstructors
                form.Guna2CheckBox6.Checked = .ExcludeRedMethods
                form.Guna2CheckBox7.Checked = .ExcludeUnsafeMethods
            End With

            ' Configuraciones Anti
            With preset.Anti
                form.AntiProxyCheck.Checked = .AntiProxy
                form.ExeptionManager.Checked = .ExceptionManager
                form.ElevationEscale.Checked = .ElevationEscale
                form.AntiDebug.Checked = .AntiDebug
                form.JitFuckerCheck.Checked = .JitFucker
                form.AntiDump.Checked = .AntiDump
                form.ProtectAntiDumpCheck.Checked = .ProtectAntiDump
                form.ExtremeADCheck.Checked = .ExtremeAD
                form.AntiHTTPDebugCheck.Checked = .AntiHTTPDebug
                form.AntiInvoke.Checked = .AntiInvoke
                form.AntiTamper.Checked = .AntiTamper
                form.Antide4dot.Checked = .Antide4dot
                form.AntiMalicious.Checked = .AntiMalicious
                form.AntiILDasm.Checked = .AntiILDasm
                form.AntiAttachCheck.Checked = .AntiAttach
                form.ThreadHiderCheck.Checked = .ThreadHider
                form.BypassAmsiCheck.Checked = .BypassAmsi
            End With

            ' Configuraciones de DLL
            With preset.DLL
                form.DLLEmbeder.Checked = .DLLEmbeder
                form.Guna2CheckBox3.Checked = .SelectAllDlls
                form.Guna2ComboBox1.SelectedIndex = .MergeMode
            End With

            ' Configuraciones de EntryPoint
            ApplyEntryPointSettings(preset.EntryPoint, form)

            ' Actualizar UI
            form.RaiseUI()
        Catch ex As Exception
            MessageBox.Show($"Error al aplicar preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Aplica configuraciones del entrypoint al formulario si es compatible
    ''' </summary>
    Private Shared Sub ApplyEntryPointSettings(entryPointSettings As EntryPointSettings, form As ProjectDesigner)
        Try
            ' Verificar si hay un entrypoint personalizado guardado
            If Not entryPointSettings.HasCustomEntryPoint Then
                Return
            End If

            ' Verificar compatibilidad del ensamblado
            If form.Assembly Is Nothing Then
                Return
            End If

            ' Verificar si es el mismo ensamblado (por nombre)
            Dim currentAssemblyName As String = form.Assembly.Name
            If String.IsNullOrEmpty(currentAssemblyName) OrElse
               Not String.Equals(currentAssemblyName, entryPointSettings.AssemblyName, StringComparison.OrdinalIgnoreCase) Then

                ' Mostrar mensaje informativo si el ensamblado no coincide
                If Not String.IsNullOrEmpty(entryPointSettings.EntryPointMethodName) Then
                    Dim msg As String = $"El preset contiene un entrypoint personalizado '{entryPointSettings.EntryPointMethodName}' " &
                                      $"del ensamblado '{entryPointSettings.AssemblyName}', pero el ensamblado actual es '{currentAssemblyName}'. " &
                                      $"El entrypoint no se aplicará automáticamente."
                    MessageBox.Show(msg, "EntryPoint no compatible", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
                Return
            End If

            ' Intentar restaurar el entrypoint por token
            If entryPointSettings.EntryPointToken > 0 Then
                Try
                    Dim method As dnlib.DotNet.MethodDef = form.Assembly.ResolveToken(entryPointSettings.EntryPointToken)
                    If method IsNot Nothing Then
                        ' Verificar que el método coincida con el guardado
                        If String.Equals(method.Name.String, entryPointSettings.EntryPointMethodName, StringComparison.Ordinal) AndAlso
                           String.Equals(method.DeclaringType.FullName, entryPointSettings.EntryPointTypeName, StringComparison.Ordinal) Then

                            ' Restaurar entrypoint
                            form.EntryPointToken = entryPointSettings.EntryPointToken
                            form.EntryPointMethod = method
                            form.Guna2TextBox4.Text = $"{entryPointSettings.EntryPointTypeName}.{entryPointSettings.EntryPointMethodName}"

                            ' Mostrar mensaje de éxito
                            'MessageBox.Show($"EntryPoint restaurado: {entryPointSettings.EntryPointMethodName}",
                            '              "EntryPoint Aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            Return
                        End If
                    End If
                Catch ex As Exception
                    ' Si falla por token, intentar por nombre
                End Try
            End If

            ' Intentar restaurar por nombre si el token falló
            If Not String.IsNullOrEmpty(entryPointSettings.EntryPointMethodName) AndAlso
               Not String.IsNullOrEmpty(entryPointSettings.EntryPointTypeName) Then

                Try
                    ' Buscar el tipo
                    Dim targetType As dnlib.DotNet.TypeDef = form.Assembly.Types.FirstOrDefault(
                        Function(t) String.Equals(t.FullName, entryPointSettings.EntryPointTypeName, StringComparison.Ordinal))

                    If targetType IsNot Nothing Then
                        ' Buscar el método
                        Dim targetMethod As dnlib.DotNet.MethodDef = targetType.Methods.FirstOrDefault(
                            Function(m) String.Equals(m.Name.String, entryPointSettings.EntryPointMethodName, StringComparison.Ordinal) AndAlso m.IsStatic)

                        If targetMethod IsNot Nothing Then
                            ' Restaurar entrypoint
                            form.EntryPointToken = targetMethod.MDToken.Raw
                            form.EntryPointMethod = targetMethod
                            form.Guna2TextBox4.Text = $"{entryPointSettings.EntryPointTypeName}.{entryPointSettings.EntryPointMethodName}"

                            ' Mostrar mensaje de éxito
                            MessageBox.Show($"EntryPoint restaurado: {entryPointSettings.EntryPointMethodName}",
                                          "EntryPoint Aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            Return
                        End If
                    End If
                Catch ex As Exception
                    ' Error buscando por nombre
                End Try
            End If

            ' Si llegamos aquí, no se pudo restaurar el entrypoint
            If Not String.IsNullOrEmpty(entryPointSettings.EntryPointMethodName) Then
                MessageBox.Show($"No se pudo restaurar el entrypoint '{entryPointSettings.EntryPointMethodName}'. " &
                              $"Puede que el método haya sido modificado o eliminado del ensamblado.",
                              "EntryPoint no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error al aplicar configuraciones del entrypoint: {ex.Message}",
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Exporta un preset a una ubicación específica
    ''' </summary>
    Public Shared Function ExportPreset(preset As ProtectionPreset, filePath As String) As Boolean
        Try
            Dim json As String = JsonConvert.SerializeObject(preset, Formatting.Indented)
            File.WriteAllText(filePath, json)
            Return True
        Catch ex As Exception
            MessageBox.Show($"Error al exportar preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Importa un preset desde una ubicación específica
    ''' </summary>
    Public Shared Function ImportPreset(filePath As String) As ProtectionPreset
        Try
            If Not File.Exists(filePath) Then
                Throw New FileNotFoundException($"El archivo no existe: {filePath}")
            End If

            Dim json As String = File.ReadAllText(filePath)
            Return JsonConvert.DeserializeObject(Of ProtectionPreset)(json)
        Catch ex As Exception
            MessageBox.Show($"Error al importar preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Crea presets por defecto
    ''' </summary>
    Private Shared Sub CreateDefaultPresets()
        Try
            ' Preset básico
            CreateBasicPreset()

            ' Preset avanzado
            CreateAdvancedPreset()

            ' Preset máximo
            CreateMaximumPreset()

            ' Preset solo renombrado
            CreateRenamerOnlyPreset()
        Catch ex As Exception
            ' Si hay error creando presets por defecto, no mostrar mensaje
        End Try
    End Sub

    Private Shared Sub CreateBasicPreset()
        Dim preset As New ProtectionPreset()
        preset.Name = "Básico"
        preset.Description = "Configuración básica de protección con renombrado y cifrado de strings"
        preset.Version = "1.0"
        preset.Created = DateTime.Now

        ' Configurar renamer básico
        With preset.Renamer
            .Enabled = True
            .Engine = 0 ' dnlib
            .Mode = 2 ' Alphanumeric
            .Length = 10
            .NamespaceEx = True
            .ClassName = True
            .Methods = True
            .Properties = True
            .Fields = True
        End With

        ' Configurar protecciones básicas
        With preset.Protections
            .StringEncryption = True
            .ControlFlow = True
            .ReduceMetadata = True
        End With

        SavePreset(preset, "Básico")
    End Sub

    Private Shared Sub CreateAdvancedPreset()
        Dim preset As New ProtectionPreset()
        preset.Name = "Avanzado"
        preset.Description = "Configuración avanzada con múltiples protecciones"
        preset.Version = "1.0"
        preset.Created = DateTime.Now

        ' Configurar renamer avanzado
        With preset.Renamer
            .Enabled = True
            .Engine = 1 ' AsmResolver
            .Mode = 3 ' Chinese
            .Length = 15
            .NamespaceEx = True
            .ClassName = True
            .Methods = True
            .Properties = True
            .Fields = True
            .Events = True
            .Resources = True
        End With

        ' Configurar protecciones avanzadas
        With preset.Protections
            .StringEncryption = True
            .ProxyStrings = True
            .ControlFlow = True
            .ControlFlowStrong = True
            .SugarControlFlow = True
            .ProxyClasses = True
            .ProxyMethods = True
            .MutationV2 = True
            .IntConfusion = True
            .EncodeIntegers = True
            .L2F = True
            .ReduceMetadata = True
            .NopAttack = True
            .AddJunkCode = True
            .JunkCodeNumber = 20
        End With

        ' Configurar Anti protecciones
        With preset.Anti
            .AntiDebug = True
            .AntiDump = True
            .AntiTamper = True
            .AntiInvoke = True
        End With

        SavePreset(preset, "Avanzado")
    End Sub

    Private Shared Sub CreateMaximumPreset()
        Dim preset As New ProtectionPreset()
        preset.Name = "Máximo"
        preset.Description = "Configuración con el máximo nivel de protección (puede ser inestable)"
        preset.Version = "1.0"
        preset.Created = DateTime.Now

        ' Configurar renamer máximo
        With preset.Renamer
            .Enabled = True
            .Engine = 1 ' AsmResolver
            .Mode = 8 ' Invisible
            .Length = 20
            .NamespaceEx = True
            .ClassName = True
            .Methods = True
            .Properties = True
            .Fields = True
            .Events = True
            .Resources = True
            .ModuleRenaming = True
            .ResourceEncryption = True
            .ResourceCompressEncrypt = True
            .UnsafeMode = True
        End With

        ' Activar todas las protecciones
        With preset.Protections
            .ImportProtection = True
            .SUFConfusion = True
            .ProxyClasses = True
            .ProxyClassesUnsafe = True
            .ProxyMethods = True
            .MoveVariables = True
            .L2F = True
            .EntryPointMover = True
            .Method2Delegate = True
            .ReduceMetadata = True
            .NopAttack = True
            .StringEncryption = True
            .ProxyStrings = True
            .DynamicStrings = True
            .ReplaceObfuscation = True
            .IntConfusion = True
            .StringsHider = True
            .Calli = True
            .ProxyInt = True
            .DynamicInt = True
            .MutationV2 = True
            .MutationV2Unsafe = True
            .EncodeIntegers = True
            .FakeObfuscation = True
            .AddJunkCode = True
            .JunkCodeNumber = 50
            .ControlFlow = True
            .ControlFlowStrong = True
            .SugarControlFlow = True
            .EXGuardControlFlow = True
            .ProxyReferences = True
            .ProxyReferencesUnsafe = True
            .Mutator = True
            .MutatorUnsafe = True
            .AntiDecompiler = True
            .InvalidOpcodes = True
            .InvalidMD = True
            .StackUnfConfusion = True
            .HideMethods = True
            .DynamicMethods = True
            .CctorHider = True
            .DynamicCctor = True
            .CctorL2F = True
            .MethodError = True
            .CodeOptimizer = True
        End With

        ' Configurar VM
        With preset.VM
            .Enabled = True
            .SelectedVM = 0
            .ProtectRuntime = True
            .VirtualizeStrings = True
        End With

        ' Activar todas las anti protecciones
        With preset.Anti
            .AntiProxy = True
            .ExceptionManager = True
            .ElevationEscale = True
            .AntiDebug = True
            .JitFucker = True
            .AntiDump = True
            .ProtectAntiDump = True
            .ExtremeAD = True
            .AntiHTTPDebug = True
            .AntiInvoke = True
            .AntiTamper = True
            .Antide4dot = True
            .AntiMalicious = True
            .AntiILDasm = True
            .AntiAttach = True
            .ThreadHider = True
            .BypassAmsi = True
        End With

        ' Configurar output avanzado
        With preset.Output
            .PreserveAll = True
            .InvalidMetadata = True
            .JITHook = True
        End With

        SavePreset(preset, "Máximo")
    End Sub

    Private Shared Sub CreateRenamerOnlyPreset()
        Dim preset As New ProtectionPreset()
        preset.Name = "Solo Renombrado"
        preset.Description = "Solo renombrado de símbolos, ideal para debugging"
        preset.Version = "1.0"
        preset.Created = DateTime.Now

        ' Configurar solo renamer
        With preset.Renamer
            .Enabled = True
            .Engine = 0 ' dnlib
            .Mode = 2 ' Alphanumeric
            .Length = 8
            .NamespaceEx = True
            .ClassName = True
            .Methods = True
            .Properties = True
            .Fields = True
        End With

        SavePreset(preset, "Solo Renombrado")
    End Sub

End Class