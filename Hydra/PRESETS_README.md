# Sistema de Presets de Protección - Hydra

Este documento describe el sistema de presets implementado en Hydra para guardar, cargar y compartir configuraciones de protección.

## Características

- **Guardado automático** de todas las configuraciones del protector
- **Presets por defecto** listos para usar
- **Importación y exportación** de presets en formato JSON
- **Gestión completa** con interfaz gráfica
- **Compatibilidad total** con todas las protecciones disponibles

## Estructura de Archivos

```
Hydra/
├── Core/
│   ├── ProtectionPreset.vb       # Clases de datos para presets
│   └── PresetManager.vb          # Gestor principal de presets
├── Forms/
│   ├── PresetManager.vb          # Formulario de gestión
│   └── PresetManager.Designer.vb # Diseño del formulario
└── Examples/
    └── PresetUsageExample.vb     # Ejemplos de uso
```

## Presets por Defecto

El sistema incluye 4 presets predefinidos:

### 1. **Básico**
- Renombrado de símbolos (Alphanumeric, 10 caracteres)
- Cifrado de strings
- Control flow básico
- Reducción de metadatos

### 2. **Avanzado**
- Renombrado avanzado (Chino, 15 caracteres)
- Múltiples protecciones de strings y enteros
- Control flow robusto
- Protecciones proxy
- Mutación v2
- Anti-debug, anti-dump, anti-tamper

### 3. **Máximo**
- **¡ATENCIÓN!** Configuración extrema que puede ser inestable
- Todas las protecciones habilitadas
- Virtualización de métodos
- Todas las anti-protecciones
- Renombrado invisible

### 4. **Solo Renombrado**
- Únicamente renombrado de símbolos
- Ideal para debugging y desarrollo

## Uso desde Código

### Guardar Configuración Actual
```vb
' Crear preset desde el formulario actual
Dim preset As ProtectionPreset = PresetManager.CreatePresetFromForm(formulario)
preset.Name = "Mi Configuración"
preset.Description = "Configuración personalizada"

' Guardar
If PresetManager.SavePreset(preset, "MiConfig") Then
    MessageBox.Show("Preset guardado exitosamente")
End If
```

### Cargar un Preset
```vb
' Cargar preset específico
Dim preset As ProtectionPreset = PresetManager.LoadPreset("Básico")
If preset IsNot Nothing Then
    PresetManager.ApplyPresetToForm(preset, formulario)
End If
```

### Listar Presets Disponibles
```vb
Dim presets As List(Of String) = PresetManager.GetAvailablePresets()
For Each presetName As String In presets
    Console.WriteLine(presetName)
Next
```

### Crear Preset Programáticamente
```vb
Dim preset As New ProtectionPreset()
preset.Name = "Custom"
preset.Description = "Configuración personalizada"

' Configurar renamer
With preset.Renamer
    .Enabled = True
    .Mode = 2 ' Alphanumeric
    .Length = 10
    .Namespace = True
    .ClassName = True
    .Methods = True
End With

' Configurar protecciones
With preset.Protections
    .StringEncryption = True
    .ControlFlow = True
    .IntConfusion = True
End With

PresetManager.SavePreset(preset, "MiPresetCustom")
```

## Uso desde Interfaz Gráfica

### Abrir el Gestor de Presets
```vb
' Desde el formulario principal
formulario.OpenPresetManager()
```

### Funciones Disponibles
- **Guardar**: Guarda la configuración actual como nuevo preset
- **Cargar**: Aplica un preset seleccionado
- **Eliminar**: Borra un preset
- **Exportar**: Guarda un preset en archivo JSON
- **Importar**: Carga un preset desde archivo JSON
- **Info**: Muestra información detallada del preset

## Formato JSON

Los presets se guardan en formato JSON legible:

```json
{
  "name": "Mi Preset",
  "description": "Descripción del preset",
  "version": "1.0",
  "created": "2024-01-01T12:00:00",
  "renamer": {
    "enabled": true,
    "engine": 0,
    "mode": 2,
    "length": 10,
    "namespace": true,
    "className": true,
    "methods": true
  },
  "protections": {
    "stringEncryption": true,
    "controlFlow": true,
    "intConfusion": false
  },
  // ... más configuraciones
}
```

## Ubicación de Archivos

Los presets se guardan en:
```
[DirectorioAplicación]/Presets/
├── Básico.json
├── Avanzado.json
├── Máximo.json
├── Solo Renombrado.json
└── [TusPresetsPersonalizados].json
```

## Importar/Exportar Presets

### Exportar
1. Selecciona un preset en el gestor
2. Haz clic en "Exportar"
3. Elige ubicación y nombre del archivo
4. El preset se guarda como archivo .json

### Importar
1. Haz clic en "Importar" en el gestor
2. Selecciona un archivo .json de preset
3. Confirma el nombre del preset
4. El preset se añade a tu colección

## Buenas Prácticas

### Nomenclatura
- Usa nombres descriptivos: "Production_v1", "Debug_Mode", "High_Security"
- Incluye versión si es necesario: "MyConfig_v2.1"
- Evita caracteres especiales en nombres de archivo

### Organización
- Crea presets específicos para diferentes tipos de proyectos
- Mantén un preset de "debugging" con protecciones mínimas
- Usa descripciones detalladas para recordar el propósito

### Backup
- Exporta regularmente tus presets importantes
- Mantén copias de seguridad de configuraciones críticas
- Comparte presets útiles con tu equipo

## Solución de Problemas

### Preset No Carga
- Verifica que el archivo JSON no esté corrupto
- Comprueba que el preset sea compatible con la versión actual
- Revisa los permisos de la carpeta Presets

### Configuración No Se Aplica
- Algunos controles pueden no existir en versiones diferentes
- El sistema omite configuraciones incompatibles automáticamente
- Revisa el log para mensajes de error

### Error al Guardar
- Verifica permisos de escritura en la carpeta Presets
- Comprueba que no uses caracteres inválidos en el nombre
- Asegúrate de tener espacio disponible en disco

## Compatibilidad

### Versiones
- Los presets incluyen información de versión
- Compatibilidad hacia adelante garantizada
- Migración automática cuando sea posible

### Protecciones
- Todas las protecciones actuales son compatibles
- Nuevas protecciones se añaden automáticamente
- Protecciones obsoletas se ignoran silenciosamente

## Extensión del Sistema

Para añadir nuevas configuraciones al sistema de presets:

1. **Agregar propiedades** a las clases en `ProtectionPreset.vb`
2. **Actualizar `CreatePresetFromForm`** en `PresetManager.vb`
3. **Actualizar `ApplyPresetToForm`** en `PresetManager.vb`
4. **Añadir atributos JSON** para serialización

Ejemplo:
```vb
' En ProtectionSettings
<JsonProperty("miNuevaProteccion")>
Public Property MiNuevaProteccion As Boolean = False

' En CreatePresetFromForm
.MiNuevaProteccion = form.MiNuevaProteccionCheck.Checked

' En ApplyPresetToForm  
form.MiNuevaProteccionCheck.Checked = .MiNuevaProteccion
```

---

**Nota**: Este sistema está diseñado para ser expandible y mantenible. Cualquier nueva protección añadida al sistema se puede integrar fácilmente en el sistema de presets siguiendo los patrones establecidos. 