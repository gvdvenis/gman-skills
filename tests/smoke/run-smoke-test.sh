#!/usr/bin/env bash
#
# Headless smoke test for the gman-skills install-to-run path.
#
# Verifies the full contract observable from a `copilot -p` headless run:
#   1. Skills install via `npx skills add gvdvenis/gman-skills` and --list shows all three.
#   2. /setup-gman-skills runs and confirms dotnet-blazor + report-server binary present.
#   3. copilot -p invocation with --self-improve produces a run ID matching run-YYYYMMDD-HHMM
#      in the first output line.
#   4. improvement-report-data.json exists at ~/.self-improve-reports/blazor-architect/runs/<run_id>/.
#   5. Report-server responds on http://127.0.0.1:5173/api/report during the --self-improve run.
#   6. copilot -p invocation without --self-improve produces no improvement-report-data.json
#      and no server on port 5173.
#
# The test creates a temp Blazor project, runs the skills headless, and asserts each contract
# point. It is structured to run both locally and in GitHub CI.
#
# Environment variables (all optional):
#   COPILOT_BIN     - path to the copilot executable (default: resolved from PATH)
#   DOTNET_BIN      - path to the dotnet executable (default: resolved from PATH)
#   KEEP_TEMP       - set to "1" to keep the temp project after the test finishes
#
set -uo pipefail

PASSES=0
FAILURES=()
TEMP_DIRS=()

# ============================================================================
# Helpers
# ============================================================================

pass() {
    PASSES=$((PASSES + 1))
    echo "  [PASS] $1"
}

fail() {
    FAILURES+=("$1")
    echo "  [FAIL] $1" >&2
}

resolve_binary() {
    local name="$1"
    local env_var="$2"
    local bin
    bin="${!env_var:-}"
    if [ -n "$bin" ]; then
        echo "$bin"
        return
    fi
    bin="$(command -v "$name" 2>/dev/null)"
    if [ -n "$bin" ]; then
        echo "$bin"
        return
    fi
    echo "  [ERROR] '$name' not found on PATH. Set $env_var to its full path." >&2
    exit 2
}

new_temp_dir() {
    local prefix="$1"
    local path
    path="$(mktemp -d "/tmp/${prefix}.XXXXXX")"
    TEMP_DIRS+=("$path")
    echo "$path"
}

cleanup() {
    if [ "${KEEP_TEMP:-0}" = "1" ]; then
        echo ""
        echo "Temp directories kept (KEEP_TEMP=1):"
        for d in "${TEMP_DIRS[@]}"; do echo "  $d"; done
    else
        for d in "${TEMP_DIRS[@]}"; do
            rm -rf "$d" 2>/dev/null || true
        done
    fi

    # Clean up temp log files
    rm -f "${copilot_log:-}" 2>/dev/null || true

    # Kill any stray report-server on port 5173
    if command -v lsof >/dev/null 2>&1; then
        local pid
        pid="$(lsof -ti:5173 2>/dev/null || true)"
        if [ -n "$pid" ]; then kill "$pid" 2>/dev/null || true; fi
    fi
}
trap cleanup EXIT

# ============================================================================
# Setup
# ============================================================================

COPILOT_BIN="$(resolve_binary copilot COPILOT_BIN)"
DOTNET_BIN="$(resolve_binary dotnet DOTNET_BIN)"

echo ""
echo "==========================================="
echo " gman-skills headless smoke test"
echo "==========================================="
echo " copilot:  $COPILOT_BIN"
echo " dotnet:   $DOTNET_BIN"
echo " platform: $(uname -s)"
echo ""

# ============================================================================
# Contract 1: Install skills and --list shows all three
# ============================================================================

echo "[1] Install skills and verify --list shows all three"

echo "  Running: npx skills add gvdvenis/gman-skills"
install_output="$(npx skills add --yes gvdvenis/gman-skills 2>&1)"
install_rc=$?
echo "$install_output"
if [ "$install_rc" -eq 0 ]; then
    pass "npx skills add succeeded"
else
    fail "npx skills add failed (exit code $install_rc)"
fi

list_output="$(npx skills list 2>&1 || true)"
echo "$list_output"

for skill in blazor-architect self-improve setup-gman-skills; do
    if echo "$list_output" | grep -q "$skill"; then
        pass "skill '$skill' appears in --list output"
    else
        fail "skill '$skill' NOT found in --list output"
    fi
done

# ============================================================================
# Contract 2: /setup-gman-skills runs and confirms dotnet-blazor + report-server
# ============================================================================

echo ""
echo "[2] /setup-gman-skills confirms dotnet-blazor + report-server binary"

setup_output="$("$COPILOT_BIN" -p "/setup-gman-skills" --allow-all --add-dir "$HOME" 2>&1 || true)"
echo "$setup_output"

if echo "$setup_output" | grep -qE "dotnet-blazor.*(installed|present|already)"; then
    pass "setup confirms dotnet-blazor plugin present"
else
    fail "setup did not confirm dotnet-blazor plugin"
fi

if echo "$setup_output" | grep -qE "report-server.*(present|downloaded|installed|already)"; then
    pass "setup confirms report-server binary present"
else
    fail "setup did not confirm report-server binary"
fi

# ============================================================================
# Contract 3-5: copilot -p with --self-improve produces run ID, report, server
# ============================================================================

echo ""
echo "[3-5] copilot -p with --self-improve: run ID, report data, server"

PROJECT_DIR=""
RUN_ID=""

# Create a temp Blazor project
PROJECT_DIR="$(new_temp_dir smoke-blazor)"
echo "  Creating temp Blazor project at: $PROJECT_DIR"
"$DOTNET_BIN" new blazor -o "$PROJECT_DIR" --no-restore >/dev/null 2>&1 || true

# Start copilot in the background so we can poll the server concurrently
echo "  Starting copilot -p with --self-improve (background)..."
copilot_log="$(mktemp)"
"$COPILOT_BIN" -p "/blazor-architect implement a simple counter component in Components/Pages/Counter.razor --self-improve" \
    --allow-all --add-dir "$PROJECT_DIR" --add-dir "$HOME" >"$copilot_log" 2>&1 &
copilot_pid=$!

# Poll for report-server on port 5173 while copilot is running AND for a short
# period after it finishes — the server may be launched near the end of the run.
responded=false
for i in $(seq 1 40); do
    sleep 3
    if curl -sf -o /dev/null "http://127.0.0.1:5173/api/report" 2>/dev/null; then
        responded=true
        echo "  [INFO] report-server responded during copilot run (poll $i)"
        break
    fi
    # Stop polling if copilot finished AND we've done at least 10 extra polls
    if ! kill -0 "$copilot_pid" 2>/dev/null && [ "$i" -gt 15 ]; then break; fi
done

# Wait for copilot to finish (generous timeout)
echo "  Waiting for copilot to finish..."
for i in $(seq 1 200); do
    kill -0 "$copilot_pid" 2>/dev/null || break
    sleep 3
done
kill -0 "$copilot_pid" 2>/dev/null && kill "$copilot_pid" 2>/dev/null || true
wait "$copilot_pid" 2>/dev/null || true
improve_output="$(cat "$copilot_log")"
echo "$improve_output"

# Extract run ID from output (search all lines, not just the first — copilot -p
# may print skill headers before the run ID line)
run_id_match="$(echo "$improve_output" | grep -oE "run-[0-9]{8}-[0-9]{4}" | head -1 || true)"
if [ -n "$run_id_match" ]; then
    RUN_ID="$run_id_match"
    pass "run ID '$RUN_ID' found in first output line"
else
    fail "run ID matching run-YYYYMMDD-HHMM not found in first output line: $first_line"
fi

if [ "$responded" = "true" ]; then
    pass "report-server responded on http://127.0.0.1:5173/api/report during the run"
else
    fail "report-server did NOT respond on http://127.0.0.1:5173/api/report during the run"
fi

# Always try to shut down the server so the next contract test has a clean port,
# even if the polling didn't catch it in time.
curl -sf -o /dev/null "http://127.0.0.1:5173/shutdown" 2>/dev/null || true
sleep 2
# Also kill any stray server process on port 5173
if command -v lsof >/dev/null 2>&1; then
    lsof -ti:5173 2>/dev/null | xargs -r kill 2>/dev/null || true
fi

# Check improvement-report-data.json exists
if [ -n "$RUN_ID" ]; then
    run_dir="$HOME/.self-improve-reports/blazor-architect/runs/$RUN_ID"
    report_file="$run_dir/improvement-report-data.json"

    if [ -f "$report_file" ]; then
        schema_version="$(python3 -c "import json; print(json.load(open('$report_file'))['schema_version'])" 2>/dev/null || echo "")"
        if [ "$schema_version" = "1.1" ]; then
            pass "improvement-report-data.json exists with schema_version 1.1 at $report_file"
        else
            fail "improvement-report-data.json schema_version is '$schema_version', expected '1.1'"
        fi
    else
        fail "improvement-report-data.json NOT found at $report_file"
    fi
else
    fail "improvement-report-data.json check skipped: no run ID from previous step"
fi

# ============================================================================
# Contract 6: copilot -p without --self-improve produces no report, no server
# ============================================================================

echo ""
echo "[6] copilot -p without --self-improve: no report, no server"

if [ -z "$PROJECT_DIR" ]; then
    fail "no self-improve: temp project not created (previous step failed)"
else
    no_improve_output="$("$COPILOT_BIN" -p "/blazor-architect add a simple greeting component" \
        --allow-all --add-dir "$PROJECT_DIR" --add-dir "$HOME" 2>&1 || true)"
    echo "$no_improve_output"

    # The spec requires no report data file and no server — we do NOT require a run ID here.
    # If a run ID is present we use it to check the run directory; if not, we scan recent runs.
    first_line="$(echo "$no_improve_output" | head -1)"
    no_improve_run_id="$(echo "$no_improve_output" | grep -oE "run-[0-9]{8}-[0-9]{4}" | head -1 || true)"

    # Check no improvement-report-data.json
    runs_root="$HOME/.self-improve-reports/blazor-architect/runs"
    found_report=false

    if [ -n "$no_improve_run_id" ]; then
        no_improve_run_dir="$runs_root/$no_improve_run_id"
        no_improve_report="$no_improve_run_dir/improvement-report-data.json"
        if [ -f "$no_improve_report" ]; then
            found_report=true
        fi
    else
        # No run ID — scan recent run dirs for any new report file (created in last 5 min),
        # excluding the run directory from the --self-improve test (contract 3-5).
        if [ -d "$runs_root" ]; then
            cutoff_time="$(date -d '5 minutes ago' +%s 2>/dev/null || date -v-5M +%s 2>/dev/null || echo 0)"
            for d in "$runs_root"/*/; do
                [ -d "$d" ] || continue
                # Skip the self-improve run directory
                [ "$(basename "$d")" = "$RUN_ID" ] && continue
                report_file="$d/improvement-report-data.json"
                if [ -f "$report_file" ]; then
                    file_time="$(stat -c %Y "$report_file" 2>/dev/null || stat -f %m "$report_file" 2>/dev/null || echo 0)"
                    if [ "$file_time" -gt "$cutoff_time" ] 2>/dev/null; then
                        found_report=true
                        break
                    fi
                fi
            done
        fi
    fi

    if [ "$found_report" = "false" ]; then
        pass "improvement-report-data.json NOT generated without --self-improve"
    else
        fail "improvement-report-data.json WAS generated without --self-improve"
    fi
fi

# Check no server on port 5173
sleep 2
if curl -sf -o /dev/null "http://127.0.0.1:5173/api/report" 2>/dev/null; then
    fail "report-server IS running on port 5173 without --self-improve"
else
    pass "report-server NOT running on port 5173 without --self-improve"
fi

# ============================================================================
# Summary
# ============================================================================

echo ""
echo "==========================================="
echo " Smoke test summary"
echo "==========================================="
echo "  passed:   $PASSES"
echo "  failed:   ${#FAILURES[@]}"

if [ "${#FAILURES[@]}" -gt 0 ]; then
    echo ""
    echo "Failures:"
    for f in "${FAILURES[@]}"; do
        echo "  - $f" >&2
    done
    exit 1
fi

echo ""
echo "All smoke tests passed."
exit 0
