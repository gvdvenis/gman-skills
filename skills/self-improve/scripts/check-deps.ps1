#Requires -Version 5.1
<#
.SYNOPSIS
    Non-blocking preflight check for gman-skills dependencies.
.DESCRIPTION
    Warns when the dotnet-blazor plugin or report-server binary is missing.
    Always exits 0 — never blocks.
#>
$ErrorActionPreference = "Continue"

$BinaryDir = Join-Path $env:USERPROFILE ".copilot\gman-skills\bin"
$BinaryPath = Join-Path $BinaryDir "report-server.exe"
$allGood = $true

# --- Check dotnet-blazor plugin ---------------------------------------------
$pluginList = ""
try {
    $pluginList = copilot plugin list 2>&1
} catch {
    $pluginList = ""
}

if ($pluginList -match "dotnet-blazor") {
    Write-Host "[OK] dotnet-blazor plugin is installed."
} else {
    Write-Host "[WARN] dotnet-blazor plugin is NOT installed."
    Write-Host "       Install it: copilot plugin marketplace add dotnet/skills && copilot plugin install dotnet-blazor@dotnet-agent-skills"
    $allGood = $false
}

# --- Check report-server binary ---------------------------------------------
if (Test-Path $BinaryPath) {
    Write-Host "[OK] report-server binary found at: $BinaryPath"
} else {
    Write-Host "[WARN] report-server binary NOT found at: $BinaryPath"
    Write-Host "       Run: scripts\setup-report-server.ps1"
    $allGood = $false
}

# --- Summary ----------------------------------------------------------------
if ($allGood) {
    Write-Host ""
    Write-Host "All gman-skills dependencies are satisfied."
} else {
    Write-Host ""
    Write-Host "Some dependencies are missing — see warnings above."
}

exit 0
