<#
.SYNOPSIS
    Builds, tests, and publishes the standalone PVM (PHP Version Manager) executable.
.DESCRIPTION
    Runs .NET restore, build, tests across all 5 test projects, and publishes a single self-contained,
    trimmed Windows x64 executable to the ./dist directory.
.PARAMETER Clean
    Cleans previous build artifacts before compiling.
.PARAMETER SkipTests
    Skips running unit tests during build.
.EXAMPLE
    .\build.ps1
.EXAMPLE
    .\build.ps1 -Clean
#>
param(
    [switch]$Clean,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$distDir = Join-Path $root "dist"

if (Test-Path "$env:LocalAppData\Microsoft\dotnet") {
    $env:DOTNET_ROOT = "$env:LocalAppData\Microsoft\dotnet"
    $env:DOTNET_ROOT_x64 = "$env:LocalAppData\Microsoft\dotnet"
    $env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = "$env:LocalAppData\Microsoft\dotnet\sdk\8.0.422\Sdks"
    $env:MSBuildSDKsPath = "$env:LocalAppData\Microsoft\dotnet\sdk\8.0.422\Sdks"
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       PVM (PHP Version Manager) Build Script           " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

if ($Clean) {
    Write-Host "`n[1/4] Cleaning previous builds and dist directory..." -ForegroundColor Yellow
    if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
    dotnet clean $root/pvm.sln -c Release -v q
} else {
    Write-Host "`n[1/4] Preparing dist directory..." -ForegroundColor Yellow
    if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }
}

Write-Host "`n[2/4] Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $root/pvm.sln

Write-Host "`n[3/4] Building solution in Release mode..." -ForegroundColor Yellow
dotnet build $root/pvm.sln -c Release --no-restore

if (-not $SkipTests) {
    Write-Host "`n[4/4] Running unit test suite across all 5 test projects..." -ForegroundColor Yellow
    dotnet test $root/pvm.sln -c Release --verbosity normal
} else {
    Write-Host "`n[4/4] Skipping unit tests..." -ForegroundColor DarkYellow
}

Write-Host "`n[Publish] Publishing standalone self-contained win-x64 executable..." -ForegroundColor Green
dotnet publish (Join-Path $root "src\Pvm.Cli\Pvm.Cli.csproj") -c Release -r win-x64 -o $distDir

$exePath = Join-Path $distDir "pvm.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host " BUILD SUCCESSFUL! 🎉" -ForegroundColor Green
    Write-Host " Standalone Executable: $exePath ($sizeMB MB)" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    
    Write-Host "`nVerifying executable version and help output:" -ForegroundColor Cyan
    & $exePath --help

    $pvmBin = "$env:USERPROFILE\.pvm\bin"
    if (-not (Test-Path $pvmBin)) { New-Item -ItemType Directory -Path $pvmBin | Out-Null }
    Copy-Item $exePath "$pvmBin\pvm.exe" -Force
    $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($userPath -notlike "*$pvmBin*") {
        [Environment]::SetEnvironmentVariable("PATH", "$pvmBin;$userPath", "User")
        Write-Host "`nRegistered $pvmBin globally in User PATH!" -ForegroundColor Green
    }
    Write-Host "`npvm is installed and ready to use globally across your system!" -ForegroundColor Green
} else {
    Write-Error "Build failed: pvm.exe not found in $distDir"
}
