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
            .Namespace = True
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
    
End Module 