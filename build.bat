@echo off
REM ============================================================================
REM ExtremeDumper Build Script v5.0
REM Multi-Target Build: .NET Framework 4.8.1 & .NET 8.0
REM Visual Studio 2026 Community & BuildTools Integration
REM ============================================================================

setlocal enabledelayedexpansion
cd /d "%~dp0"

REM ============================================================================
REM Colors & Logging
REM ============================================================================
for /F %%A in ('echo prompt $H ^| cmd') do set "BS=%%A"

REM ============================================================================
REM Configuration
REM ============================================================================
set "VS_COMMUNITY=C:\Program Files\Microsoft Visual Studio\18\Community"
set "VS_BUILDTOOLS=C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools"
set "VCVARS_PATH=%VS_BUILDTOOLS%\VC\Auxiliary\Build\vcvarsall.bat"
set "MSBUILD_EXE=%VS_COMMUNITY%\MSBuild\Current\Bin\MSBuild.exe"
set "DOTNET_CLI=dotnet"

set "SOLUTION=ExtremeDumper.sln"
set "OUTPUT_DIR=bin\Release"
set "PLATFORM_X86=Win32"
set "PLATFORM_X64=x64"
set "PLATFORM_ANY=Any CPU"

echo.
echo ============================================================================
echo ExtremeDumper Build System v5.0
echo ============================================================================
echo.

REM ============================================================================
REM Validation
REM ============================================================================
echo [INFO] Validating environment...

if not exist "%SOLUTION%" (
    echo [ERROR] Solution file not found: %SOLUTION%
    exit /b 1
)

if not exist "%VS_COMMUNITY%" (
    echo [WARNING] VS 2026 Community not found: %VS_COMMUNITY%
    echo [INFO] Attempting to use MSBuild from PATH...
    where /q msbuild
    if errorlevel 1 (
        echo [ERROR] MSBuild not found in PATH
        exit /b 1
    )
    set "MSBUILD_EXE=msbuild"
) else (
    echo [SUCCESS] VS 2026 Community found
)

if not exist "%VCVARS_PATH%" (
    echo [WARNING] VC++ Build Tools vcvarsall.bat not found
    echo [INFO] C++ projects may not build correctly
) else (
    echo [SUCCESS] VC++ Build Tools found
)

REM ============================================================================
REM .NET SDK Check
REM ============================================================================
echo.
echo [INFO] Checking .NET SDKs...
for /f "tokens=*" %%i in ('%DOTNET_CLI% --list-sdks 2^>nul') do (
    echo [INFO] SDK: %%i
)

REM ============================================================================
REM Clean Previous Builds
REM ============================================================================
echo.
echo [INFO] Cleaning previous builds...
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%" 2>nul
    echo [SUCCESS] Output directory cleaned
)

REM ============================================================================
REM Restore NuGet Packages
REM ============================================================================
echo.
echo [INFO] Restoring NuGet packages...
echo Command: %DOTNET_CLI% restore %SOLUTION%
%DOTNET_CLI% restore %SOLUTION%
if errorlevel 1 (
    echo [ERROR] NuGet restore failed
    exit /b 1
)
echo [SUCCESS] NuGet packages restored

REM ============================================================================
REM Initialize VC++ Environment (if available)
REM ============================================================================
if exist "%VCVARS_PATH%" (
    echo.
    echo [INFO] Initializing VC++ environment...
    call "%VCVARS_PATH%" x64 2>nul
    if errorlevel 1 (
        echo [WARNING] Failed to initialize x64 VC++ environment
    ) else (
        echo [SUCCESS] VC++ environment initialized for x64
    )
)

REM ============================================================================
REM Build .NET Framework 4.8.1 (x86)
REM ============================================================================
echo.
echo ============================================================================
echo Building: .NET Framework 4.8.1 - x86
echo ============================================================================
echo Command: %MSBUILD_EXE% %SOLUTION% /p:Configuration=Release /p:Platform=%PLATFORM_X86% /p:TargetFramework=net481 /m /verbosity:normal /nologo

%MSBUILD_EXE% %SOLUTION% /p:Configuration=Release /p:Platform=%PLATFORM_X86% /p:TargetFramework=net481 /m /verbosity:normal /nologo
if errorlevel 1 (
    echo [ERROR] Build failed for .NET Framework 4.8.1 - x86
    REM Continue to next build instead of failing
) else (
    echo [SUCCESS] Build completed for .NET Framework 4.8.1 - x86
)

REM ============================================================================
REM Build .NET Framework 4.8.1 (x64)
REM ============================================================================
echo.
echo ============================================================================
echo Building: .NET Framework 4.8.1 - x64
echo ============================================================================
echo Command: %MSBUILD_EXE% %SOLUTION% /p:Configuration=Release /p:Platform=%PLATFORM_X64% /p:TargetFramework=net481 /m /verbosity:normal /nologo

%MSBUILD_EXE% %SOLUTION% /p:Configuration=Release /p:Platform=%PLATFORM_X64% /p:TargetFramework=net481 /m /verbosity:normal /nologo
if errorlevel 1 (
    echo [ERROR] Build failed for .NET Framework 4.8.1 - x64
) else (
    echo [SUCCESS] Build completed for .NET Framework 4.8.1 - x64
)

REM ============================================================================
REM Build .NET 8.0 (x86)
REM ============================================================================
echo.
echo ============================================================================
echo Building: .NET 8.0 - x86
echo ============================================================================
echo Command: %DOTNET_CLI% build %SOLUTION% --configuration Release --os win --arch x86 --framework net8.0-windows

%DOTNET_CLI% build %SOLUTION% --configuration Release --os win --arch x86 --framework net8.0-windows
if errorlevel 1 (
    echo [ERROR] Build failed for .NET 8.0 - x86
) else (
    echo [SUCCESS] Build completed for .NET 8.0 - x86
)

REM ============================================================================
REM Build .NET 8.0 (x64)
REM ============================================================================
echo.
echo ============================================================================
echo Building: .NET 8.0 - x64
echo ============================================================================
echo Command: %DOTNET_CLI% build %SOLUTION% --configuration Release --os win --arch x64 --framework net8.0-windows

%DOTNET_CLI% build %SOLUTION% --configuration Release --os win --arch x64 --framework net8.0-windows
if errorlevel 1 (
    echo [ERROR] Build failed for .NET 8.0 - x64
) else (
    echo [SUCCESS] Build completed for .NET 8.0 - x64
)

REM ============================================================================
REM Output Summary
REM ============================================================================
echo.
echo ============================================================================
echo Build Summary
echo ============================================================================
echo.
echo Output Directory: %OUTPUT_DIR%
echo.

if exist "%OUTPUT_DIR%" (
    echo [INFO] Build artifacts:
    dir /b "%OUTPUT_DIR%" 2>nul | findstr /v /c:"^$"
    echo.
    echo [SUCCESS] Builds completed. Check output directory for binaries.
) else (
    echo [WARNING] Output directory does not exist
)

echo.
echo ============================================================================
echo Build process finished
echo ============================================================================
echo.

pause
endlocal