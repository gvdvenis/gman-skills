#!/usr/bin/env bash
#
# Non-blocking preflight check for gman-skills dependencies.
# Warns when the dotnet-blazor plugin or report-server binary is missing.
# Always exits 0 — never blocks.
#
set -uo pipefail

BINARY_DIR="$HOME/.copilot/gman-skills/bin"
BINARY_PATH="$BINARY_DIR/report-server"
all_good=1

# --- Check dotnet-blazor plugin ---------------------------------------------
plugin_list=""
plugin_list=$(copilot plugin list 2>&1) || plugin_list=""

if echo "$plugin_list" | grep -q "dotnet-blazor"; then
    echo "[OK] dotnet-blazor plugin is installed."
else
    echo "[WARN] dotnet-blazor plugin is NOT installed."
    echo "       Install it: copilot plugin marketplace add dotnet/skills && copilot plugin install dotnet-blazor@dotnet-agent-skills"
    all_good=0
fi

# --- Check report-server binary ---------------------------------------------
if [ -x "$BINARY_PATH" ]; then
    echo "[OK] report-server binary found at: $BINARY_PATH"
else
    echo "[WARN] report-server binary NOT found at: $BINARY_PATH"
    echo "       Run: scripts/setup-report-server.sh"
    all_good=0
fi

# --- Summary ----------------------------------------------------------------
if [ "$all_good" -eq 1 ]; then
    echo ""
    echo "All gman-skills dependencies are satisfied."
else
    echo ""
    echo "Some dependencies are missing — see warnings above."
fi

exit 0
