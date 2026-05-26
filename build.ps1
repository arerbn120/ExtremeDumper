# ============================================================================
# ExtremeDumper Build Script v5.0 (PowerShell)
# Multi-Target Build: .NET Framework 4.8.1 & .NET 8.0
# Visual Studio 2026 Community & BuildTools Integration
# ============================================================================

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [ValidateSet('net481', 'net8.0-windows', 'all')]
    [string]$Framework = 'all',
    
    [ValidateSet('x86', 'x64', 'both')]
    [string]$Architecture = 'both',
    
    [switch]$Clean,
    [switch]$Help
)

# ============================================================================
# Functions
# ============================================================================
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Show-Help {
    @"
ExtremeDumper Build Script v5.0

Usage: .\build.ps1 [Options]

Options:
  -Configuration {Debug|Release}      Build configuration (default: Release)
  -Framework {net481|net8.0-windows|all}  Target framework (default: all)
  -Architecture {x86|x64|both}        Target architecture (default: both)
  -Clean                              Clean build output before building
  -Help                               Show this help message

Examples:
  .\build.ps1                         # Build all targets (Release)
  .\build.ps1 -Configuration Debug    # Build with Debug configuration
  .\build.ps1 -Framework net481 -Architecture x86  # Build .NET 4.8.1 x86 only
  .\build.ps1 -Clean                  # Clean and build
"@
}

# ============================================================================
# Main Script
# ============================================================================
if ($Help) {
    Show-Help
    exit 0
}

Write-Host "`n" -NoNewline
Write-Host "==================================================================================" -ForegroundColor Magenta
Write-Host "ExtremeDumper Build System v5.0" -ForegroundColor Magenta
Write-Host "==================================================================================" -ForegroundColor Magenta
Write-Host "`n" -NoNewline

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

$VSCommunity = "C:\Program Files\Microsoft Visual Studio\18\Community"
$VSBuildTools = "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools"
$VCVarsPath = Join-Path $VSBuildTools "VC\Auxiliary\Build\vcvarsall.bat"
$Solution = "ExtremeDumper.sln"
$OutputDir = "bin\$Configuration"

# Validate environment
Write-Info "Validating environment..."

if (-not (Test-Path $Solution)) {
    Write-Error "Solution file not found: $Solution"
    exit 1
}

if (Test-Path $VSCommunity) {
    Write-Success "VS 2026 Community found"
    $MSBuild = Join-Path $VSCommunity "MSBuild\Current\Bin\MSBuild.exe"
} else {
    Write-Warning "VS 2026 Community not found, using dotnet CLI"
}

if (Test-Path $VCVarsPath) {
    Write-Success "VC++ Build Tools found"
} else {
    Write-Warning "VC++ Build Tools not found"
}

# Check .NET SDKs
Write-Info "Checking .NET SDKs..."
$sdks = dotnet --list-sdks 2>$null
if ($sdks) {
    $sdks | ForEach-Object { Write-Info "SDK: $_" }
} else {
    Write-Warning "No .NET SDKs found or dotnet CLI not available"
}

# Clean if requested
if ($Clean -and (Test-Path $OutputDir)) {
    Write-Info "Cleaning output directory..."
    Remove-Item $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Success "Output directory cleaned"
}

# Restore NuGet packages
Write-Info "Restoring NuGet packages..."
dotnet restore $Solution
if ($LASTEXITCODE -ne 0) {
    Write-Error "NuGet restore failed"
    exit 1
}
Write-Success "NuGet packages restored"

# Build targets
$Frameworks = if ($Framework -eq 'all') { @('net481', 'net8.0-windows') } else { @($Framework) }
$Architectures = if ($Architecture -eq 'both') { @('x86', 'x64') } else { @($Architecture) }

foreach ($fw in $Frameworks) {
    foreach ($arch in $Architectures) {
        Write-Host "`n" -NoNewline
        Write-Host "==================================================================================" -ForegroundColor Green
        Write-Host "Building: $fw - $arch" -ForegroundColor Green
        Write-Host "==================================================================================" -ForegroundColor Green
        Write-Host "`n" -NoNewline
        
        if ($fw -eq 'net481') {
            $platform = if ($arch -eq 'x86') { 'Win32' } else { 'x64' }
            Write-Info "Using MSBuild for .NET Framework..."
            
            if (Test-Path $MSBuild) {
                & $MSBuild $Solution /p:Configuration=$Configuration /p:Platform=$platform /p:TargetFramework=$fw /m /verbosity:normal /nologo
            } else {
                Write-Error "MSBuild not found. Skipping .NET Framework build."
            }
        } else {
            Write-Info "Using dotnet CLI for .NET..."
            dotnet build $Solution --configuration $Configuration --no-restore --framework $fw
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build completed for $fw - $arch"
        } else {
            Write-Error "Build failed for $fw - $arch"
        }
    }
}

# Summary
Write-Host "`n" -NoNewline
Write-Host "==================================================================================" -ForegroundColor Magenta
Write-Host "Build Summary" -ForegroundColor Magenta
Write-Host "==================================================================================" -ForegroundColor Magenta
Write-Host "`n" -NoNewline

Write-Info "Output directory: $OutputDir"

if (Test-Path $OutputDir) {
    Write-Success "Build artifacts found:"
    Get-ChildItem $OutputDir | Select-Object -ExpandProperty Name | ForEach-Object { Write-Host "  - $_" }
    Write-Success "Build process completed successfully!"
} else {
    Write-Warning "Output directory not created"
}

Write-Host "`n" -NoNewline