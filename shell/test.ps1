# ==============================================================================
# Universal PVM Automated Test Suite (PowerShell Edition)
# Repository: https://github.com/hasanhawary/phpvm
# License: MIT
# ==============================================================================

[CmdletBinding()]
param()

$ErrorActionPreference = "Continue"

$script:C_RESET = "`e[0m"
$script:C_BOLD = "`e[1m"
$script:C_GREEN = "`e[0;32m"
$script:C_CYAN = "`e[0;36m"
$script:C_YELLOW = "`e[1;33m"
$script:C_RED = "`e[0;31m"

$passCount = 0
$failCount = 0

function Log-Test([string]$message) { Write-Host ("`n{0}[TEST]{1} {2}{3}{1}" -f $script:C_CYAN, $script:C_RESET, $script:C_BOLD, $message) }
function Log-Pass([string]$message) { Write-Host ("{0}[OK] PASS:{1} {2}" -f $script:C_GREEN, $script:C_RESET, $message); $script:passCount++ }
function Log-Fail([string]$message) { Write-Host ("{0}[ERR] FAIL:{1} {2}" -f $script:C_RED, $script:C_RESET, $message); $script:failCount++ }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pvmPs1 = [System.IO.Path]::Combine($scriptDir, "pvm.ps1")

if (-not (Test-Path $pvmPs1)) {
    Log-Fail ("pvm.ps1 not found at {0}" -f $pvmPs1)
    exit 1
}

Log-Test "1. Verify --version command"
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 --version 2>&1 | Out-String
if ($out -match "1.1.0-shell") {
    Log-Pass "--version reported expected version"
} else {
    Log-Fail "--version check failed ($out)"
}

Log-Test "2. Verify --help command"
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 --help 2>&1 | Out-String
if ($out -match "Usage:") {
    Log-Pass "--help output is valid"
} else {
    Log-Fail "--help check failed"
}

Log-Test "3. Verify list command"
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 list 2>&1 | Out-String
if ($out -match "8\.") {
    Log-Pass "list command listed installed PHP versions"
} else {
    Log-Fail "list command check failed"
}

Log-Test "4. Verify current command"
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 current 2>&1 | Out-String
if ($out -match "Active PVM Runtime Status:") {
    Log-Pass "current command displayed runtime status and CLI output"
} else {
    Log-Fail "current command check failed"
}

Log-Test "5. Verify alias creation, lookup, and removal"
powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 alias testalias 8.4 | Out-Null
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 alias 2>&1 | Out-String
if ($out -match "testalias") {
    Log-Pass "Created and listed 'testalias'"
} else {
    Log-Fail "Failed to verify alias creation"
}
powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 alias -r testalias | Out-Null
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 alias 2>&1 | Out-String
if (-not ($out -match "testalias")) {
    Log-Pass "Successfully removed 'testalias'"
} else {
    Log-Fail "Alias removal failed"
}

Log-Test "6. Verify ini ls command"
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 ini ls 2>&1 | Out-String
if ($out -match "curl|mbstring|openssl") {
    Log-Pass "ini ls successfully enumerated extensions"
} else {
    Log-Fail "ini ls check failed"
}

Log-Test "7. Verify env --check command"
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 env --check 2>&1 | Out-String
if ($out -match "PVM PATH Environment Audit:") {
    Log-Pass "env --check successfully audited environment"
} else {
    Log-Fail "env --check check failed"
}

Log-Test "8. Verify doctor health check"
$out = powershell -NoProfile -ExecutionPolicy Bypass -File $pvmPs1 doctor 2>&1 | Out-String
if ($out -match "Check 1: PVM Directory Structure is intact") {
    Log-Pass "doctor health check completed successfully"
} else {
    Log-Fail "doctor health check failed"
}

Write-Host ("`n{0}=============================================================================={1}" -f $script:C_BOLD, $script:C_RESET)
if ($failCount -eq 0) {
    Write-Host ("{0}[OK] All {1} PowerShell tests PASSED cleanly with zero errors!{2}" -f $script:C_GREEN, $passCount, $script:C_RESET)
    Write-Host ("{0}=============================================================================={1}`n" -f $script:C_BOLD, $script:C_RESET)
    exit 0
} else {
    Write-Host ("{0}[ERR] Test Suite completed with {1} failed test(s) ({2} passed).{3}" -f $script:C_RED, $failCount, $passCount, $script:C_RESET)
    Write-Host ("{0}=============================================================================={1}`n" -f $script:C_BOLD, $script:C_RESET)
    exit 1
}
