@echo off
title Hydra.NET - Command Line Protection Example
color 0A

echo ====================================
echo  Hydra.NET Command Line Protection
echo ====================================
echo.

:: Configuration
set HYDRA_PATH=Hydra.exe
set INPUT_DIR=Input
set OUTPUT_DIR=Protected
set PRESET=Basic

:: Check if Hydra exists
if not exist "%HYDRA_PATH%" (
    echo ERROR: Hydra.exe not found!
    echo Make sure Hydra.exe is in the same directory as this script.
    pause
    exit /b 1
)

:: Create directories if they don't exist
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Checking for files to protect...
echo.

:: Example 1: Basic protection with console
if exist "%INPUT_DIR%\MyApp.exe" (
    echo [1/4] Protecting MyApp.exe with Basic preset...
    "%HYDRA_PATH%" -file "%INPUT_DIR%\MyApp.exe" -preset "%PRESET%" -output "%OUTPUT_DIR%\MyApp_Protected.exe" -mode console
    if %errorlevel% neq 0 (
        echo ERROR: Failed to protect MyApp.exe
        goto error
    )
    echo ✓ MyApp.exe protected successfully
    echo.
) else (
    echo [1/4] MyApp.exe not found, skipping...
    echo.
)

:: Example 2: Advanced silent protection
if exist "%INPUT_DIR%\MyLibrary.dll" (
    echo [2/4] Protecting MyLibrary.dll with Advanced preset (silent mode)...
    "%HYDRA_PATH%" -file "%INPUT_DIR%\MyLibrary.dll" -preset "Advanced" -output "%OUTPUT_DIR%\MyLibrary_Protected.dll" -mode hidden
    if %errorlevel% neq 0 (
        echo ERROR: Failed to protect MyLibrary.dll
        goto error
    )
    echo ✓ MyLibrary.dll protected successfully
    echo.
) else (
    echo [2/4] MyLibrary.dll not found, skipping...
    echo.
)

:: Example 3: Use custom preset
if exist "%INPUT_DIR%\CustomApp.exe" (
    if exist "CustomProtection.json" (
        echo [3/4] Protecting CustomApp.exe with custom preset...
        "%HYDRA_PATH%" -file "%INPUT_DIR%\CustomApp.exe" -preset-file "CustomProtection.json" -output "%OUTPUT_DIR%\CustomApp_Protected.exe" -mode console
        if %errorlevel% neq 0 (
            echo ERROR: Failed to protect CustomApp.exe
            goto error
        )
        echo ✓ CustomApp.exe protected successfully
        echo.
    ) else (
        echo [3/4] CustomProtection.json not found, using Basic preset instead...
        "%HYDRA_PATH%" -file "%INPUT_DIR%\CustomApp.exe" -preset "Basic" -output "%OUTPUT_DIR%\CustomApp_Protected.exe" -mode hidden
        if %errorlevel% neq 0 (
            echo ERROR: Failed to protect CustomApp.exe
            goto error
        )
        echo ✓ CustomApp.exe protected successfully
        echo.
    )
) else (
    echo [3/4] CustomApp.exe not found, skipping...
    echo.
)

:: Example 4: Maximum protection with warning
if exist "%INPUT_DIR%\ImportantApp.exe" (
    echo [4/4] Protecting ImportantApp.exe with Maximum preset...
    echo WARNING: Maximum preset may cause instability!
    echo.
    "%HYDRA_PATH%" -file "%INPUT_DIR%\ImportantApp.exe" -preset "Maximum" -output "%OUTPUT_DIR%\ImportantApp_Protected.exe" -mode console
    if %errorlevel% neq 0 (
        echo ERROR: Failed to protect ImportantApp.exe
        goto error
    )
    echo ✓ ImportantApp.exe protected successfully
    echo.
) else (
    echo [4/4] ImportantApp.exe not found, skipping...
    echo.
)

:: Show results
echo ====================================
echo  Protection Summary
echo ====================================
echo.
echo Protected files are saved in: %OUTPUT_DIR%\
echo.

if exist "%OUTPUT_DIR%\*.exe" (
    echo Executables:
    for %%f in ("%OUTPUT_DIR%\*.exe") do (
        echo   ✓ %%~nxf
    )
    echo.
)

if exist "%OUTPUT_DIR%\*.dll" (
    echo Libraries:
    for %%f in ("%OUTPUT_DIR%\*.dll") do (
        echo   ✓ %%~nxf
    )
    echo.
)

echo ✓ All protections completed successfully!
goto end

:error
echo.
echo ====================================
echo  ERROR OCCURRED
echo ====================================
echo.
echo Protection process failed. Check the error messages above.
echo.
echo Troubleshooting tips:
echo - Make sure input files are valid .NET assemblies
echo - Verify that the preset names are correct
echo - Check that you have write permissions to the output directory
echo - Try using a different preset if one fails
echo.
pause
exit /b 1

:end
echo.
echo Protection process completed!
echo.
echo You can now distribute the protected files from the '%OUTPUT_DIR%' folder.
echo.
pause 