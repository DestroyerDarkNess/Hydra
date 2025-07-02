# Hydra.NET - Command Line Usage

Hydra.NET now supports command line execution, allowing you to automate the protection process without the graphical interface.

## Execution Modes

### 1. GUI Mode (Default)
Runs the traditional graphical interface:
```cmd
Hydra.exe
```

### 2. Console Mode
Shows console with real-time progress but hides the GUI:
```cmd
Hydra.exe -file "MyApp.exe" -preset "Basic" -mode console
```

### 3. Hidden Mode
Runs completely in the background without showing any interface:
```cmd
Hydra.exe -file "MyApp.exe" -preset "Advanced" -mode hidden
```

### 4. GUI Mode with Auto-Load
Shows the GUI but automatically loads the specified file and preset:
```cmd
Hydra.exe -file "MyApp.exe" -preset "Maximum" -mode gui
```

## Command Line Arguments

| Argument | Alias | Description | Required |
|----------|-------|-------------|----------|
| `-file` | `-f` | Input .NET assembly (.exe or .dll) | Yes (for command mode) |
| `-preset` | `-p` | Preset name (Basic, Advanced, Maximum, Renaming Only) | Yes* |
| `-preset-file` | `-pf` | Path to custom JSON preset file | Yes* |
| `-output` | `-o` | Output file path (optional, auto-generated) | No |
| `-mode` | `-m` | Execution mode (gui, console, hidden) | No |
| `-help` | `-h` | Show help | No |

*Either `-preset` OR `-preset-file` must be specified, but not both.

## Usage Examples

### Basic Protection with Console
```cmd
Hydra.exe -file "MyApplication.exe" -preset "Basic" -mode console
```

### Advanced Silent Protection
```cmd
Hydra.exe -file "MyLibrary.dll" -preset "Advanced" -mode hidden -output "MyLibrary_Secured.dll"
```

### Using Custom Preset
```cmd
Hydra.exe -file "MyApp.exe" -preset-file "C:\MyPresets\CustomProtection.json" -mode console
```

### Auto-Load in GUI
```cmd
Hydra.exe -file "MyApp.exe" -preset "Maximum" -mode gui
```

### Batch Processing
```batch
@echo off
echo Protecting multiple files...

Hydra.exe -file "App1.exe" -preset "Basic" -mode hidden
if %errorlevel% neq 0 goto error

Hydra.exe -file "App2.exe" -preset "Advanced" -mode hidden  
if %errorlevel% neq 0 goto error

Hydra.exe -file "Library.dll" -preset "Renaming Only" -mode hidden
if %errorlevel% neq 0 goto error

echo ✓ All files protected successfully
goto end

:error
echo ✗ Error during protection
exit /b 1

:end
pause
```

## Exit Codes

| Code | Description |
|------|-------------|
| 0 | Protection successful |
| 1 | Error during protection |

## Available Presets

### Basic
- Alphanumeric renaming
- String encryption
- Basic control flow

### Advanced  
- Chinese character renaming
- Multiple protections enabled
- Anti-debug, anti-dump, anti-tamper

### Maximum
- All protections enabled
- ⚠️ May cause instability

### Renaming Only
- Symbol renaming only
- Ideal for debugging

## Script Integration

### PowerShell
```powershell
# Protect file with error handling
$result = Start-Process -FilePath "Hydra.exe" -ArgumentList "-file `"MyApp.exe`" -preset `"Basic`" -mode hidden" -Wait -PassThru

if ($result.ExitCode -eq 0) {
    Write-Host "✓ Protection successful" -ForegroundColor Green
} else {
    Write-Host "✗ Protection failed" -ForegroundColor Red
}
```

### Python
```python
import subprocess
import sys

def protect_file(input_file, preset, mode="hidden"):
    cmd = [
        "Hydra.exe",
        "-file", input_file,
        "-preset", preset,
        "-mode", mode
    ]
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    
    if result.returncode == 0:
        print(f"✓ {input_file} protected successfully")
        return True
    else:
        print(f"✗ Error protecting {input_file}")
        print(result.stderr)
        return False

# Usage example
files_to_protect = [
    ("App1.exe", "Basic"),
    ("App2.exe", "Advanced"),
    ("Library.dll", "Renaming Only")
]

for file_path, preset in files_to_protect:
    protect_file(file_path, preset)
```

## CI/CD Automation

### GitHub Actions
```yaml
name: Protect Binaries
on: [push]

jobs:
  protect:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Build Application
      run: dotnet build --configuration Release
    
    - name: Protect with Hydra
      run: |
        Hydra.exe -file "bin/Release/MyApp.exe" -preset "Advanced" -mode hidden
        
    - name: Upload Protected Binary
      uses: actions/upload-artifact@v2
      with:
        name: protected-binary
        path: bin/Release/MyApp_Protected.exe
```

## Tips and Best Practices

1. **Use `hidden` mode for automation** - No user interaction required
2. **Always specify output path** - To avoid overwriting files
3. **Validate exit codes** - To detect errors in scripts
4. **Use appropriate presets** - Basic for development, Advanced for production
5. **Backup original files** - Before protecting

## Troubleshooting

### Error: "Input file does not exist"
- Verify the file path is correct
- Use absolute paths if having issues with relative paths

### Error: "Preset not found"
- Verify the preset exists in the HydraPresets folder
- Use exact names (case-sensitive)

### Error: "Failed to load assembly"
- Ensure it's a valid .NET assembly
- Verify it's not corrupted or already protected

### Application doesn't respond in hidden mode
- This is normal behavior, use exit codes to verify status
- Switch to console mode to see progress

## Configuration Files

Presets are stored in:
```
[AppDirectory]/HydraPresets/
├── Basic.json
├── Advanced.json  
├── Maximum.json
└── Renaming Only.json
```

You can create custom presets by saving them from the GUI or creating JSON files manually. 