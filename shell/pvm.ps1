# ==============================================================================
# PVM - Universal PHP Version Manager (PowerShell Edition)
# Version: 1.1.0-shell
# Repository: https://github.com/hasanhawary/phpvm
# License: MIT
# ==============================================================================

[CmdletBinding()]
param(
    [Parameter(Position=0)]
    [string]$Command,

    [Parameter(Position=1, ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$ErrorActionPreference = "Stop"

# --- Configuration & Paths ---
$script:PVM_VERSION = "1.1.0-shell"
$script:PVM_HOME = if ($env:PVM_HOME) { $env:PVM_HOME } else { [System.IO.Path]::Combine($env:USERPROFILE, ".pvm") }
$script:PVM_VERSIONS_DIR = [System.IO.Path]::Combine($script:PVM_HOME, "versions")
$script:PVM_CURRENT_DIR = [System.IO.Path]::Combine($script:PVM_HOME, "current")
$script:PVM_BIN_DIR = [System.IO.Path]::Combine($script:PVM_HOME, "bin")
$script:PVM_ALIASES_FILE = [System.IO.Path]::Combine($script:PVM_HOME, "aliases.json")
$script:RELEASES_JSON_URL = "https://windows.php.net/downloads/releases/releases.json"
$script:BASE_DOWNLOAD_URL = "https://windows.php.net/downloads/releases/"
$script:GITHUB_API_LATEST = "https://api.github.com/repos/hasanhawary/phpvm/releases/latest"

# --- ANSI Colors & Styles ---
$script:C_RESET = "`e[0m"
$script:C_BOLD = "`e[1m"
$script:C_RED = "`e[0;31m"
$script:C_GREEN = "`e[0;32m"
$script:C_YELLOW = "`e[1;33m"
$script:C_CYAN = "`e[0;36m"

function Print-Info([string]$message)    { Write-Host ("{0}[INFO]{1} {2}" -f $script:C_CYAN, $script:C_RESET, $message) }
function Print-Success([string]$message) { Write-Host ("{0}[OK]{1}   {2}" -f $script:C_GREEN, $script:C_RESET, $message) }
function Print-Warn([string]$message)    { Write-Host ("{0}[WARN]{1} {2}" -f $script:C_YELLOW, $script:C_RESET, $message) }
function Print-Error([string]$message)   { [Console]::Error.WriteLine(("{0}[ERR]{1}  {2}" -f $script:C_RED, $script:C_RESET, $message)) }

function Ensure-Directories {
    [void][System.IO.Directory]::CreateDirectory($script:PVM_VERSIONS_DIR)
    [void][System.IO.Directory]::CreateDirectory($script:PVM_BIN_DIR)
    if (-not (Test-Path $script:PVM_ALIASES_FILE) -or (Get-Item $script:PVM_ALIASES_FILE).Length -eq 0) {
        "{}" | Out-File -FilePath $script:PVM_ALIASES_FILE -Encoding utf8
    }
}

function Get-AliasesDict {
    if (-not (Test-Path $script:PVM_ALIASES_FILE)) { return @{} }
    try {
        $content = Get-Content -Raw -Path $script:PVM_ALIASES_FILE -ErrorAction SilentlyContinue
        if ([string]::IsNullOrWhiteSpace($content)) { return @{} }
        $obj = $content | ConvertFrom-Json
        if ($null -eq $obj) { return @{} }
        if ($obj -is [Hashtable]) { return $obj }
        $dict = @{}
        foreach ($prop in $obj.PSObject.Properties) {
            $dict[$prop.Name] = [string]$prop.Value
        }
        return $dict
    } catch {
        return @{}
    }
}

function Save-AliasesDict([Hashtable]$dict) {
    Ensure-Directories
    if ($null -eq $dict -or $dict.Count -eq 0) {
        "{}" | Out-File -FilePath $script:PVM_ALIASES_FILE -Encoding utf8
        return
    }
    $json = $dict | ConvertTo-Json -Depth 5
    if ([string]::IsNullOrWhiteSpace($json)) { $json = "{}" }
    $json | Out-File -FilePath $script:PVM_ALIASES_FILE -Encoding utf8
}

function Resolve-Alias([string]$spec) {
    $dict = Get-AliasesDict
    if ($dict.ContainsKey($spec)) { return $dict[$spec] }
    return $spec
}

function Get-ActiveVersion {
    if (-not (Test-Path $script:PVM_CURRENT_DIR)) { return $null }
    try {
        $info = Get-Item -Path $script:PVM_CURRENT_DIR -Force
        if ($info.Attributes -match "ReparsePoint") {
            if ($info.Target) {
                return Split-Path -Leaf $info.Target
            }
        }
        $dirOut = cmd /c "dir /al `"$script:PVM_HOME`"" 2>$null | Select-String -Pattern "current \["
        if ($dirOut -match "\[(.*)\]") {
            return Split-Path -Leaf $Matches[1]
        }
    } catch { }
    return $null
}

function Render-TableHeader([string]$title) {
    Write-Host ("{0}+-----------------------------------------------------------------------------+{1}" -f $script:C_CYAN, $script:C_RESET)
    Write-Host ("{0}| {1}{2}{3}{0} |{3}" -f $script:C_CYAN, $script:C_BOLD, (-join ($title.PadRight(75).Substring(0, 75))), $script:C_RESET)
    Write-Host ("{0}+--------+-----------------+------+---------------+---------------------------+{1}" -f $script:C_CYAN, $script:C_RESET)
    Write-Host ("{0}| {1}{2}{7}{0} | {1}{3}{7}{0} | {1}{4}{7}{0} | {1}{5}{7}{0} | {1}{6}{7}{0} |{7}" -f $script:C_CYAN, $script:C_BOLD, $("Active".PadRight(6)), $("Version".PadRight(15)), $("Arch".PadRight(4)), $("Thread Safety".PadRight(13)), $("Path".PadRight(25)), $script:C_RESET)
    Write-Host ("{0}+--------+-----------------+------+---------------+---------------------------+{1}" -f $script:C_CYAN, $script:C_RESET)
}

function Render-TableRow([string]$active, [string]$ver, [string]$arch, [string]$ts, [string]$path) {
    $actStr = "      "
    if ($active -eq "true" -or $active -eq "*") {
        $actStr = ("{0}  *   {1}" -f $script:C_GREEN, $script:C_RESET)
    }
    if ($path.Length -gt 25) {
        $path = "..." + $path.Substring($path.Length - 22)
    } else {
        $path = $path.PadRight(25)
    }
    Write-Host ("{0}|{1} {2} {0}|{1} {3} {0}|{1} {4} {0}|{1} {5} {0}|{1} {6} {0}|{1}" -f $script:C_CYAN, $script:C_RESET, $actStr, $($ver.PadRight(15)), $($arch.PadRight(4)), $($ts.PadRight(13)), $path)
}

function Render-TableFooter {
    Write-Host ("{0}+--------+-----------------+------+---------------+---------------------------+{1}" -f $script:C_CYAN, $script:C_RESET)
}

# ==============================================================================
# Commands
# ==============================================================================
function Cmd-List {
    param([bool]$Remote = $false)
    Ensure-Directories
    $activeVer = Get-ActiveVersion

    if ($Remote) {
        Print-Info "Fetching available PHP builds from official windows.php.net mirrors..."
        try {
            $response = Invoke-RestMethod -Uri $script:RELEASES_JSON_URL -Method Get
        } catch {
            Print-Error "Failed to fetch release catalog from windows.php.net."
            return
        }

        Render-TableHeader "Available Remote PHP Versions (windows.php.net)"
        $remoteVersions = @()
        foreach ($prop in $response.PSObject.Properties) {
            $val = $prop.Value
            if ($val -and $val.version) {
                foreach ($subProp in $val.PSObject.Properties) {
                    $key = $subProp.Name
                    if ($key -match '^(ts|nts)-.*-x64$') {
                        $ts = if ($key -like "ts-*") { "TS" } else { "NTS" }
                        $remoteVersions += [PSCustomObject]@{
                            Version = $val.version
                            TS = $ts
                        }
                    }
                }
            }
        }
        $remoteVersions | Sort-Object { [version]($_.Version -replace '-.*','') } -Descending | Select-Object -First 30 | ForEach-Object {
            $isActive = if ($_.Version -eq $activeVer) { "true" } else { "false" }
            Render-TableRow $isActive $_.Version "x64" $_.TS "windows.php.net"
        }
        Render-TableFooter
        return
    }

    $installed = @()
    if (Test-Path $script:PVM_VERSIONS_DIR) {
        Get-ChildItem -Path $script:PVM_VERSIONS_DIR -Directory | ForEach-Object {
            if (Test-Path (Join-Path $_.FullName "php.exe")) {
                $ver = $_.Name
                $ts = if ($ver -like "*-nts*") { "NTS" } else { "TS (Default)" }
                $isActive = if ($ver -eq $activeVer) { "true" } else { "false" }
                $installed += [PSCustomObject]@{
                    Active = $isActive
                    Version = $ver
                    TS = $ts
                    Path = $_.FullName
                }
            }
        }
    }

    if ($installed.Count -eq 0) {
        Print-Warn "No PHP versions installed yet. Run 'pvm list --remote' to view remote releases, or 'pvm install <version>' to install."
    } else {
        Render-TableHeader ("Installed PHP Versions ({0})" -f $script:PVM_VERSIONS_DIR)
        foreach ($item in $installed) {
            Render-TableRow $item.Active $item.Version "x64" $item.TS $item.Path
        }
        Render-TableFooter
    }
}

function Cmd-Current {
    $activeVer = Get-ActiveVersion
    $exe = Join-Path $script:PVM_CURRENT_DIR "php.exe"
    if (-not $activeVer -or -not (Test-Path $exe)) {
        Print-Warn "No active PHP version is set across PVM."
        Print-Info "Run 'pvm use <version>' to switch or set the active system PHP runtime."
        return
    }

    Write-Host ("`n{0}Active PVM Runtime Status:{1}" -f $script:C_BOLD, $script:C_RESET)
    Write-Host ("  * {0}Version      :{1} {2}" -f $script:C_CYAN, $script:C_RESET, $activeVer)
    Write-Host ("  * {0}Junction Link:{1} {2}" -f $script:C_CYAN, $script:C_RESET, $script:PVM_CURRENT_DIR)
    Write-Host ("  * {0}Binary Path  :{1} {2}" -f $script:C_CYAN, $script:C_RESET, $exe)
    Write-Host ("`n{0}PHP CLI Output ({2} -v):{1}" -f $script:C_BOLD, $script:C_RESET, $exe)
    & $exe -v 2>&1 | ForEach-Object { Write-Host "  $_" }
    Write-Host ""
}

function Cmd-Use([string]$spec) {
    if (-not $spec) {
        Print-Error "Please specify a PHP version or alias to use (e.g., 'pvm use 8.4')."
        return
    }
    Ensure-Directories
    $targetVer = Resolve-Alias $spec

    $matchedDir = $null
    $matchedVer = $null
    $exactPath = Join-Path $script:PVM_VERSIONS_DIR $targetVer
    if (Test-Path $exactPath) {
        $matchedDir = $exactPath
        $matchedVer = $targetVer
    } else {
        $dirs = Get-ChildItem -Path $script:PVM_VERSIONS_DIR -Directory | Where-Object { $_.Name -like "$targetVer*" } | Sort-Object Name -Descending
        if ($dirs -and $dirs.Count -gt 0) {
            $matchedDir = $dirs[0].FullName
            $matchedVer = $dirs[0].Name
        }
    }

    if (-not $matchedDir -or -not (Test-Path (Join-Path $matchedDir "php.exe"))) {
        Print-Error ("PHP version matching '{0}' ({1}) is not installed in {2}." -f $spec, $targetVer, $script:PVM_VERSIONS_DIR)
        Print-Info ("Run 'pvm install {0}' first to install it." -f $spec)
        return
    }

    Print-Info ("Switching active PHP version to {0}{1}{2}..." -f $script:C_BOLD, $matchedVer, $script:C_RESET)
    if (Test-Path $script:PVM_CURRENT_DIR) {
        cmd /c "rmdir `"$script:PVM_CURRENT_DIR`" 2>nul" | Out-Null
        if (Test-Path $script:PVM_CURRENT_DIR) {
            Remove-Item -Path $script:PVM_CURRENT_DIR -Force -Recurse -ErrorAction SilentlyContinue
        }
    }

    cmd /c "mklink /J `"$script:PVM_CURRENT_DIR`" `"$matchedDir`"" 2>&1 | Out-Null
    if (-not (Test-Path $script:PVM_CURRENT_DIR)) {
        New-Item -ItemType SymbolicLink -Path $script:PVM_CURRENT_DIR -Target $matchedDir -Force | Out-Null
    }

    $env:PATH = "$script:PVM_CURRENT_DIR;$script:PVM_BIN_DIR;" + ($env:PATH -replace [regex]::Escape("$script:PVM_CURRENT_DIR;"), "")
    Print-Success ("Successfully switched to PHP {0}{1}{2}!" -f $script:C_BOLD, $matchedVer, $script:C_RESET)
}

function Cmd-Install([string]$spec, [bool]$Force = $false) {
    if (-not $spec) {
        Print-Error "Please specify a PHP version to install (e.g., 'pvm install 8.4')."
        return
    }
    Ensure-Directories
    $targetVer = Resolve-Alias $spec

    Print-Info ("Resolving remote download catalog for PHP '{0}'..." -f $targetVer)
    try {
        $response = Invoke-RestMethod -Uri $script:RELEASES_JSON_URL -Method Get
    } catch {
        Print-Error "Failed to fetch release catalog from windows.php.net."
        return
    }

    $resolvedVer = $null
    $downloadPath = $null
    $sha256 = $null

    foreach ($prop in $response.PSObject.Properties) {
        $val = $prop.Value
        if ($val -and $val.version -and $val.version.StartsWith($targetVer)) {
            foreach ($subProp in $val.PSObject.Properties) {
                $key = $subProp.Name
                if ($key -match '^ts-.*-x64$' -and $subProp.Value.zip) {
                    if ($null -eq $resolvedVer -or [version]($val.version -replace '-.*','') -gt [version]($resolvedVer -replace '-.*','')) {
                        $resolvedVer = $val.version
                        $downloadPath = $subProp.Value.zip.path
                        $sha256 = $subProp.Value.zip.sha256
                    }
                }
            }
        }
    }

    if (-not $resolvedVer -or -not $downloadPath) {
        Print-Error ("Could not find an official Windows x64 Thread Safe build for PHP '{0}'." -f $targetVer)
        return
    }

    $targetDir = Join-Path $script:PVM_VERSIONS_DIR $resolvedVer
    if ((Test-Path $targetDir) -and -not $Force) {
        Print-Warn ("PHP {0} is already installed at {1}." -f $resolvedVer, $targetDir)
        Print-Info ("Use 'pvm install {0} --force' to reinstall." -f $resolvedVer)
        return
    }

    $fullUrl = $script:BASE_DOWNLOAD_URL + $downloadPath
    $tempZip = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "php_${resolvedVer}.zip")

    Print-Info ("Downloading PHP {0}{1}{2} from {3}..." -f $script:C_BOLD, $resolvedVer, $script:C_RESET, $fullUrl)
    Invoke-WebRequest -Uri $fullUrl -OutFile $tempZip

    if ($sha256) {
        Print-Info "Verifying SHA256 cryptographic checksum..."
        $hashObj = Get-FileHash -Path $tempZip -Algorithm SHA256
        if ($hashObj.Hash -ne $sha256) {
            Remove-Item -Path $tempZip -Force
            Print-Error ("SHA256 checksum mismatch! Expected: {0}, Actual: {1}" -f $sha256, $hashObj.Hash)
            return
        }
        Print-Success ("Checksum verified ({0})" -f $sha256)
    }

    Print-Info ("Extracting binaries into {0}..." -f $targetDir)
    if (Test-Path $targetDir) { Remove-Item -Path $targetDir -Force -Recurse }
    [void][System.IO.Directory]::CreateDirectory($targetDir)
    Expand-Archive -Path $tempZip -DestinationPath $targetDir -Force
    Remove-Item -Path $tempZip -Force

    $iniPath = Join-Path $targetDir "php.ini"
    if (-not (Test-Path $iniPath)) {
        $devIni = Join-Path $targetDir "php.ini-development"
        if (Test-Path $devIni) { Copy-Item -Path $devIni -Destination $iniPath }
    }

    Print-Success ("Successfully installed PHP {0}{1}{2}!" -f $script:C_BOLD, $resolvedVer, $script:C_RESET)
    Print-Info ("Run 'pvm use {0}' to switch to this version." -f $resolvedVer)
}

function Cmd-Uninstall([string]$spec) {
    if (-not $spec) {
        Print-Error "Please specify a PHP version to uninstall (e.g., 'pvm uninstall 8.3.3')."
        return
    }
    $targetVer = Resolve-Alias $spec
    $targetDir = Join-Path $script:PVM_VERSIONS_DIR $targetVer

    if (-not (Test-Path $targetDir)) {
        Print-Error ("PHP version '{0}' is not installed in {1}." -f $targetVer, $script:PVM_VERSIONS_DIR)
        return
    }

    $activeVer = Get-ActiveVersion
    if ($activeVer -eq $targetVer) {
        Print-Error ("Cannot uninstall PHP {0} because it is currently the active runtime!" -f $targetVer)
        Print-Info "Switch to another version with 'pvm use <version>' first."
        return
    }

    Print-Info ("Removing {0}..." -f $targetDir)
    Remove-Item -Path $targetDir -Force -Recurse
    Print-Success ("PHP {0} uninstalled cleanly." -f $targetVer)
}

function Cmd-Env([string]$Mode) {
    if (-not $Mode -or $Mode -eq "--check") {
        Write-Host ("`n{0}PVM PATH Environment Audit:{1}" -f $script:C_BOLD, $script:C_RESET)
        Write-Host ("  * {0}PVM Junction Path:{1} {2}" -f $script:C_CYAN, $script:C_RESET, $script:PVM_CURRENT_DIR)
        $inPath = ($env:PATH -split ";") -contains $script:PVM_CURRENT_DIR
        if ($inPath) {
            Write-Host ("  * {0}PATH Hygiene     :{1} {2}HEALTHY (PVM current junction is present in session PATH){1}" -f $script:C_CYAN, $script:C_RESET, $script:C_GREEN)
        } else {
            Write-Host ("  * {0}PATH Hygiene     :{1} {2}WARNING (PVM current junction is missing from session PATH){1}" -f $script:C_CYAN, $script:C_RESET, $script:C_YELLOW)
        }
        $phpLocation = Get-Command php -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
        if ($phpLocation) {
            Write-Host ("  * {0}Active php binary:{1} {2}" -f $script:C_CYAN, $script:C_RESET, $phpLocation)
        } else {
            Write-Host ("  * {0}Active php binary:{1} {2}Not found in PATH{1}" -f $script:C_CYAN, $script:C_RESET, $script:C_RED)
        }
        Write-Host ""
    } elseif ($Mode -eq "--clean") {
        Print-Info "Cleaning duplicate entries from session PATH..."
        $parts = $env:PATH -split ";" | Where-Object { $_ -ne "" }
        $clean = $parts | Select-Object -Unique
        $env:PATH = ($clean -join ";")
        Print-Success "Session PATH cleaned!"
    } elseif ($Mode -eq "--ps1") {
        Write-Output ("`$env:PATH = `"{0};{1};`" + `$env:PATH" -f $script:PVM_CURRENT_DIR, $script:PVM_BIN_DIR)
    } elseif ($Mode -eq "--cmd") {
        Write-Output ("set PATH={0};{1};%PATH%" -f $script:PVM_CURRENT_DIR, $script:PVM_BIN_DIR)
    } else {
        Print-Error ("Unknown env option: {0} (Use --check, --clean, --ps1, --cmd)" -f $Mode)
    }
}

function Cmd-Ini([string]$Sub, [string]$Arg1, [string]$Arg2) {
    $activeVer = Get-ActiveVersion
    if (-not $activeVer) {
        Print-Error "No active PHP version is set. Run 'pvm use <version>' first."
        return
    }
    $iniPath = Join-Path (Join-Path $script:PVM_VERSIONS_DIR $activeVer) "php.ini"
    if (-not (Test-Path $iniPath)) {
        Print-Error ("php.ini file not found for active version ({0})." -f $iniPath)
        return
    }

    switch ($Sub) {
        { $_ -in "ls", "list" } {
            Render-TableHeader ("PHP Extensions ({0} -> php.ini)" -f $activeVer)
            Get-Content -Path $iniPath | Where-Object { $_ -match '^[;\s]*extension\s*=' } | ForEach-Object {
                if ($_ -match '^[;\s]*extension\s*=\s*([^;\s]+)') {
                    $ext = $Matches[1]
                    $status = if ($_ -like ";*") { ("{0}DISABLED{1}" -f $script:C_YELLOW, $script:C_RESET) } else { ("{0}ENABLED {1}" -f $script:C_GREEN, $script:C_RESET) }
                    Render-TableRow $status $ext "DLL" "Zend/Standard" "php.ini"
                }
            }
            Render-TableFooter
        }
        "enable" {
            if (-not $Arg1) { Print-Error "Specify extension name"; return }
            $lines = Get-Content -Path $iniPath
            $found = $false
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match "^[;\s]*extension\s*=\s*$Arg1") {
                    $lines[$i] = "extension=$Arg1"
                    $found = $true
                }
            }
            if (-not $found) { $lines += "extension=$Arg1" }
            $lines | Out-File -FilePath $iniPath -Encoding utf8
            Print-Success ("Enabled extension '{0}{1}{2}' in {3}" -f $script:C_BOLD, $Arg1, $script:C_RESET, $iniPath)
        }
        "disable" {
            if (-not $Arg1) { Print-Error "Specify extension name"; return }
            $lines = Get-Content -Path $iniPath
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match "^\s*extension\s*=\s*$Arg1") {
                    $lines[$i] = ";extension=$Arg1"
                }
            }
            $lines | Out-File -FilePath $iniPath -Encoding utf8
            Print-Success ("Disabled extension '{0}{1}{2}' in {3}" -f $script:C_BOLD, $Arg1, $script:C_RESET, $iniPath)
        }
        "get" {
            if (-not $Arg1) { Print-Error "Specify directive name"; return }
            $match = Get-Content -Path $iniPath | Select-String -Pattern "^\s*$Arg1\s*=\s*(.*)" | Select-Object -First 1
            if ($match -and $match.Matches.Groups[1].Value) {
                Write-Host ("{0} = {1}" -f $Arg1, $match.Matches.Groups[1].Value)
            } else {
                Write-Host ("{0} = Not Set" -f $Arg1)
            }
        }
        "set" {
            if (-not $Arg1 -or -not $Arg2) { Print-Error "Specify directive and value"; return }
            $lines = Get-Content -Path $iniPath
            $found = $false
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match "^[;\s]*$Arg1\s*=") {
                    $lines[$i] = "$Arg1 = $Arg2"
                    $found = $true
                }
            }
            if (-not $found) { $lines += "$Arg1 = $Arg2" }
            $lines | Out-File -FilePath $iniPath -Encoding utf8
            Print-Success ("Set directive {0}{1} = {2}{3} in {4}" -f $script:C_BOLD, $Arg1, $Arg2, $script:C_RESET, $iniPath)
        }
        "open" {
            Print-Info ("Launching editor for {0}..." -f $iniPath)
            Start-Process notepad.exe -ArgumentList $iniPath
        }
        default {
            Print-Error ("Unknown ini option: {0} (Use ls, enable, disable, get, set, open)" -f $Sub)
        }
    }
}

function Cmd-Alias([string]$Name, [string]$Target) {
    Ensure-Directories
    $dict = Get-AliasesDict
    if (-not $Name) {
        Render-TableHeader ("Configured PVM Version Aliases ({0})" -f $script:PVM_ALIASES_FILE)
        foreach ($key in $dict.Keys) {
            Render-TableRow "Alias" $key "->  " $dict[$key] $script:PVM_ALIASES_FILE
        }
        Render-TableFooter
        return
    }

    if ($Name -in "--remove", "-r") {
        $Name = $Target
        $Target = "--remove"
    }

    if ($Target -in "--remove", "-r") {
        if ($dict.ContainsKey($Name)) {
            $dict.Remove($Name)
            Save-AliasesDict $dict
            Print-Success ("Removed alias '{0}'." -f $Name)
        } else {
            Print-Warn ("Alias '{0}' does not exist." -f $Name)
        }
        return
    }

    if (-not $Target) {
        Write-Host ("{0} -> {1}" -f $Name, (Resolve-Alias $Name))
        return
    }

    $dict[$Name] = $Target
    Save-AliasesDict $dict
    Print-Success ("Created alias '{0}{1}{2}' -> '{0}{3}{2}'" -f $script:C_BOLD, $Name, $script:C_RESET, $Target)
}

function Cmd-Doctor([bool]$Fix = $false) {
    Write-Host ("`n{0}Running PVM Doctor System Diagnostics & Health Audit...{1}`n" -f $script:C_BOLD, $script:C_RESET)
    $errors = 0

    if ((Test-Path $script:PVM_HOME) -and (Test-Path $script:PVM_VERSIONS_DIR) -and (Test-Path $script:PVM_BIN_DIR)) {
        Print-Success ("Check 1: PVM Directory Structure is intact ({0})" -f $script:PVM_HOME)
    } else {
        Print-Error "Check 1: Missing core PVM directories."
        $errors++
        if ($Fix) { Ensure-Directories; Print-Success "Repaired PVM directories!"; $errors-- }
    }

    $activeVer = Get-ActiveVersion
    if ($activeVer -and (Test-Path (Join-Path $script:PVM_CURRENT_DIR "php.exe"))) {
        Print-Success ("Check 2: Active junction link is valid -> {0}" -f $activeVer)
    } else {
        Print-Warn ("Check 2: No active PHP runtime is currently linked in {0}" -f $script:PVM_CURRENT_DIR)
    }

    if (($env:PATH -split ";") -contains $script:PVM_CURRENT_DIR) {
        Print-Success ("Check 3: PVM current junction ({0}) is properly inside session PATH" -f $script:PVM_CURRENT_DIR)
    } else {
        Print-Warn "Check 3: PVM current junction is not in active session PATH"
    }

    $activePhp = Get-Command php -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if ($activePhp -and ($activePhp -like "*$script:PVM_HOME*" -or $activePhp -like "*.pvm*")) {
        Print-Success ("Check 4: php command correctly resolves to PVM ({0})" -f $activePhp)
    } elseif ($activePhp) {
        Print-Warn ("Check 4: External PHP installation shadows PVM ({0})" -f $activePhp)
    }

    Write-Host ""
    if ($errors -eq 0) {
        Print-Success "PVM Doctor audit completed with zero fatal errors!"
    } else {
        Print-Error ("PVM Doctor audit found {0} error(s)." -f $errors)
    }
}

function Cmd-SelfUpdate([bool]$CheckOnly = $false) {
    Print-Info "Checking GitHub Releases API for latest PVM update..."
    try {
        $response = Invoke-RestMethod -Uri $script:GITHUB_API_LATEST -Method Get
        $latestTag = $response.tag_name -replace '^v',''
        Print-Info ("Current Version: {0}{1}{2} | Latest Release: {0}{3}{2}" -f $script:C_BOLD, $script:PVM_VERSION, $script:C_RESET, $latestTag)
        if (-not $CheckOnly) {
            $rawUrl = "https://raw.githubusercontent.com/hasanhawary/phpvm/main/shell/pvm.ps1"
            Invoke-WebRequest -Uri $rawUrl -OutFile (Join-Path $script:PVM_BIN_DIR "pvm.ps1")
            Print-Success "PVM PowerShell script updated successfully!"
        }
    } catch {
        Print-Error "Failed to check or update from GitHub."
    }
}

function Cmd-SelfUninstall([bool]$Yes = $false) {
    if (-not $Yes) {
        Write-Host ("{0}[WARN] WARNING: This will completely delete {1} and all installed PHP versions!{2}" -f $script:C_YELLOW, $script:PVM_HOME, $script:C_RESET)
        $confirm = Read-Host "Are you sure you want to proceed? [y/N]"
        if ($confirm -ne "y" -and $confirm -ne "Y") {
            Print-Info "Uninstallation cancelled."
            return
        }
    }
    Print-Info ("Removing {0} directory..." -f $script:PVM_HOME)
    Remove-Item -Path $script:PVM_HOME -Force -Recurse -ErrorAction SilentlyContinue
    Print-Success "PVM has been completely uninstalled from your machine."
}

function Cmd-Completion([string]$ShellType) {
    if ($ShellType -eq "powershell") {
        Write-Output 'Register-ArgumentCompleter -CommandName pvm -ScriptBlock { param($cmd, $arg, $wordToComplete) @("list","current","use","install","uninstall","env","ini","alias","doctor","self-update","self-uninstall") | Where-Object { $_ -like "$wordToComplete*" } }'
    } elseif ($ShellType -eq "bash") {
        Write-Output 'complete -W "list ls current use install uninstall env ini alias doctor self-update self-uninstall completion --help --version" pvm'
    } else {
        Print-Error "Specify shell type: powershell or bash"
    }
}

function Show-Help {
    Write-Host ("{0}PVM - PHP Version Manager (PowerShell Edition v{1}){2}`n" -f $script:C_BOLD, $script:PVM_VERSION, $script:C_RESET)
    Write-Host ("Usage: {0}pvm <command> [arguments]{1}`n" -f $script:C_BOLD, $script:C_RESET)
    Write-Host ("{0}Commands:{1}" -f $script:C_BOLD, $script:C_RESET)
    Write-Host '  list, ls [--remote]     List local installed PHP versions or fetch remote mirror catalog'
    Write-Host '  current                 Display the currently active PHP runtime status and CLI output'
    Write-Host '  use <version>           Switch active PHP version via atomic directory junction'
    Write-Host '  install <version> [-f]  Download, verify SHA256, and install a PHP version from official mirror'
    Write-Host '  uninstall <version>     Remove an installed PHP version from ~/.pvm/versions'
    Write-Host '  env [--check|--clean]   Audit PATH hygiene or clean duplicate PATH entries'
    Write-Host '  ini <subcommand>        Inspect or modify active php.ini (ls, enable, disable, get, set, open)'
    Write-Host '  alias [name] [target]   Create, list, or remove (--remove) semantic version aliases'
    Write-Host '  doctor [--fix]          Run system health audits and automatically remediate PATH issues'
    Write-Host '  self-update [--check]   Check or update the pvm script directly from GitHub Releases'
    Write-Host '  self-uninstall [-y]     Completely uninstall PVM and remove all local PHP installations'
    Write-Host '  completion <shell>      Generate tab autocompletion script'
    Write-Host ''
}

# --- Router ---
switch ($Command) {
    { $_ -in "list", "ls" } {
        $remote = $Arguments -contains "--remote" -or $Arguments -contains "-r"
        Cmd-List -Remote $remote
    }
    "current" { Cmd-Current }
    "use" {
        $spec = if ($Arguments.Count -gt 0) { $Arguments[0] } else { "" }
        Cmd-Use -spec $spec
    }
    { $_ -in "install", "i" } {
        $force = $Arguments -contains "--force" -or $Arguments -contains "-f"
        $spec = if ($Arguments[0] -in "--force", "-f") { $Arguments[1] } else { $Arguments[0] }
        Cmd-Install -spec $spec -Force $force
    }
    { $_ -in "uninstall", "rm" } {
        $spec = if ($Arguments.Count -gt 0) { $Arguments[0] } else { "" }
        Cmd-Uninstall -spec $spec
    }
    "env" {
        $mode = if ($Arguments.Count -gt 0) { $Arguments[0] } else { "--check" }
        Cmd-Env -Mode $mode
    }
    "ini" {
        $sub = if ($Arguments.Count -gt 0) { $Arguments[0] } else { "ls" }
        $arg1 = if ($Arguments.Count -gt 1) { $Arguments[1] } else { "" }
        $arg2 = if ($Arguments.Count -gt 2) { $Arguments[2] } else { "" }
        Cmd-Ini -Sub $sub -Arg1 $arg1 -Arg2 $arg2
    }
    "alias" {
        $name = if ($Arguments.Count -gt 0) { $Arguments[0] } else { "" }
        $target = if ($Arguments.Count -gt 1) { $Arguments[1] } else { "" }
        Cmd-Alias -Name $name -Target $target
    }
    "doctor" {
        $fix = $Arguments -contains "--fix" -or $Arguments -contains "-f"
        Cmd-Doctor -Fix $fix
    }
    { $_ -in "self-update", "update" } {
        $chk = $Arguments -contains "--check" -or $Arguments -contains "-c"
        Cmd-SelfUpdate -CheckOnly $chk
    }
    "self-uninstall" {
        $yes = $Arguments -contains "-y" -or $Arguments -contains "--yes"
        Cmd-SelfUninstall -Yes $yes
    }
    "completion" {
        $shell = if ($Arguments.Count -gt 0) { $Arguments[0] } else { "powershell" }
        Cmd-Completion -ShellType $shell
    }
    { $_ -in "--help", "-h", "help", "" } { Show-Help }
    { $_ -in "--version", "-v" } { Write-Host "pvm version $script:PVM_VERSION" }
    default { Print-Error "Unknown command: $Command. Run 'pvm --help' for usage." }
}
