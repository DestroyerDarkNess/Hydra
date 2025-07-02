# Protection Preset System - Hydra

This document describes the preset system implemented in Hydra for saving, loading, and sharing protection configurations.

## Features

- **Automatic saving** of all protector configurations
- **Default presets** ready to use
- **Import and export** of presets in JSON format
- **Complete management** with graphical interface
- **Full compatibility** with all available protections

## File Structure

```
Hydra/
├── Core/
│   ├── ProtectionPreset.vb       # Data classes for presets
│   └── PresetManager.vb          # Main preset manager
├── Forms/
│   ├── PresetManager.vb          # Management form
│   └── PresetManager.Designer.vb # Form design
└── Examples/
    └── PresetUsageExample.vb     # Usage examples
```

## Default Presets

The system includes 4 predefined presets:

### 1. **Basic**
- Symbol renaming (Alphanumeric, 10 characters)
- String encryption
- Basic control flow
- Metadata reduction

### 2. **Advanced**
- Advanced renaming (Chinese, 15 characters)
- Multiple string and integer protections
- Robust control flow
- Proxy protections
- Mutation v2
- Anti-debug, anti-dump, anti-tamper

### 3. **Maximum**
- **WARNING!** Extreme configuration that may be unstable
- All protections enabled
- Method virtualization
- All anti-protections
- Invisible renaming

### 4. **Renaming Only**
- Symbol renaming only
- Ideal for debugging and development

## Code Usage

### Save Current Configuration
```vb
' Create preset from current form
Dim preset As ProtectionPreset = PresetManager.CreatePresetFromForm(form)
preset.Name = "My Configuration"
preset.Description = "Custom configuration"

' Save
If PresetManager.SavePreset(preset, "MyConfig") Then
    MessageBox.Show("Preset saved successfully")
End If
```

### Load a Preset
```vb
' Load specific preset
Dim preset As ProtectionPreset = PresetManager.LoadPreset("Basic")
If preset IsNot Nothing Then
    PresetManager.ApplyPresetToForm(preset, form)
End If
```

### List Available Presets
```vb
Dim presets As List(Of String) = PresetManager.GetAvailablePresets()
For Each presetName As String In presets
    Console.WriteLine(presetName)
Next
```

### Create Preset Programmatically
```vb
Dim preset As New ProtectionPreset()
preset.Name = "Custom"
preset.Description = "Custom configuration"

' Configure renamer
With preset.Renamer
    .Enabled = True
    .Mode = 2 ' Alphanumeric
    .Length = 10
    .Namespace = True
    .ClassName = True
    .Methods = True
End With

' Configure protections
With preset.Protections
    .StringEncryption = True
    .ControlFlow = True
    .IntConfusion = True
End With

PresetManager.SavePreset(preset, "MyCustomPreset")
```

## Graphical Interface Usage

### Open Preset Manager
```vb
' From main form
form.OpenPresetManager()
```

### Available Functions
- **Save**: Saves current configuration as new preset
- **Load**: Applies selected preset
- **Delete**: Removes a preset
- **Export**: Saves preset to JSON file
- **Import**: Loads preset from JSON file
- **Info**: Shows detailed preset information

## JSON Format

Presets are saved in readable JSON format:

```json
{
  "name": "My Preset",
  "description": "Preset description",
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
  // ... more configurations
}
```

## File Location

Presets are saved in:
```
[ApplicationDirectory]/Presets/
├── Basic.json
├── Advanced.json
├── Maximum.json
├── Renaming Only.json
└── [YourCustomPresets].json
```

## Import/Export Presets

### Export
1. Select a preset in the manager
2. Click "Export"
3. Choose location and file name
4. The preset is saved as a .json file

### Import
1. Click "Import" in the manager
2. Select a preset .json file
3. Confirm the preset name
4. The preset is added to your collection

## Best Practices

### Naming
- Use descriptive names: "Production_v1", "Debug_Mode", "High_Security"
- Include version if necessary: "MyConfig_v2.1"
- Avoid special characters in file names

### Organization
- Create specific presets for different project types
- Keep a "debugging" preset with minimal protections
- Use detailed descriptions to remember the purpose

### Backup
- Regularly export your important presets
- Keep backups of critical configurations
- Share useful presets with your team

## Troubleshooting

### Preset Won't Load
- Verify that the JSON file is not corrupted
- Check that the preset is compatible with the current version
- Review permissions for the Presets folder

### Configuration Not Applied
- Some controls may not exist in different versions
- The system automatically skips incompatible configurations
- Check the log for error messages

### Save Error
- Verify write permissions in the Presets folder
- Check that you're not using invalid characters in the name
- Ensure you have available disk space

## Compatibility

### Versions
- Presets include version information
- Forward compatibility guaranteed
- Automatic migration when possible

### Protections
- All current protections are compatible
- New protections are added automatically
- Obsolete protections are silently ignored

## Custom EntryPoints

### DLL Functionality

The preset system includes full support for custom entrypoints, especially useful for DLLs:

### Automatic Capture
- The selected entrypoint is automatically saved in the preset
- Includes method token, name, type, and source assembly
- Only captured if a custom entrypoint is configured

### Intelligent Restoration
- **Compatibility verification**: Only restores if the assembly matches
- **Token search**: Attempts to restore using the original method token
- **Name search**: If token fails, searches by method and type name
- **Informative messages**: Notifies about success, incompatibility, or errors

### Saved Information
```json
{
  "entryPoint": {
    "hasCustomEntryPoint": true,
    "entryPointToken": 100663297,
    "entryPointMethodName": "Main",
    "entryPointTypeName": "MyNamespace.Program", 
    "assemblyName": "MyDLL",
    "assemblyFullName": "MyDLL, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
  }
}
```

### Use Cases
- **DLLs without native entrypoint**: Configure a method as entry point
- **Configuration reuse**: Maintain the same configuration across similar projects
- **Automated workflows**: Automatically restore complete configuration

### Limitations
- Only works with the same assembly (verification by name)
- Method must exist and maintain its signature
- Requires the method to be static and public

## System Extension

To add new configurations to the preset system:

1. **Add properties** to classes in `ProtectionPreset.vb`
2. **Update `CreatePresetFromForm`** in `PresetManager.vb`
3. **Update `ApplyPresetToForm`** in `PresetManager.vb`
4. **Add JSON attributes** for serialization

Example:
```vb
' In ProtectionSettings
<JsonProperty("myNewProtection")>
Public Property MyNewProtection As Boolean = False

' In CreatePresetFromForm
.MyNewProtection = form.MyNewProtectionCheck.Checked

' In ApplyPresetToForm  
form.MyNewProtectionCheck.Checked = .MyNewProtection
```

---

**Note**: This system is designed to be expandable and maintainable. Any new protection added to the system can be easily integrated into the preset system by following the established patterns. 