# ==============================================================================
# Universal PVM Installer (Windows PowerShell / pwsh / cmd)
# Repository: https://github.com/hasanhawary/phpvm
# License: MIT
# ==============================================================================

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$script:C_RESET = "`e[0m"
$script:C_BOLD = "`e[1m"
$script:C_GREEN = "`e[0;32m"
$script:C_CYAN = "`e[0;36m"
$script:C_YELLOW = "`e[1;33m"
$script:C_RED = "`e[0;31m"

function Print-Info([string]$message)    { Write-Host ("{0}[INFO]{1} {2}" -f $script:C_CYAN, $script:C_RESET, $message) }
function Print-Success([string]$message) { Write-Host ("{0}[OK]{1}   {2}" -f $script:C_GREEN, $script:C_RESET, $message) }
function Print-Warn([string]$message)    { Write-Host ("{0}[WARN]{1} {2}" -f $script:C_YELLOW, $script:C_RESET, $message) }
function Print-Error([string]$message)   { [Console]::Error.WriteLine(("{0}[ERR]{1}  {2}" -f $script:C_RED, $script:C_RESET, $message)) }

$pvmHome = if ($env:PVM_HOME) { $env:PVM_HOME } else { [System.IO.Path]::Combine($env:USERPROFILE, ".pvm") }
$pvmBin = [System.IO.Path]::Combine($pvmHome, "bin")
$pvmVersions = [System.IO.Path]::Combine($pvmHome, "versions")
$pvmCurrent = [System.IO.Path]::Combine($pvmHome, "current")

Print-Info ("Installing PVM (PHP Version Manager) into {0}..." -f $pvmHome)

[void][System.IO.Directory]::CreateDirectory($pvmBin)
[void][System.IO.Directory]::CreateDirectory($pvmVersions)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$localPs1 = [System.IO.Path]::Combine($scriptDir, "pvm.ps1")
$localCmd = [System.IO.Path]::Combine($scriptDir, "pvm.cmd")
$localBash = [System.IO.Path]::Combine($scriptDir, "pvm")

if (Test-Path $localPs1) {
    Print-Info ("Copying local scripts from {0} to {1}..." -f $scriptDir, $pvmBin)
    $destPs1 = [System.IO.Path]::Combine($pvmBin, "pvm.ps1")
    $destCmd = [System.IO.Path]::Combine($pvmBin, "pvm.cmd")
    $destBash = [System.IO.Path]::Combine($pvmBin, "pvm")
    Copy-Item -Path $localPs1 -Destination $destPs1 -Force
    if (Test-Path $localCmd)  { Copy-Item -Path $localCmd -Destination $destCmd -Force }
    if (Test-Path $localBash) { Copy-Item -Path $localBash -Destination $destBash -Force }
} else {
    Print-Info "Downloading latest PVM scripts from GitHub..."
    $baseUrl = "https://raw.githubusercontent.com/hasanhawary/phpvm/main/shell"
    $destPs1 = [System.IO.Path]::Combine($pvmBin, "pvm.ps1")
    $destCmd = [System.IO.Path]::Combine($pvmBin, "pvm.cmd")
    $destBash = [System.IO.Path]::Combine($pvmBin, "pvm")
    Invoke-WebRequest -Uri "$baseUrl/pvm.ps1" -OutFile $destPs1
    try { Invoke-WebRequest -Uri "$baseUrl/pvm.cmd" -OutFile $destCmd } catch {}
    try { Invoke-WebRequest -Uri "$baseUrl/pvm" -OutFile $destBash } catch {}
}

# Clean any legacy .NET executables so shell scripts take precedence in PATH
try {
    Get-ChildItem -Path $pvmBin -Filter "pvm*.exe*" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
} catch {}

Print-Success ("PVM scripts installed to {0}" -f $pvmBin)

# Configure User Environment PATH
Print-Info "Auditing and updating User Environment PATH..."
try {
    $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    if (-not $userPath) { $userPath = "" }
    $parts = $userPath -split ";" | Where-Object { $_ -ne "" }

    $missingCurrent = -not ($parts -contains $pvmCurrent)
    $missingBin = -not ($parts -contains $pvmBin)

    if ($missingCurrent -or $missingBin) {
        $newParts = @()
        if ($missingCurrent) { $newParts += $pvmCurrent }
        if ($missingBin) { $newParts += $pvmBin }
        $newParts += $parts

        $updatedUserPath = $newParts -join ";"
        [Environment]::SetEnvironmentVariable("PATH", $updatedUserPath, "User")
        Print-Success "Updated User PATH environment variable permanently."

        # Broadcast WM_SETTINGCHANGE
        try {
            if (-not ("Win32.NativeMethods" -as [type])) {
                Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace Win32 {
    public class NativeMethods {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
    }
}
'@
            }
            $HWND_BROADCAST = [IntPtr]0xffff
            $WM_SETTINGCHANGE = 0x001A
            $SMTO_ABORTIFHUNG = 0x0002
            $result = [UIntPtr]::Zero
            [Win32.NativeMethods]::SendMessageTimeout($HWND_BROADCAST, $WM_SETTINGCHANGE, [UIntPtr]::Zero, "Environment", $SMTO_ABORTIFHUNG, 5000, [ref]$result) | Out-Null
            Print-Success "Broadcasted WM_SETTINGCHANGE to notify system of new environment variables."
        } catch { }
    } else {
        Print-Success "User PATH already contains PVM paths."
    }
} catch {
    Print-Warn ("Could not update registry User PATH automatically ({0})." -f $_.Exception.Message)
}

# Update current session PATH right now
$sessionParts = $env:PATH -split ";" | Where-Object { $_ -ne "" }
if (-not ($sessionParts -contains $pvmCurrent)) { $env:PATH = "$pvmCurrent;$env:PATH" }
if (-not ($sessionParts -contains $pvmBin)) { $env:PATH = "$pvmBin;$env:PATH" }

Print-Success "Current terminal session PATH updated instantly!"

Write-Host ("`n{0}=============================================================================={1}" -f $script:C_BOLD, $script:C_RESET)
Write-Host ("{0}[OK] PVM Installation Complete!{1}" -f $script:C_GREEN, $script:C_RESET)
Write-Host ("{0}=============================================================================={1}`n" -f $script:C_BOLD, $script:C_RESET)
Write-Host ("To test your installation right now, run:" -f $script:C_RESET)
Write-Host ("  {0}pvm --version{1}" -f $script:C_BOLD, $script:C_RESET)
Write-Host ("  {0}pvm list --remote{1}  (View available PHP versions to install)" -f $script:C_CYAN, $script:C_RESET)
Write-Host ("  {0}pvm install 8.4{1}    (Install PHP 8.4)" -f $script:C_CYAN, $script:C_RESET)
Write-Host ("  {0}pvm use 8.4{1}        (Switch active PHP runtime globally)`n" -f $script:C_CYAN, $script:C_RESET)
