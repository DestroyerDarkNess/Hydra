# Hydra

[Download on the official website](https://toolslib.net/downloads/viewdownload/600-hydranet/)
[Download on Github](https://github.com/DestroyerDarkNess/Hydra/releases/tag/1.1.3)

# Embed DLL assemblies into executable

![{DCC3C1D8-5A40-419B-9D0A-871AEBC70F2D}](https://github.com/user-attachments/assets/12bdde6f-1b48-4f0b-8716-324d1d016252)
 
# Protections Preview 

![{FBFBDF60-8891-4408-BF11-92BAB1FB9BCB}](https://github.com/user-attachments/assets/b94c0579-a161-41e2-b292-48a7e5ff9a9a)

![Untitled](https://github.com/user-attachments/assets/041f21a5-0d17-4211-8c2b-14baf4008dfb)

# Packers

![{5F861715-1FE7-40BE-88B6-B68802B89384}](https://github.com/user-attachments/assets/01482d1f-650a-414c-af36-8c34d826e735)

## NativeV2 Packer Warning

![{4E879C52-37D8-455F-9728-720497078BA5}](https://github.com/user-attachments/assets/c65056e4-deb7-49ba-8876-03eb54843ae5)
 
 > **Warning**
>
> If you use Native Packer :
> ![{DD9A3F26-48DB-4690-B788-5ABD81AA0ADA}](https://github.com/user-attachments/assets/bf93839b-30ed-4144-81a9-80e2f5a7a417)
> You should combine it with Extreme AntiDump :
> ![{DBA730AB-B4CB-42E1-998B-EAEE1E7F24B7}](https://github.com/user-attachments/assets/ba97ede7-f697-44a8-8029-0e464b4c5d0c)
> 
> **And the final result should be protected with VMP or some other similar software to prevent it from being unpacked.**

The NativeV2 Packer itself is just a loader written in C for .net, its purpose is simply to add a native layer, but without protection, so it must be protected in the end with VMP or similar software .

# Command Line Support

Hydra.NET now supports command line automation for CI/CD pipelines and batch processing:

## Quick Examples

```cmd
# Basic protection with console output
Hydra.exe -file "MyApp.exe" -preset "Basic" -mode console

# Silent protection for automation
Hydra.exe -file "MyLibrary.dll" -preset "Advanced" -mode hidden

# Custom preset with specific output
Hydra.exe -file "MyApp.exe" -preset-file "custom.json" -output "MyApp_Secured.exe"

# GUI mode with auto-load
Hydra.exe -file "MyApp.exe" -preset "Maximum" -mode gui
```

## Available Modes
- **GUI Mode**: Traditional interface (default)
- **Console Mode**: Show progress in console, hide GUI  
- **Hidden Mode**: Run completely silently for automation

## Built-in Presets
- **Basic**: Alphanumeric renaming + string encryption + control flow
- **Advanced**: Chinese renaming + multiple protections + anti-debug/dump
- **Maximum**: All protections enabled (may cause instability)
- **Renaming Only**: Symbol renaming only (debugging-friendly)

## Documentation
- 📖 [Complete Command Line Guide](Hydra/COMMANDLINE_README.md)
- 📝 [Preset System Documentation](Hydra/PRESETS_README.md)
- 📁 [Example Scripts](Hydra/Examples/)

## Automation Examples
- [Batch Script](Hydra/Examples/ProtectFiles.bat) - Windows batch automation
- [PowerShell Script](Hydra/Examples/ProtectFiles.ps1) - Advanced PowerShell automation

# More...

![{F05B08A1-F9DF-425F-9131-5D6C65F40D9B}](https://github.com/user-attachments/assets/66f47bdb-e831-47f2-83d3-37ac3a095f19)







