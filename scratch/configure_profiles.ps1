$ErrorActionPreference = "Stop"

Write-Host "Configuring Git Bash (~/.bashrc and ~/.bash_profile)..." -ForegroundColor Cyan
$bashConfig = @"

# PVM (PHP Version Manager)
export PATH="`$HOME/.pvm/current:`$HOME/.pvm/bin:`$PATH"
"@

foreach ($file in @("$HOME\.bashrc", "$HOME\.bash_profile", "$HOME\.profile")) {
    $content = ""
    if (Test-Path $file) {
        $content = Get-Content -Path $file -Raw -ErrorAction SilentlyContinue
    }
    if ($content -notlike "*\.pvm/current*") {
        Add-Content -Path $file -Value $bashConfig -Force
        Write-Host "Added PVM to $file" -ForegroundColor Green
    } else {
        Write-Host "PVM already in $file" -ForegroundColor Green
    }
}

Write-Host "`nConfiguring PowerShell Profiles..." -ForegroundColor Cyan
$psConfig = @"

# PVM (PHP Version Manager)
`$env:PATH = "`$HOME\.pvm\current;`$HOME\.pvm\bin;`$env:PATH"
"@

$psProfiles = @(
    "$HOME\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1",
    "$HOME\Documents\PowerShell\Microsoft.PowerShell_profile.ps1"
)

foreach ($file in $psProfiles) {
    $dir = Split-Path -Parent $file
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $content = ""
    if (Test-Path $file) {
        $content = Get-Content -Path $file -Raw -ErrorAction SilentlyContinue
    }
    if ($content -notlike "*\.pvm\current*") {
        Add-Content -Path $file -Value $psConfig -Force
        Write-Host "Added PVM to $file" -ForegroundColor Green
    } else {
        Write-Host "PVM already in $file" -ForegroundColor Green
    }
}

Write-Host "`nAll shell profiles (Bash, Git Bash, PowerShell) configured to prioritize PVM without Administrator privileges!" -ForegroundColor Green
