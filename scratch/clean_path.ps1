$ErrorActionPreference = "Stop"

Write-Host "Checking Machine (System) PATH..." -ForegroundColor Cyan
try {
    $mPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($mPath -like "*\PHP\PHP*") {
        $cleanM = ($mPath.Split(';') | Where-Object { $_ -and $_.Trim() -ne "" -and $_ -notlike "*\PHP\PHP*" }) -join ';'
        [Environment]::SetEnvironmentVariable("PATH", $cleanM, "Machine")
        Write-Host "Removed external PHP from Machine PATH!" -ForegroundColor Green
    } else {
        Write-Host "Machine PATH has no external PHP conflicts." -ForegroundColor Green
    }
} catch {
    Write-Warning "Could not modify Machine PATH (requires administrator privileges): $_"
}

Write-Host "`nChecking User PATH..." -ForegroundColor Cyan
$uPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($uPath -like "*\PHP\PHP*") {
    $cleanU = ($uPath.Split(';') | Where-Object { $_ -and $_.Trim() -ne "" -and $_ -notlike "*\PHP\PHP*" }) -join ';'
    [Environment]::SetEnvironmentVariable("PATH", $cleanU, "User")
    Write-Host "Removed external PHP from User PATH!" -ForegroundColor Green
} else {
    Write-Host "User PATH has no external PHP conflicts." -ForegroundColor Green
}

# Ensure PVM current is at position 1 in User PATH right after .pvm\bin
$installDir = "$HOME\.pvm\bin"
$currentDir = "$HOME\.pvm\current"
$userEntries = [Environment]::GetEnvironmentVariable("PATH", "User").Split(';') | Where-Object { 
    $_ -and $_.Trim() -ne "" -and 
    $_.Trim().TrimEnd('\') -ne $installDir.TrimEnd('\') -and 
    $_.Trim().TrimEnd('\') -ne $currentDir.TrimEnd('\') 
}

$newUserPath = "$installDir;$currentDir;" + ($userEntries -join ';')
[Environment]::SetEnvironmentVariable("PATH", $newUserPath, "User")
Write-Host "`nSuccessfully prioritized PVM paths at the very beginning of User PATH!" -ForegroundColor Green

# Broadcast WM_SETTINGCHANGE
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
