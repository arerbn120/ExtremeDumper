# ExtremeDumper Build Guide v5.0

## Overview

ExtremeDumper has been modernized to support both **Microsoft .NET Framework 4.8.1** and **.NET 8.0**, with improved build automation for Visual Studio 2026.

## System Requirements

### Minimum Requirements

- **Windows 10/11** (x86 or x64)
- **Visual Studio 2026 Community** or **Build Tools**
  - C++ workload for native compilation support
  - .NET workload

### For .NET 8.0 Support

- **.NET 8.0 SDK** (or later)
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0

### For .NET Framework 4.8.1 Support

- **.NET Framework 4.8.1** (installed via Visual Studio or Windows Update)
- **Visual Studio 2019** or later with C# support

## Installation Paths

Ensure the following paths exist on your system:

```
C:\Program Files\Microsoft Visual Studio\18\Community
C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvarsall.bat
```

If your Visual Studio installation is in a different location, edit `build.bat` or `build.ps1` accordingly.

## Build Instructions

### Method 1: Batch Script (Windows Command Prompt)

#### Full Build (All targets: .NET 4.8.1 + .NET 8.0, x86 + x64)

```cmd
cd C:\path\to\ExtremeDumper
build.bat
```

#### Build Output

Success output will look like:

```
============================================================================
Build Summary
============================================================================

Output Directory: bin\Release

[INFO] Build artifacts:
ExtremeDumper.exe      (x86 - .NET 4.8.1)
ExtremeDumper-x86.exe  (x86 - .NET 4.8.1)
ExtremeDumper-x64.exe  (x64 - .NET 4.8.1)
[...more binaries...]
```

### Method 2: PowerShell Script (Advanced)

#### Enable PowerShell Script Execution (if needed)

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### Full Build

```powershell
.\build.ps1
```

#### Build with Specific Configuration

```powershell
# Debug configuration
.\build.ps1 -Configuration Debug

# Only .NET Framework 4.8.1
.\build.ps1 -Framework net481 -Architecture x64

# Only .NET 8.0, x86 architecture
.\build.ps1 -Framework net8.0-windows -Architecture x86

# Clean and rebuild everything
.\build.ps1 -Clean
```

#### Available Options

```
-Configuration {Debug|Release}                   Build configuration (default: Release)
-Framework {net481|net8.0-windows|all}          Target framework (default: all)
-Architecture {x86|x64|both}                    Target architecture (default: both)
-Clean                                           Clean output before building
-Help                                            Show help message
```

### Method 3: Manual Build with MSBuild

#### Build All Configurations

```cmd
# For .NET Framework 4.8.1 (x86)
msbuild ExtremeDumper.sln /p:Configuration=Release /p:Platform=Win32 /p:TargetFramework=net481

# For .NET Framework 4.8.1 (x64)
msbuild ExtremeDumper.sln /p:Configuration=Release /p:Platform=x64 /p:TargetFramework=net481
```

### Method 4: Using .NET CLI

#### Build .NET 8.0

```cmd
dotnet build ExtremeDumper.sln --configuration Release --framework net8.0-windows
```

#### Build Specific Project

```cmd
dotnet build ExtremeDumper\ExtremeDumper.csproj --configuration Release
```

## Project Structure

```
ExtremeDumper/
├── ExtremeDumper/                    # Main application (net481 + net8.0)
│   ├── Forms/                        # Windows Forms UI
│   ├── Diagnostics/                  # Process and module diagnostics
│   ├── Dumping/                      # Assembly dumping logic
│   └── ExtremeDumper.csproj          # Multi-target project file
│
├── ExtremeDumper-x86/                # 32-bit specific build (net481 + net8.0)
│   └── ExtremeDumper-x86.csproj      # x86 build configuration
│
├── ExtremeDumper.AntiAntiDump/       # Anti anti-dump module (net35 + net481 + net8.0)
│   ├── AADServer.cs                  # Named pipe server
│   ├── AADClient.cs                  # Named pipe client
│   └── ExtremeDumper.AntiAntiDump.csproj
│
├── ExtremeDumper.LoaderHook/         # C++ Native DLL injection module
│   └── ExtremeDumper.LoaderHook.vcxproj
│
├── build.bat                         # Batch build script (Windows)
├── build.ps1                         # PowerShell build script
├── ExtremeDumper.sln                 # Solution file
├── ExtremeDumper.Common.props        # Shared build properties
└── BUILD.md                          # This file
```

## Dependency Updates (v5.0)

| Package | Old Version | New Version | Notes |
|---------|-------------|-------------|-------|
| dnlib | 3.4.0 | **4.5.0** | Latest IL/PE manipulation |
| Microsoft.Diagnostics.Runtime | 1.1.142101 | **3.1.328701** | Improved runtime diagnostics |
| Costura.Fody | 4.1.0 | **5.2.0** | Better assembly embedding |
| Ookii.Dialogs.WinForms | 4.0.0 | **4.0.0** | Stable version |
| NativeSharp-lib | 3.0.0.1 | **3.0.0.1** | Unchanged |

## Features by Target

### .NET Framework 4.8.1

✅ Full compatibility with legacy .NET Framework ecosystem
✅ Maximum compatibility with older Windows versions
✅ Smaller binary size than .NET 8.0
✅ All features supported

### .NET 8.0

✅ Modern language features (C# 12)
✅ Performance improvements
✅ Better diagnostics tools integration
✅ Nullable reference types
✅ Records and pattern matching
✅ Reduced dependencies in some cases

## Troubleshooting

### Build fails with "MSBuild not found"

**Solution**: Install Visual Studio 2026 Community with C++ and .NET workloads, or install Build Tools separately.

### "dotnet" command not found

**Solution**: Install .NET 8.0 SDK from https://dotnet.microsoft.com/download/dotnet/8.0

### vcvarsall.bat not found

**Solution**: This is required for the C++ LoaderHook module. Install Visual Studio Build Tools with C++ workload.

### Build succeeds but no binaries in output

**Solution**: Check `bin\Release\` directory. The build system creates platform-specific subdirectories:

```
bin/Release/
  ExtremeDumper.exe          (main app)
  ExtremeDumper-x86.exe      (32-bit variant)
  ExtremeDumper.AntiAntiDump.dll
  [other dependencies]
```

### NuGet package restore fails

**Solution**: 

1. Check internet connection
2. Clear NuGet cache: `dotnet nuget locals all --clear`
3. Try manual restore: `dotnet restore`
4. Check proxy settings

### C++ Project (LoaderHook) fails to build

**Solution**: 

1. Ensure Build Tools C++ component is installed
2. Run `vcvarsall.bat x64` to initialize environment
3. Use x64 Native Tools Command Prompt from Visual Studio

## Performance Considerations

### Recommended for Production

- **.NET Framework 4.8.1 (x64)**: Best compatibility and performance
- **.NET 8.0 (x64)**: Modern runtime with latest optimizations

### Recommended for Legacy Systems

- **.NET Framework 4.8.1 (x86)**: For older Windows installations

## Build Artifacts

After successful build, check `bin\Release\`:

```
✓ ExtremeDumper.exe              - Main application
✓ ExtremeDumper-x86.exe          - 32-bit variant
✓ ExtremeDumper.AntiAntiDump.dll - Anti anti-dump module
✓ ExtremeDumper.LoaderHook.dll   - Loader hook module (32 & 64 bit)
✓ *.pdb                          - Debug symbols
```

## Clean Build

```cmd
# Batch script
build.bat /clean

# PowerShell
.\build.ps1 -Clean

# Manual
rmdir /s /q bin obj
build.bat
```

## CI/CD Integration

### GitHub Actions

See `.github\workflows\` for automated build examples.

### Azure Pipelines / AppVeyor

Update build configuration to use `build.bat` or `build.ps1`.

## Support & Contributing

- Report build issues: Include your system specs and full build output
- Fork and contribute improvements
- Test on multiple .NET versions

## License

Same as ExtremeDumper (Original by wwh1004, Modernized 2026)

---

**Last Updated**: 2026-05-26  
**Build System Version**: 5.0  
**Supported Frameworks**: .NET 4.8.1, .NET 8.0