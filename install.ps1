<#
.SYNOPSIS
    One-line installer for PVM (PHP Version Manager for Windows).
.DESCRIPTION
    Downloads the latest self-contained standalone executable (pvm.exe) from GitHub Releases,
    installs it into %USERPROFILE%\.pvm\bin, and registers it in the Windows User PATH.
.EXAMPLE
    irm https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.ps1 | iex
#>
param(
    [string]$InstallDir = "$env:USERPROFILE\.pvm\bin",
    [string]$Repo = "hasanhawary/phpvm",
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "     PVM (PHP Version Manager) One-Line Installer       " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

# 1. Prepare installation directory
if (-not (Test-Path $InstallDir)) {
    Write-Host "`n[1/4] Creating installation directory ($InstallDir)..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
} else {
    Write-Host "`n[1/4] Using installation directory: $InstallDir" -ForegroundColor Yellow
}

# 2. Determine download URL
Write-Host "`n[2/4] Fetching release information from GitHub ($Repo)..." -ForegroundColor Yellow
$zipUrl = ""
if ($Tag -eq "latest") {
    try {
        $apiUrl = "https://api.github.com/repos/$Repo/releases/latest"
        $response = Invoke-RestMethod -Uri $apiUrl -UseBasicParsing
        $asset = $response.assets | Where-Object { $_.name -ieq "pvm-win-x64.zip" }
        if ($asset) {
            $zipUrl = $asset.browser_download_url
            Write-Host "Found latest release: $($response.tag_name)" -ForegroundColor Green
        }
    } catch {
        Write-Warning "Failed to fetch release API: $_"
    }
} else {
    $zipUrl = "https://github.com/$Repo/releases/download/$Tag/pvm-win-x64.zip"
}

# Fallback: if release not published online or running locally during development
if (-not $zipUrl -or $zipUrl -eq "") {
    $localDist = Join-Path $PSScriptRoot "dist\pvm.exe"
    if (Test-Path $localDist) {
        Write-Host "Notice: Online release asset not found. Using locally compiled binary ($localDist)..." -ForegroundColor DarkYellow
        Copy-Item $localDist (Join-Path $InstallDir "pvm.exe") -Force
    } else {
        Write-Error "Could not find pvm-win-x64.zip on GitHub Releases and no local build exists in ./dist/pvm.exe. Please run .\build.ps1 first."
    }
} else {
    # 3. Download and extract
    Write-Host "`n[3/4] Downloading PVM standalone binary ($zipUrl)..." -ForegroundColor Yellow
    $tempZip = Join-Path $env:TEMP "pvm-win-x64.zip"
    Invoke-WebRequest -Uri $zipUrl -OutFile $tempZip -UseBasicParsing
    
    Write-Host "Extracting archive to $InstallDir..." -ForegroundColor Yellow
    Expand-Archive -Path $tempZip -DestinationPath $InstallDir -Force
    if (Test-Path $tempZip) { Remove-Item $tempZip -Force }
}

# 4. Register in User Environment PATH
Write-Host "`n[4/4] Ensuring PVM is registered in your User Environment PATH..." -ForegroundColor Yellow
$userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($userPath -notlike "*$InstallDir*") {
    $newPath = "$InstallDir;$userPath"
    [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
    Write-Host "Added $InstallDir permanently to your User PATH!" -ForegroundColor Green
} else {
    Write-Host "$InstallDir is already in your User PATH." -ForegroundColor Green
}

# Update current session PATH for immediate use
$env:PATH = "$InstallDir;$env:PATH"

$exePath = Join-Path $InstallDir "pvm.exe"
if (Test-Path $exePath) {
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host " INSTALLATION SUCCESSFUL! 🎉" -ForegroundColor Green
    Write-Host " PVM installed to: $exePath" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "`nQuick Start: Open a new terminal window and type 'pvm --help' or 'pvm list --remote'." -ForegroundColor Cyan
} else {
    Write-Error "Installation completed but pvm.exe was not found in $InstallDir."
}
