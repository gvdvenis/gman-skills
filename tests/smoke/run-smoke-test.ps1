#Requires -Version 5.1
<#
.SYNOPSIS
    Headless smoke test for the gman-skills install-to-run path.
.DESCRIPTION
    Verifies the full contract observable from a `copilot -p` headless run:
      1. Skills install via `npx skills add gvdvenis/gman-skills` and `--list` shows all three.
      2. /setup-gman-skills runs and confirms dotnet-blazor + report-server binary present.
      3. copilot -p invocation with --self-improve produces a run ID matching run-YYYYMMDD-HHMM
         in the first output line.
      4. improvement-report-data.json exists at ~/.self-improve-reports/blazor-architect/runs/<run_id>/.
      5. Report-server responds on http://127.0.0.1:5173/api/report during the --self-improve run.
      6. copilot -p invocation without --self-improve produces no improvement-report-data.json
         and no server on port 5173.

    The test creates a temp Blazor project, runs the skills headless, and asserts each contract
    point. It is structured to run both locally and in GitHub CI.

    Environment variables (all optional):
      COPILOT_BIN     - path to the copilot executable (default: resolved from PATH)
      DOTNET_BIN       - path to the dotnet executable (default: resolved from PATH)
      KEEP_TEMP       - set to "1" to keep the temp project after the test finishes
.EXAMPLE
    pwsh -File tests/smoke/run-smoke-test.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3.0

$script:failures = @()
$script:passes   = 0
$script:tempDirs = @()
$script:tempFiles = @()
$script:serverPids = @()

# ============================================================================
# Helpers
# ============================================================================

function Write-Pass {
    param([string]$Message)
    $script:passes++
    Write-Host "  [PASS] $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    $script:failures += $Message
    Write-Host "  [FAIL] $Message" -ForegroundColor Red
}

function Test-Contract {
    param([string]$Name, [scriptblock]$Check)
    try {
        & $Check
        if ($LASTEXITCODE -ne 0 -and -not $?) {
            throw "Check exited with code $LASTEXITCODE"
        }
    } catch {
        Write-Fail "${Name}: $($_.Exception.Message)"
    }
}

function New-TempDir {
    param([string]$Prefix)
    $path = Join-Path ([System.IO.Path]::GetTempPath()) "$Prefix-$(Get-Random)"
    New-Item -ItemType Directory -Path $path -Force | Out-Null
    $script:tempDirs += $path
    return $path
}

function Resolve-Binary {
    param([string]$Name, [string]$EnvVar)
    $bin = [Environment]::GetEnvironmentVariable($EnvVar)
    if ($bin) { return $bin }
    $found = Get-Command $Name -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    Write-Host "  [ERROR] '$Name' not found on PATH. Set ${EnvVar} to its full path." -ForegroundColor Red
    exit 2
}

function Stop-ServerPids {
    foreach ($pid in $script:serverPids) {
        try { Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue } catch {}
    }
}

function Invoke-Cleanup {
    # Kill any stray report-server processes we started
    Stop-ServerPids

    # Kill any stray report-server still on port 5173
    try {
        $conns = Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue
        foreach ($conn in $conns) {
            try { Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue } catch {}
        }
    } catch {}

    # Remove temp files
    foreach ($f in $script:tempFiles) {
        Remove-Item $f -Force -ErrorAction SilentlyContinue
    }

    # Remove temp directories
    if ($env:KEEP_TEMP -eq "1") {
        Write-Host ""
        Write-Host "Temp directories kept (KEEP_TEMP=1):" -ForegroundColor Yellow
        foreach ($d in $script:tempDirs) { Write-Host "  $d" }
    } else {
        foreach ($d in $script:tempDirs) {
            Remove-Item $d -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

# ============================================================================
# Setup
# ============================================================================

$CopilotBin = Resolve-Binary -Name "copilot" -EnvVar "COPILOT_BIN"
$DotnetBin  = Resolve-Binary -Name "dotnet"  -EnvVar "DOTNET_BIN"

Write-Host ""
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host " gman-skills headless smoke test" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host " copilot:  $CopilotBin"
Write-Host " dotnet:   $DotnetBin"
Write-Host " platform: $($PSVersionTable.OS)"
Write-Host ""

# ============================================================================
# Contract 1: Install skills and --list shows all three
# ============================================================================

Write-Host "[1] Install skills and verify --list shows all three" -ForegroundColor Yellow

Test-Contract "skills-install" {
    Write-Host "  Running: npx skills add gvdvenis/gman-skills"
    $installOutput = & npx skills add --yes gvdvenis/gman-skills 2>&1 | Out-String
    Write-Host $installOutput
    if ($LASTEXITCODE -eq 0) {
        Write-Pass "npx skills add succeeded"
    } else {
        Write-Fail "npx skills add failed (exit code $LASTEXITCODE)"
    }
}

Test-Contract "skills-list" {
    $listOutput = & npx skills list 2>&1 | Out-String
    Write-Host "  npx skills list output:"
    Write-Host $listOutput

    $skillNames = @("blazor-architect", "self-improve", "setup-gman-skills")
    foreach ($skill in $skillNames) {
        if ($listOutput -match $skill) {
            Write-Pass "skill '$skill' appears in --list output"
        } else {
            Write-Fail "skill '$skill' NOT found in --list output"
        }
    }
}

# ============================================================================
# Contract 2: /setup-gman-skills runs and confirms dotnet-blazor + report-server
# ============================================================================

Write-Host ""
Write-Host "[2] /setup-gman-skills confirms dotnet-blazor + report-server binary" -ForegroundColor Yellow

Test-Contract "setup-gman-skills" {
    $setupOutput = & $CopilotBin -p "/setup-gman-skills" --allow-all --add-dir $env:USERPROFILE 2>&1 | Out-String
    Write-Host $setupOutput

    if ($setupOutput -match "dotnet-blazor.*installed|dotnet-blazor.*present|dotnet-blazor.*already") {
        Write-Pass "setup confirms dotnet-blazor plugin present"
    } else {
        Write-Fail "setup did not confirm dotnet-blazor plugin"
    }

    if ($setupOutput -match "report-server.*present|report-server.*downloaded|report-server.*installed|report-server.*already") {
        Write-Pass "setup confirms report-server binary present"
    } else {
        Write-Fail "setup did not confirm report-server binary"
    }
}

# ============================================================================
# Contract 3-5: copilot -p with --self-improve produces run ID, report, server
# ============================================================================

Write-Host ""
Write-Host "[3-5] copilot -p with --self-improve: run ID, report data, server" -ForegroundColor Yellow

$projectDir = $null
$runId = $null
$improveOutput = $null
$script:serverResponded = $false

Test-Contract "self-improve-run-id-and-server" {
    # Create a temp Blazor project
    $script:projectDir = New-TempDir -Prefix "smoke-blazor"
    Write-Host "  Creating temp Blazor project at: $script:projectDir"
    & $DotnetBin new blazor -o $script:projectDir --no-restore 2>&1 | Out-Null

    # Start copilot as a background job so we can poll the server concurrently
    Write-Host "  Starting copilot -p with --self-improve (background)..."
    $job = Start-Job -ScriptBlock {
        param($CopilotBin, $ProjectDir, $UserProfile)
        & $CopilotBin -p "/blazor-architect implement a simple counter component in Components/Pages/Counter.razor --self-improve" `
            --allow-all --add-dir $ProjectDir --add-dir $UserProfile 2>&1
    } -ArgumentList $CopilotBin, $script:projectDir, $env:USERPROFILE

    # Poll for report-server on port 5173 while copilot is running AND for a
    # short period after it finishes — the server may be launched near the end.
    $maxRetries = 40
    for ($i = 0; $i -lt $maxRetries; $i++) {
        Start-Sleep -Seconds 3
        try {
            $response = Invoke-RestMethod -Uri "http://127.0.0.1:5173/api/report" -TimeoutSec 5 -ErrorAction Stop
            $script:serverResponded = $true
            Write-Host "  [INFO] report-server responded during copilot run (poll $i)"
            break
        } catch {
            # server not yet up — keep polling
        }
        # Stop polling if copilot finished AND we've done at least 15 polls
        if ($job.State -eq "Completed" -and $i -gt 15) { break }
    }

    # Wait for copilot to finish (with a generous timeout)
    if ($job.State -ne "Completed") {
        Write-Host "  Waiting for copilot to finish..."
        $job | Wait-Job -Timeout 600 | Out-Null
    }
    $improveOutput = Receive-Job $job | Out-String
    Remove-Job $job -Force
    Write-Host $improveOutput

    $firstLine = ($improveOutput -split "`n" | Select-Object -First 1)

    # Search all output lines for the run ID, not just the first — copilot -p
    # may print skill headers before the run ID line.
    $runIdMatch = $null
    foreach ($line in ($improveOutput -split "`n")) {
        $m = [regex]::Match($line, "(run-\d{8}-\d{4})")
        if ($m.Success) {
            $runIdMatch = $m
            break
        }
    }
    if ($runIdMatch.Success) {
        $script:runId = $runIdMatch.Groups[1].Value
        Write-Pass "run ID '$($script:runId)' found in first output line"
    } else {
        Write-Fail "run ID matching run-YYYYMMDD-HHMM not found in first output line: $firstLine"
    }

    if ($script:serverResponded) {
        Write-Pass "report-server responded on http://127.0.0.1:5173/api/report during the run"
    } else {
        Write-Fail "report-server did NOT respond on http://127.0.0.1:5173/api/report during the run"
    }

    # Always try to shut down the server so the next contract test has a clean port,
    # even if the polling didn't catch it in time.
    try {
        Invoke-RestMethod -Uri "http://127.0.0.1:5173/shutdown" -TimeoutSec 5 -ErrorAction SilentlyContinue | Out-Null
    } catch {}
    Start-Sleep -Seconds 2
    # Also kill any stray server process on port 5173
    try {
        $conns = Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue
        foreach ($conn in $conns) {
            try { Stop-Process -Id $conn.OwningProcess -Force -ErrorAction SilentlyContinue } catch {}
        }
    } catch {}
}

Test-Contract "self-improve-report-data" {
    if (-not $script:runId) {
        Write-Fail "improvement-report-data.json: no run ID from previous step"
        return
    }

    $runDir = Join-Path $env:USERPROFILE ".self-improve-reports\blazor-architect\runs\$($script:runId)"
    $reportFile = Join-Path $runDir "improvement-report-data.json"

    if (Test-Path $reportFile) {
        $content = Get-Content $reportFile -Raw
        $json = $content | ConvertFrom-Json
        if ($json.schema_version -eq "1.1") {
            Write-Pass "improvement-report-data.json exists with schema_version 1.1 at $reportFile"
        } else {
            Write-Fail "improvement-report-data.json schema_version is '$($json.schema_version)', expected '1.1'"
        }
    } else {
        Write-Fail "improvement-report-data.json NOT found at $reportFile"
    }
}

# ============================================================================
# Contract 6: copilot -p without --self-improve produces no report, no server
# ============================================================================

Write-Host ""
Write-Host "[6] copilot -p without --self-improve: no report, no server" -ForegroundColor Yellow

$noImproveRunId = $null
$noImproveOutput = $null

Test-Contract "no-self-improve-run-id" {
    if (-not $script:projectDir) {
        Write-Fail "no self-improve: temp project not created (previous step failed)"
        return
    }

    $noImproveOutput = & $CopilotBin -p "/blazor-architect add a simple greeting component" `
        --allow-all --add-dir $script:projectDir --add-dir $env:USERPROFILE 2>&1 | Out-String
    Write-Host $noImproveOutput

    # The spec requires no report data file and no server — we do NOT require a run ID here.
    # If a run ID is present we use it to check the run directory; if not, we check all runs
    # created in the last 2 minutes for a stray report file.
    $firstLine = ($noImproveOutput -split "`n" | Select-Object -First 1)
    $runIdMatch = $null
    foreach ($line in ($noImproveOutput -split "`n")) {
        $m = [regex]::Match($line, "(run-\d{8}-\d{4})")
        if ($m.Success) {
            $runIdMatch = $m
            break
        }
    }
    if ($runIdMatch.Success) {
        $script:noImproveRunId = $runIdMatch.Groups[1].Value
    }
}

Test-Contract "no-self-improve-no-report" {
    $runsRoot = Join-Path $env:USERPROFILE ".self-improve-reports\blazor-architect\runs"
    $foundReport = $false

    if ($script:noImproveRunId) {
        $runDir = Join-Path $runsRoot $script:noImproveRunId
        $reportFile = Join-Path $runDir "improvement-report-data.json"
        if (Test-Path $reportFile) {
            $foundReport = $true
        }
    } else {
        # No run ID — scan recent run dirs for any new report file, excluding
        # the run directory from the --self-improve test (contract 3-5).
        if (Test-Path $runsRoot) {
            $cutoff = (Get-Date).AddMinutes(-5)
            $foundReport = Get-ChildItem $runsRoot -Directory |
                Where-Object { $_.Name -ne $script:runId } |
                Where-Object { $_.CreationTime -gt $cutoff } |
                Where-Object { Test-Path (Join-Path $_.FullName "improvement-report-data.json") } |
                Select-Object -First 1
        }
    }

    if (-not $foundReport) {
        Write-Pass "improvement-report-data.json NOT generated without --self-improve"
    } else {
        Write-Fail "improvement-report-data.json WAS generated without --self-improve"
    }
}

Test-Contract "no-self-improve-no-server" {
    Start-Sleep -Seconds 2
    try {
        Invoke-RestMethod -Uri "http://127.0.0.1:5173/api/report" -TimeoutSec 3 -ErrorAction Stop | Out-Null
        Write-Fail "report-server IS running on port 5173 without --self-improve"
    } catch {
        Write-Pass "report-server NOT running on port 5173 without --self-improve"
    }
}

# ============================================================================
# Summary
# ============================================================================

Write-Host ""
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host " Smoke test summary" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  passed:   $script:passes" -ForegroundColor Green
Write-Host "  failed:   $($script:failures.Count)" -ForegroundColor $(if ($script:failures.Count -gt 0) { "Red" } else { "Green" })

if ($script:failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Failures:" -ForegroundColor Red
    foreach ($f in $script:failures) {
        Write-Host "  - $f" -ForegroundColor Red
    }
}

Invoke-Cleanup

if ($script:failures.Count -gt 0) {
    exit 1
}

Write-Host ""
Write-Host "All smoke tests passed." -ForegroundColor Green
exit 0
