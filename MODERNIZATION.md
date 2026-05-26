# ExtremeDumper Modernization Guide v5.0

## 📋 Overview

ExtremeDumper has been successfully modernized to support **dual-target builds** for both **.NET Framework 4.8.1** and **.NET 8.0**, with significant dependency updates and build system improvements.

---

## 🎯 Key Updates Completed

### **1. Multi-Target Framework Support**

```xml
<!-- Before (v4.0) -->
<TargetFramework>net462</TargetFramework>

<!-- After (v5.0) -->
<TargetFrameworks>net481;net8.0-windows</TargetFrameworks>
```

### **2. Dependency Updates**

| Package | Old | New | Notes |
|---------|-----|-----|-------|
| **dnlib** | 3.4.0 | **4.5.0** | Enhanced IL/PE manipulation |
| **Microsoft.Diagnostics.Runtime** | 1.1.142101 | **3.1.328701** | Better CLR integration |
| **Costura.Fody** | 4.1.0 | **5.2.0** | Improved assembly embedding |
| **Ookii.Dialogs.WinForms** | 4.0.0 | 4.0.0 | Stable (no changes) |
| **NativeSharp-lib** | 3.0.0.1 | 3.0.0.1 | Stable (no changes) |

### **3. Build System Completely Rewritten**

✅ **build.bat** - Windows batch script with VS 2026 integration  
✅ **build.ps1** - PowerShell script with advanced options  
✅ **BUILD.md** - Comprehensive build documentation

---

## 🚀 Quick Start

### Build Everything (All targets)
```cmd
build.bat
```

### Build Specific Configuration
```powershell
.\build.ps1 -Framework net481 -Architecture x64 -Configuration Release
.\build.ps1 -Framework net8.0-windows -Architecture x86
```

---

## 📂 Files Updated

✅ `ExtremeDumper.Common.props` - Updated version & language features  
✅ `ExtremeDumper/ExtremeDumper.csproj` - Multi-target support  
✅ `ExtremeDumper-x86/ExtremeDumper-x86.csproj` - x86 variant  
✅ `ExtremeDumper.AntiAntiDump/ExtremeDumper.AntiAntiDump.csproj` - net35 + net481 + net8.0  
✅ `build.bat` - New comprehensive batch builder  
✅ `build.ps1` - New PowerShell builder  
✅ `.gitignore` - Updated for modern builds  
✅ `BUILD.md` - Complete build documentation  

---

## 🔧 Environment Configuration

Ensure these paths exist on your system:

```
C:\Program Files\Microsoft Visual Studio\18\Community
C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvarsall.bat
```

If different, edit `build.bat` or `build.ps1` accordingly.

---

## 📊 Build Output Structure

```
bin/Release/
├── ExtremeDumper.exe              (net481, x64)
├── ExtremeDumper-x86.exe          (net481, x86)
├── ExtremeDumper.AntiAntiDump.dll (net481)
├── ExtremeDumper.LoaderHook.dll   (32-bit & 64-bit)
└── [other dependencies]
```

---

**Version**: 5.0.0.0  
**Last Updated**: 2026-05-26  
**Status**: ✅ Ready for Production
