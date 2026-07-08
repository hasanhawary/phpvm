# PVM (PHP Version Manager) One-Line PowerShell Uninstaller
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
#   irm https://raw.githubusercontent.com/hasanhawary/phpvm/main/uninstall.ps1 | iex

$ErrorActionPreference = "Continue"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "       PVM (PHP Version Manager) Uninstaller            " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$InstallDir = "$Home\.pvm\bin"
$PvmRoot = "$Home\.pvm"

# 1. Clean Windows User PATH
Write-Host "`n[1/3] Removing PVM from Windows User PATH..." -ForegroundColor Yellow
try {
    $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($userPath) {
        $paths = $userPath.Split(';') | Where-Object { 
            $_ -and $_.Trim() -ne "" -and 
            $_.Trim().TrimEnd('\') -ne $InstallDir.TrimEnd('\') -and 
            $_.Trim().TrimEnd('\') -ne "$PvmRoot\current".TrimEnd('\') 
        }
        $newPath = $paths -join ';'
        [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
        $env:PATH = $newPath
        Write-Host "Cleaned User PATH successfully." -ForegroundColor Green

        # Broadcast setting change so open terminal windows see the change after restart
        try {
            $code = @"
using System;
using System.Runtime.InteropServices;
public class PvmBroadcast {
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
}
"@
            Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue | Out-Null
            $result = [IntPtr]::Zero
            [PvmBroadcast]::SendMessageTimeout([IntPtr]0xffff, 0x001A, [IntPtr]::Zero, "Environment", 2, 5000, [ref]$result) | Out-Null
        } catch { }
    }
} catch {
    Write-Warning "Could not modify User PATH: $_"
}

# 2. Terminate any running pvm or php instances originating from .pvm
Write-Host "`n[2/3] Checking for active PVM processes..." -ForegroundColor Yellow
try {
    Get-Process -Name "pvm", "php" -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*\.pvm\*" } | ForEach-Object {
        Write-Host "Stopping process $($_.ProcessName) (PID: $($_.Id))..." -ForegroundColor DarkYellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
} catch { }

# 3. Remove .pvm directory
Write-Host "`n[3/3] Removing directory ($PvmRoot)..." -ForegroundColor Yellow
if (Test-Path $PvmRoot) {
    try {
        Remove-Item -Path $PvmRoot -Recurse -Force -ErrorAction Stop
        Write-Host "Removed $PvmRoot successfully." -ForegroundColor Green
    } catch {
        # If pvm.exe itself is running this script or locked, schedule delayed removal via cmd
        Write-Host "Notice: File lock encountered. Scheduling background cleanup..." -ForegroundColor DarkYellow
        Start-Process -FilePath "cmd.exe" -ArgumentList "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q `"$PvmRoot`"" -WindowStyle Hidden
    }
} else {
    Write-Host "$PvmRoot is already removed." -ForegroundColor Green
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host " UNINSTALLATION SUCCESSFUL! PVM has been removed.      " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Cyan
