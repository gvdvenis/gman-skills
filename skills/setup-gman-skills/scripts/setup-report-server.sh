#!/usr/bin/env bash
#
# Downloads and installs the report-server binary from GitHub Releases.
# Idempotent: skips download if the binary already exists.
# Queries https://api.github.com/repos/gvdvenis/gman-skills/releases/latest,
# downloads the matching archive, and extracts to ~/.copilot/gman-skills/bin.
#
set -euo pipefail

REPO="gvdvenis/gman-skills"
INSTALL_DIR="$HOME/.copilot/gman-skills/bin"
BINARY_NAME="report-server"
BINARY_PATH="$INSTALL_DIR/$BINARY_NAME"
API_URL="https://api.github.com/repos/${REPO}/releases/latest"

# --- Idempotency check -------------------------------------------------------
if [ -x "$BINARY_PATH" ]; then
    echo "report-server binary already exists at: $BINARY_PATH"
    echo "Skipping download."
    exit 0
fi

# --- Detect OS and architecture ----------------------------------------------
OS=$(uname -s)
ARCH=$(uname -m)

case "$OS" in
    Darwin) os="osx" ;;
    Linux)  os="linux" ;;
    *)
        echo "ERROR: Unsupported OS: $OS"
        exit 1
        ;;
esac

case "$ARCH" in
    x86_64|amd64) arch="x64" ;;
    aarch64|arm64) arch="arm64" ;;
    *)
        echo "ERROR: Unsupported architecture: $ARCH"
        exit 1
        ;;
esac

# --- Query GitHub Releases API ----------------------------------------------
echo "Querying latest release from $API_URL ..."
api_response=$(curl -fsSL -H "User-Agent: gman-skills-setup" "$API_URL" 2>&1) || {
    echo "ERROR: No release found for $REPO (or failed to reach GitHub API)."
    echo "Build the report-server from source:"
    echo "  cd src/report-server && dotnet publish -c Release -r ${os}-${arch}"
    exit 1
}

# --- Find the matching asset ------------------------------------------------
asset_pattern="report-server-${os}-${arch}.zip"
download_url=$(echo "$api_response" | grep -o '"browser_download_url": *"[^"]*"' | grep "$asset_pattern" | head -1 | sed 's/.*"browser_download_url": *"//;s/"//')

if [ -z "$download_url" ]; then
    echo "WARNING: No asset matching '$asset_pattern' found in latest release."
    echo "Attempting to build from source..."
    REPO_ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
    SRC_DIR="$REPO_ROOT/src/report-server/ReportServer"
    if [ -d "$SRC_DIR" ] && command -v dotnet >/dev/null 2>&1; then
        mkdir -p "$INSTALL_DIR"
        dotnet publish "$SRC_DIR/ReportServer.csproj" \
            -c Release -r "${os}-${arch}" --self-contained true \
            -p:PublishAot=false \
            -o "$INSTALL_DIR" 2>&1 || {
            echo "ERROR: Build from source failed."
            echo "Build the report-server manually:"
            echo "  cd src/report-server && dotnet publish -c Release -r ${os}-${arch}"
            exit 1
        }
        chmod +x "$BINARY_PATH" 2>/dev/null || true
        echo "report-server built from source and installed at: $BINARY_PATH"
        exit 0
    else
        echo "ERROR: Cannot build from source (source directory or dotnet not found)."
        echo "Build the report-server manually:"
        echo "  cd src/report-server && dotnet publish -c Release -r ${os}-${arch}"
        echo "Then copy the binary to $INSTALL_DIR/"
        exit 1
    fi
fi

# --- Download ---------------------------------------------------------------
temp_archive="/tmp/report-server-${os}-${arch}.zip"
echo "Downloading $download_url ..."
curl -fsSL -o "$temp_archive" "$download_url"

# --- Extract ----------------------------------------------------------------
mkdir -p "$INSTALL_DIR"
echo "Extracting to $INSTALL_DIR ..."
if command -v unzip >/dev/null 2>&1; then
    unzip -o "$temp_archive" -d "$INSTALL_DIR"
else
    echo "ERROR: 'unzip' command not found. Please install unzip or extract manually."
    rm -f "$temp_archive"
    exit 1
fi
rm -f "$temp_archive"

# --- Handle nested folder layout -------------------------------------------
# Release zips sometimes nest the binary under a top-level folder. If the binary
# is not at the expected root path, search for it and move it into place.
if [ ! -f "$BINARY_PATH" ]; then
    found_bin=$(find "$INSTALL_DIR" -type f -name "$BINARY_NAME" | head -1)
    if [ -n "$found_bin" ] && [ "$found_bin" != "$BINARY_PATH" ]; then
        echo "Binary found in subfolder, moving to $INSTALL_DIR ..."
        mv -f "$found_bin" "$BINARY_PATH"
        # Clean up empty subdirectories
        find "$INSTALL_DIR" -mindepth 1 -type d -empty -delete 2>/dev/null || true
    fi
fi

# --- Make executable ---------------------------------------------------------
chmod +x "$BINARY_PATH" 2>/dev/null || true

# --- Verify -----------------------------------------------------------------
if [ -x "$BINARY_PATH" ]; then
    echo "Verifying binary ..."
    "$BINARY_PATH" --version 2>/dev/null || echo "Binary exists at $BINARY_PATH (--version not supported, skipping)."
    echo ""
    echo "report-server installed at: $BINARY_PATH"
else
    echo "ERROR: Binary not found at expected path after extraction: $BINARY_PATH"
    echo "Contents of $INSTALL_DIR :"
    find "$INSTALL_DIR" -type f
    exit 1
fi
