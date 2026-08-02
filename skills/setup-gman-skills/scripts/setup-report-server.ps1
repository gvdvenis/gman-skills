#Requires -Version 5.1
<#
.SYNOPSIS
    Downloads and installs the report-server binary from GitHub Releases.
.DESCRIPTION
    Idempotent: skips download if the binary already exists.
    Queries https://api.github.com/repos/gvdvenis/gman-skills/releases/latest,
    downloads the win-x64 or win-arm64 zip, and extracts to
    %USERPROFILE%\.copilot\gman-skills\bin.
#>
$ErrorActionPreference = "Stop"

$Repo        = "gvdvenis/gman-skills"
$InstallDir  = Join-Path $env:USERPROFILE ".copilot\gman-skills\bin"
$BinaryName  = "report-server.exe"
$BinaryPath  = Join-Path $InstallDir $BinaryName
$ApiUrl      = "https://api.github.com/repos/$Repo/releases/latest"

# --- Idempotency check -------------------------------------------------------
if (Test-Path $BinaryPath) {
    Write-Host "report-server binary already exists at: $BinaryPath"
    Write-Host "Skipping download."
    exit 0
}

# --- Detect architecture -----------------------------------------------------
$arch = switch ($env:PROCESSOR_ARCHITECTURE) {
    "AMD64" { "x64"; break }
    "ARM64" { "arm64"; break }
    default {
        Write-Error "Unsupported architecture: $env:PROCESSOR_ARCHITECTURE"
        exit 1
    }
}

# --- Query GitHub Releases API ----------------------------------------------
Write-Host "Querying latest release from $ApiUrl ..."
try {
    $response = Invoke-RestMethod -Uri $ApiUrl -Headers @{ "User-Agent" = "gman-skills-setup" } -ErrorAction Stop
} catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "ERROR: No release found for $Repo."
        Write-Host "Build the report-server from source:"
        Write-Host "  cd src\report-server && dotnet publish -c Release -r win-$arch"
        exit 1
    }
    Write-Error "Failed to query GitHub API: $_"
    exit 1
}

# --- Find the matching asset ------------------------------------------------
$assetPattern = "report-server-win-$arch.zip"
$asset = $response.assets | Where-Object { $_.name -eq $assetPattern } | Select-Object -First 1

if (-not $asset) {
    Write-Host "ERROR: No asset matching '$assetPattern' found in latest release."
    Write-Host "Available assets:"
    $response.assets | ForEach-Object { Write-Host "  $($_.name)" }
    Write-Host "Build the report-server from source:"
    Write-Host "  cd src\report-server && dotnet publish -c Release -r win-$arch"
    exit 1
}

# --- Download ---------------------------------------------------------------
$downloadUrl = $asset.browser_download_url
$tempZip = Join-Path $env:TEMP "report-server-win-$arch.zip"

Write-Host "Downloading $downloadUrl ..."
Invoke-WebRequest -Uri $downloadUrl -OutFile $tempZip -UseBasicParsing

# --- Extract ----------------------------------------------------------------
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

Write-Host "Extracting to $InstallDir ..."
Expand-Archive -Path $tempZip -DestinationPath $InstallDir -Force

Remove-Item $tempZip -Force

# --- Handle nested folder layout -------------------------------------------
# Release zips sometimes nest the binary under a top-level folder. If the binary
# is not at the expected root path, search for it and move it into place.
if (-not (Test-Path $BinaryPath)) {
    $found = Get-ChildItem -Path $InstallDir -Recurse -Filter $BinaryName | Select-Object -First 1
    if ($found) {
        Write-Host "Binary found in subfolder '$($found.DirectoryName)', moving to $InstallDir ..."
        Move-Item -Path $found.FullName -Destination $BinaryPath -Force
        # Clean up any empty subdirectories left behind
        Get-ChildItem -Path $InstallDir -Directory | Where-Object { -not (Get-ChildItem $_.FullName -Recurse) } | Remove-Item -Force -Recurse
    }
}

# --- Verify -----------------------------------------------------------------
if (Test-Path $BinaryPath) {
    Write-Host "Verifying binary ..."
    try {
        & $BinaryPath --version 2>$null
    } catch {
        Write-Host "Binary exists at $BinaryPath (--version not supported, skipping)."
    }
    Write-Host ""
    Write-Host "report-server installed at: $BinaryPath"
} else {
    Write-Host "ERROR: Binary not found at expected path after extraction: $BinaryPath"
    Write-Host "Contents of $InstallDir :"
    Get-ChildItem $InstallDir -Recurse | ForEach-Object { Write-Host "  $($_.FullName)" }
    exit 1
}
