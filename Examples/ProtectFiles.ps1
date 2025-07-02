# Hydra.NET - PowerShell Protection Script
param(
    [string]$InputDir = "Input",
    [string]$OutputDir = "Protected", 
    [string]$DefaultPreset = "Basic",
    [switch]$Verbose,
    [switch]$WhatIf
)

# Configuration
$HydraPath = "Hydra.exe"
$LogFile = "protection_log_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"

# Function to write log
function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    
    switch ($Level) {
        "ERROR" { Write-Host $logMessage -ForegroundColor Red }
        "WARNING" { Write-Host $logMessage -ForegroundColor Yellow }
        "SUCCESS" { Write-Host $logMessage -ForegroundColor Green }
        default { Write-Host $logMessage -ForegroundColor White }
    }
    
    $logMessage | Out-File -FilePath $LogFile -Append
}

# Function to protect a file
function Protect-File {
    param(
        [string]$FilePath,
        [string]$Preset,
        [string]$OutputPath,
        [string]$Mode = "hidden"
    )
    
    if ($WhatIf) {
        Write-Log "WHATIF: Would protect $FilePath with preset $Preset"
        return $true
    }
    
    Write-Log "Protecting: $FilePath with preset: $Preset"
    
    $arguments = @(
        "-file", "`"$FilePath`"",
        "-preset", "`"$Preset`"",
        "-output", "`"$OutputPath`"",
        "-mode", $Mode
    )
    
    if ($Verbose) {
        $arguments[10] = "console"  # Change mode to console if verbose
    }
    
    try {
        $process = Start-Process -FilePath $HydraPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
        
        if ($process.ExitCode -eq 0) {
            Write-Log "✓ Successfully protected: $(Split-Path $FilePath -Leaf)" "SUCCESS"
            
            if (Test-Path $OutputPath) {
                $size = (Get-Item $OutputPath).Length
                $sizeKB = [math]::Round($size / 1KB, 2)
                Write-Log "  Output size: $sizeKB KB"
            }
            
            return $true
        } else {
            Write-Log "✗ Failed to protect: $(Split-Path $FilePath -Leaf) (Exit code: $($process.ExitCode))" "ERROR"
            return $false
        }
    }
    catch {
        Write-Log "✗ Exception protecting: $(Split-Path $FilePath -Leaf) - $($_.Exception.Message)" "ERROR"
        return $false
    }
}

# Script initialization
Write-Log "================================================="
Write-Log "Hydra.NET PowerShell Protection Script Started"
Write-Log "================================================="

# Verify dependencies
if (-not (Test-Path $HydraPath)) {
    Write-Log "ERROR: Hydra.exe not found at: $HydraPath" "ERROR"
    Write-Log "Make sure Hydra.exe is in the current directory or update the HydraPath variable"
    exit 1
}

Write-Log "✓ Hydra.exe found"

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Log "✓ Created output directory: $OutputDir"
}

# Verify input directory
if (-not (Test-Path $InputDir)) {
    Write-Log "WARNING: Input directory not found: $InputDir" "WARNING"
    Write-Log "Creating input directory..."
    New-Item -ItemType Directory -Path $InputDir -Force | Out-Null
    Write-Log "✓ Created input directory: $InputDir"
    Write-Log "Please place .NET assemblies in the $InputDir directory and run the script again."
    exit 0
}

# Search for files to protect
$filesToProtect = @()
$filesToProtect += Get-ChildItem -Path $InputDir -Filter "*.exe" -File
$filesToProtect += Get-ChildItem -Path $InputDir -Filter "*.dll" -File

if ($filesToProtect.Count -eq 0) {
    Write-Log "No .NET assemblies found in $InputDir" "WARNING"
    exit 0
}

Write-Log "Found $($filesToProtect.Count) files to protect:"
foreach ($file in $filesToProtect) {
    Write-Log "  - $($file.Name) ($([math]::Round($file.Length / 1KB, 2)) KB)"
}

# Protection configuration by file type
$protectionConfig = @{
    "*.exe" = @{
        "MyApp.exe" = "Basic"
        "ImportantApp.exe" = "Advanced" 
        "CriticalApp.exe" = "Maximum"
        "Default" = $DefaultPreset
    }
    "*.dll" = @{
        "Core.dll" = "Renaming Only"
        "Important.dll" = "Advanced"
        "Default" = "Basic"
    }
}

# Protect files
$successCount = 0
$failureCount = 0
$totalFiles = $filesToProtect.Count

Write-Log "Starting protection process..."
Write-Log "-" * 50

for ($i = 0; $i -lt $totalFiles; $i++) {
    $file = $filesToProtect[$i]
    $progress = [math]::Round((($i + 1) / $totalFiles) * 100, 1)
    
    Write-Log "[$($i + 1)/$totalFiles] ($progress%) Processing: $($file.Name)"
    
    # Determine preset based on configuration
    $preset = $DefaultPreset
    $extension = "*$($file.Extension)"
    
    if ($protectionConfig.ContainsKey($extension)) {
        $config = $protectionConfig[$extension]
        if ($config.ContainsKey($file.Name)) {
            $preset = $config[$file.Name]
        } else {
            $preset = $config["Default"]
        }
    }
    
    # Generate output path
    $outputFileName = $file.BaseName + "_Protected" + $file.Extension
    $outputPath = Join-Path $OutputDir $outputFileName
    
    # Protect file
    $mode = if ($Verbose) { "console" } else { "hidden" }
    $success = Protect-File -FilePath $file.FullName -Preset $preset -OutputPath $outputPath -Mode $mode
    
    if ($success) {
        $successCount++
    } else {
        $failureCount++
    }
    
    Write-Log ""
}

# Final summary
Write-Log "================================================="
Write-Log "Protection Summary"
Write-Log "================================================="
Write-Log "Total files processed: $totalFiles"
Write-Log "Successfully protected: $successCount" "SUCCESS"
Write-Log "Failed: $failureCount" $(if ($failureCount -gt 0) { "ERROR" } else { "INFO" })

if ($successCount -gt 0) {
    Write-Log ""
    Write-Log "Protected files are available in: $OutputDir"
    
    $protectedFiles = Get-ChildItem -Path $OutputDir -File | Sort-Object Name
    foreach ($file in $protectedFiles) {
        $sizeKB = [math]::Round($file.Length / 1KB, 2)
        Write-Log "  ✓ $($file.Name) ($sizeKB KB)" "SUCCESS"
    }
}

if ($failureCount -gt 0) {
    Write-Log ""
    Write-Log "Some files failed to protect. Check the log above for details." "WARNING"
    Write-Log "Common solutions:"
    Write-Log "- Ensure files are valid .NET assemblies"
    Write-Log "- Check file permissions"
    Write-Log "- Try different presets"
    Write-Log "- Run with -Verbose for more details"
}

Write-Log ""
Write-Log "Log file saved to: $LogFile"
Write-Log "Script completed with exit code: $(if ($failureCount -eq 0) { 0 } else { 1 })"

# Open output directory if there are protected files
if ($successCount -gt 0 -and -not $WhatIf) {
    $response = Read-Host "Open output directory? (y/N)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Invoke-Item $OutputDir
    }
}

exit $(if ($failureCount -eq 0) { 0 } else { 1 }) 