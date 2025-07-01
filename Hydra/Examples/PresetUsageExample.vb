Imports Hydra.Core

''' <summary>
''' Ejemplo de uso del sistema de presets de Hydra
''' </summary>
Public Module PresetUsageExample
    
    ''' <summary>
    ''' Ejemplo básico de cómo guardar la configuración actual como preset
    ''' </summary>
    Public Sub ExampleSavePreset(form As ProjectDesigner)
        ' Crear preset desde la configuración actual del formulario
        Dim preset As ProtectionPreset = PresetManager.CreatePresetFromForm(form)
        
        ' Configurar información del preset
        preset.Name = "Mi Configuración Personalizada"
        preset.Description = "Configuración personalizada para mis proyectos"
        preset.Version = "1.0"
        preset.Created = DateTime.Now
        
        ' Guardar el preset
        If PresetManager.SavePreset(preset, "MiConfiguracion") Then
            Console.WriteLine("Preset guardado exitosamente")
        End If
    End Sub
    
    ''' <summary>
    ''' Ejemplo de cómo cargar un preset específico
    ''' </summary>
    Public Sub ExampleLoadPreset(form As ProjectDesigner, presetName As String)
        ' Cargar el preset
        Dim preset As ProtectionPreset = PresetManager.LoadPreset(presetName)
        
        If preset IsNot Nothing Then
            ' Aplicar el preset al formulario
            PresetManager.ApplyPresetToForm(preset, form)
            Console.WriteLine($"Preset '{preset.Name}' aplicado exitosamente")
        Else
            Console.WriteLine("No se pudo cargar el preset")
        End If
    End Sub
    
    ''' <summary>
    ''' Ejemplo de cómo listar todos los presets disponibles
    ''' </summary>
    Public Sub ExampleListPresets()
        Dim presets As List(Of String) = PresetManager.GetAvailablePresets()
        
        Console.WriteLine("Presets disponibles:")
        For Each presetName As String In presets
            Console.WriteLine($"- {presetName}")
        Next
    End Sub
    
    ''' <summary>
    ''' Ejemplo de cómo crear un preset personalizado programáticamente
    ''' </summary>
    Public Sub ExampleCreateCustomPreset()
        Dim preset As New ProtectionPreset()
        
        ' Configurar información básica
        preset.Name = "Preset Personalizado"
        preset.Description = "Configuración creada programáticamente"
        preset.Version = "1.0"
        preset.Created = DateTime.Now
        
        ' Configurar renamer
        With preset.Renamer
            .Enabled = True
            .Engine = 0 ' dnlib
            .Mode = 2 ' Alphanumeric
            .Length = 12
            .BaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
            .NamespaceEx = True
            .ClassName = True
            .Methods = True
            .Properties = True
            .Fields = True
        End With
        
        ' Configurar protecciones específicas
        With preset.Protections
            .StringEncryption = True
            .ControlFlow = True
            .ProxyStrings = True
            .IntConfusion = True
            .ReduceMetadata = True
            .AddJunkCode = True
            .JunkCodeNumber = 15
        End With
        
        ' Configurar anti-protecciones
        With preset.Anti
            .AntiDebug = True
            .AntiDump = True
            .AntiTamper = True
        End With
        
        ' Guardar el preset
        If PresetManager.SavePreset(preset, "PersonalizadoProgramatico") Then
            Console.WriteLine("Preset personalizado creado exitosamente")
        End If
    End Sub
    
    ''' <summary>
    ''' Ejemplo de cómo exportar e importar presets
    ''' </summary>
    Public Sub ExampleExportImportPreset()
        ' Exportar un preset existente
        Dim preset As ProtectionPreset = PresetManager.LoadPreset("Básico")
        If preset IsNot Nothing Then
            If PresetManager.ExportPreset(preset, "C:\temp\MiPreset.json") Then
                Console.WriteLine("Preset exportado a C:\temp\MiPreset.json")
            End If
        End If
        
        ' Importar un preset
        Dim importedPreset As ProtectionPreset = PresetManager.ImportPreset("C:\temp\MiPreset.json")
        If importedPreset IsNot Nothing Then
            ' Cambiar el nombre para evitar conflictos
            importedPreset.Name = "Preset Importado"
            
            If PresetManager.SavePreset(importedPreset, "PresetImportado") Then
                Console.WriteLine("Preset importado exitosamente")
            End If
        End If
    End Sub
    
    ''' <summary>
    ''' Ejemplo de cómo eliminar un preset
    ''' </summary>
    Public Sub ExampleDeletePreset(presetName As String)
        If PresetManager.DeletePreset(presetName) Then
            Console.WriteLine($"Preset '{presetName}' eliminado exitosamente")
        Else
            Console.WriteLine($"No se pudo eliminar el preset '{presetName}'")
        End If
    End Sub
    
    ''' <summary>
    ''' Ejemplo completo de flujo de trabajo con presets
    ''' </summary>
    Public Sub ExampleCompleteWorkflow(form As ProjectDesigner)
        Console.WriteLine("=== Flujo de trabajo completo con presets ===")
        
        ' 1. Listar presets disponibles
        Console.WriteLine("1. Listando presets disponibles:")
        ExampleListPresets()
        
        ' 2. Crear un preset personalizado
        Console.WriteLine(vbCrLf & "2. Creando preset personalizado:")
        ExampleCreateCustomPreset()
        
        ' 3. Cargar un preset específico
        Console.WriteLine(vbCrLf & "3. Cargando preset 'Básico':")
        ExampleLoadPreset(form, "Básico")
        
        ' 4. Guardar configuración actual
        Console.WriteLine(vbCrLf & "4. Guardando configuración actual:")
        ExampleSavePreset(form)
        
        ' 5. Exportar e importar
        Console.WriteLine(vbCrLf & "5. Exportando e importando preset:")
        ExampleExportImportPreset()
        
        Console.WriteLine(vbCrLf & "=== Flujo completado ===")
    End Sub
    
    ''' <summary>
    ''' Ejemplo de cómo trabajar con entrypoints personalizados en presets
    ''' </summary>
    Public Sub ExampleEntryPointPresets(form As ProjectDesigner)
        Console.WriteLine("=== Trabajo con EntryPoints en Presets ===")
        
        ' Los entrypoints se guardan automáticamente en los presets cuando:
        ' 1. Se ha seleccionado un entrypoint personalizado en el formulario
        ' 2. Se guarda un preset usando CreatePresetFromForm
        
        ' Crear preset con entrypoint personalizado (si existe)
        Dim preset As ProtectionPreset = PresetManager.CreatePresetFromForm(form)
        preset.Name = "Preset con EntryPoint"
        preset.Description = "Preset que incluye configuración de entrypoint personalizado"
        
        ' Verificar si se capturó un entrypoint personalizado
        If preset.EntryPoint.HasCustomEntryPoint Then
            Console.WriteLine($"EntryPoint capturado: {preset.EntryPoint.EntryPointMethodName}")
            Console.WriteLine($"Tipo: {preset.EntryPoint.EntryPointTypeName}")
            Console.WriteLine($"Token: {preset.EntryPoint.EntryPointToken}")
            Console.WriteLine($"Ensamblado: {preset.EntryPoint.AssemblyName}")
        Else
            Console.WriteLine("No hay entrypoint personalizado configurado")
        End If
        
        ' Guardar el preset
        If PresetManager.SavePreset(preset, "PresetConEntryPoint") Then
            Console.WriteLine("Preset con entrypoint guardado exitosamente")
        End If
        
        ' Al cargar el preset, el entrypoint se restaurará automáticamente
        ' si el ensamblado actual es compatible
        Console.WriteLine(vbCrLf & "Cargando preset con entrypoint...")
        Dim loadedPreset As ProtectionPreset = PresetManager.LoadPreset("PresetConEntryPoint")
        If loadedPreset IsNot Nothing Then
            PresetManager.ApplyPresetToForm(loadedPreset, form)
            Console.WriteLine("Preset aplicado. El entrypoint se restaurará si es compatible.")
        End If
        
        Console.WriteLine(vbCrLf & "=== Notas importantes sobre EntryPoints ===")
        Console.WriteLine("- Los entrypoints se restauran solo si el ensamblado es compatible")
        Console.WriteLine("- Se verifica por nombre de ensamblado y luego por token/nombre del método")
        Console.WriteLine("- Si el método no existe, se muestra una advertencia")
        Console.WriteLine("- Los entrypoints son útiles principalmente para DLLs sin entrypoint nativo")
    End Sub
    
    ''' <summary>
    ''' Ejemplo de preset específico para DLLs con entrypoint personalizado
    ''' </summary>
    Public Sub ExampleDLLPresetWithEntryPoint()
        Dim preset As New ProtectionPreset()
        
        ' Configurar información básica
        preset.Name = "DLL con EntryPoint Personalizado"
        preset.Description = "Configuración optimizada para DLLs con entrypoint personalizado"
        preset.Version = "1.0"
        preset.Created = DateTime.Now
        
        ' Configuraciones básicas para DLL
        With preset.Renamer
            .Enabled = True
            .Engine = 0
            .Mode = 2 ' Alphanumeric
            .Length = 10
            .ClassName = True
            .Methods = True
            .Properties = True
            .Fields = True
        End With
        
        ' Protecciones ligeras para DLLs
        With preset.Protections
            .StringEncryption = True
            .ControlFlow = True
            .ReduceMetadata = True
            .ProxyStrings = True
        End With
        
        ' Configuraciones de EntryPoint
        ' Nota: Estos valores se configuran automáticamente desde el formulario
        ' Este es solo un ejemplo de la estructura
        With preset.EntryPoint
            .HasCustomEntryPoint = False ' Se establecerá automáticamente
            .EntryPointToken = 0         ' Se capturará desde el formulario
            .EntryPointMethodName = ""   ' Se capturará desde el formulario
            .EntryPointTypeName = ""     ' Se capturará desde el formulario
            .AssemblyName = ""           ' Se capturará desde el formulario
            .AssemblyFullName = ""       ' Se capturará desde el formulario
        End With
        
        ' Guardar el preset base
        If PresetManager.SavePreset(preset, "DLL_BaseConEntryPoint") Then
            Console.WriteLine("Preset base para DLL creado exitosamente")
            Console.WriteLine("Para usar con entrypoint: selecciona un método como entrypoint y luego guarda el preset")
        End If
    End Sub
    
End Module 