#!/usr/bin/env bash
#
# Shell test for skills/vsreview/scripts/open.sh.
# Uses a fake Firstmate home, a throwaway git repository as the ship worktree, a fake PATH and a
# stub `code`, so nothing real is launched. Run: bash tests/vsreview/run-vsreview-test.sh
# shellcheck disable=SC2319,SC2143  # check() takes the condition's status via $(...; echo $?)
set -uo pipefail

SCRIPT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/skills/vsreview/scripts/open.sh"
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
PASSES=0
FAILS=0

check() { # name, condition result (0 = pass)
  if [ "$2" -eq 0 ]; then PASSES=$((PASSES + 1)); echo "  [PASS] $1"
  else FAILS=$((FAILS + 1)); echo "  [FAIL] $1"; fi
}
has() { grep -qF -- "$2" <<<"$1"; }

# ---- fake Firstmate home -------------------------------------------------
FM="$TMP/firstmate"
mkdir -p "$FM/state" "$FM/bin" "$FM/data"
touch "$FM/bin/fm-session-start.sh"

# ---- throwaway git repository as the ship worktree -----------------------
WT="$TMP/ship-wt"
git init -q -b main "$WT"
git -C "$WT" config user.email t@example.com
git -C "$WT" config user.name t
echo base >"$WT/a.txt"
git -C "$WT" add . && git -C "$WT" commit -q -m base
git -C "$WT" checkout -q -b feature
mkdir "$WT/tests"
printf 'one\ntwo\nthree\n' >"$WT/big.txt"
echo x >"$WT/tests/t.sh"
git -C "$WT" add . && git -C "$WT" commit -q -m "add things"
git -C "$WT" update-ref refs/remotes/origin/main main
git -C "$WT" symbolic-ref refs/remotes/origin/HEAD refs/remotes/origin/main

mk_task() { # id kind status [worktree]
  printf 'kind=%s\nworktree=%s\nbranch=feature\n' "$2" "${4:-}" >"$FM/state/$1.meta"
  echo "$3" >"$FM/state/$1.status"
}
mk_task perf-mainthread-impl-k1 ship "done [at=1]: PR" "$WT"
mk_task perf-verify-scout-i1 scout "done [at=1]: report"
mk_task perf-wasm-scout-f3 scout "working [at=1]: busy"
mkdir -p "$FM/data/perf-verify-scout-i1"
echo "# report" >"$FM/data/perf-verify-scout-i1/report.md"
cat >"$FM/data/backlog.md" <<'B'
# Backlog

## In flight
- [ ] perf-mainthread-impl-k1 - Defer price warm-up in the worker (repo: case-configurator) (kind: ship) (since 2026-10-07)
  Mode local-only.
- [ ] perf-verify-scout-i1 - Verify deployed layout-shift fix (repo: case-configurator) (kind: scout) (since 2026-10-07)
B

# ---- fake PATH: only the tools the script needs, plus an optional stub code
BIN="$TMP/bin"
mkdir -p "$BIN"
for t in bash sh git sed awk grep sort head tail tr basename dirname cat mkdir nohup; do
  src=$(command -v "$t") && ln -s "$src" "$BIN/$t"
done
CODELOG="$TMP/code.log"
put_code() {
  printf '#!/bin/sh\necho "$@" >> "%s"\n' "$CODELOG" >"$BIN/code"
  chmod +x "$BIN/code"
}
drop_code() { rm -f "$BIN/code"; }

run() { # args...; sets OUT (stdout+stderr) and RC
  OUT=$(cd "$TMP" && env -i HOME="$TMP/home" PATH="$BIN" FM_HOME="${RUN_FM-$FM}" \
    VSREVIEW_PROC_VERSION="${PV:-/nonexistent}" VSREVIEW_WIN_C="${WINC:-$TMP/nowin}" \
    "$BIN/bash" "$SCRIPT" "$@" 2>&1)
  RC=$?
}

echo "Firstmate missing"
RUN_FM="$TMP/nowhere" run --no-open
check "exit 6" "$([ "$RC" -eq 6 ]; echo $?)"
check "names Firstmate URL" "$(has "$OUT" "https://github.com/kunchenguid/firstmate"; echo $?)"
check "mentions FM_HOME" "$(has "$OUT" "FM_HOME"; echo $?)"
check "nothing created in missing home" "$([ ! -e "$TMP/nowhere" ]; echo $?)"

echo "Firstmate home found from the current directory"
mkdir -p "$TMP/home"
OUT=$(cd "$FM/state" && env -i HOME="$TMP/home" PATH="$BIN" "$BIN/bash" "$SCRIPT" --no-open perf-mainthread-impl-k1 2>&1); RC=$?
check "found via parent directory" "$([ "$RC" -eq 0 ]; echo $?)"

echo "Id matching"
run --no-open perf-mainthread-impl-k1
check "exact id works" "$([ "$RC" -eq 0 ] && has "$OUT" "TASK: perf-mainthread-impl-k1 (ship)"; echo $?)"
run --no-open MAINthread
check "partial case-insensitive id works" "$([ "$RC" -eq 0 ] && has "$OUT" "TASK: perf-mainthread-impl-k1"; echo $?)"
run --no-open scout
check "ambiguous exits 2" "$([ "$RC" -eq 2 ]; echo $?)"
check "ambiguous lists both with titles" "$(has "$OUT" "perf-verify-scout-i1 [finished] - Verify deployed layout-shift fix" && has "$OUT" "perf-wasm-scout-f3"; echo $?)"
check "finished listed before unfinished" "$([ "$(grep -n 'verify-scout' <<<"$OUT" | head -1 | cut -d: -f1)" -lt "$(grep -n 'wasm-scout' <<<"$OUT" | head -1 | cut -d: -f1)" ]; echo $?)"
check "id without backlog entry shown alone" "$(has "$OUT" "  perf-wasm-scout-f3"; echo $?)"
run --no-open zzz
check "no match exits 2 and lists tasks" "$([ "$RC" -eq 2 ] && has "$OUT" "perf-mainthread-impl-k1"; echo $?)"
run --no-open
check "no id with several finished lists them" "$([ "$RC" -eq 2 ] && has "$OUT" "Finished tasks"; echo $?)"
echo "done [at=1]: x" >"$FM/state/perf-verify-scout-i1.status.bak"
mv "$FM/state/perf-verify-scout-i1.meta" "$TMP/hold.meta"
run --no-open
check "no id picks the only finished task" "$([ "$RC" -eq 0 ] && has "$OUT" "TASK: perf-mainthread-impl-k1"; echo $?)"
mv "$TMP/hold.meta" "$FM/state/perf-verify-scout-i1.meta"
rm "$FM/state/perf-verify-scout-i1.status.bak"
mv "$FM/data/backlog.md" "$TMP/backlog.md"
run --no-open scout
check "missing backlog still lists ids" "$([ "$RC" -eq 2 ] && has "$OUT" "perf-verify-scout-i1"; echo $?)"
mv "$TMP/backlog.md" "$FM/data/backlog.md"

echo "Scout versus ship"
run --no-open perf-verify-scout-i1
check "scout prints report" "$([ "$RC" -eq 0 ] && has "$OUT" "REPORT: $FM/data/perf-verify-scout-i1/report.md"; echo $?)"
run --no-open perf-mainthread-impl-k1
check "ship prints folder and branch" "$(has "$OUT" "FOLDER: $WT" && has "$OUT" "BRANCH: feature"; echo $?)"
check "ship changed files most changed first" "$(grep -A2 'CHANGED FILES' <<<"$OUT" | sed -n 2p | grep -q 'big.txt'; echo $?)"
check "ship lists test files" "$(has "$OUT" "  tests/t.sh"; echo $?)"
check "ship prints diff command" "$(has "$OUT" "DIFF COMMAND: git -C \"$WT\" diff origin/HEAD"; echo $?)"
check "no write to Firstmate home" "$([ -z "$(find "$FM" -newer "$TMP/bin" -type f 2>/dev/null | grep -v 'state/.*\.\(meta\|status\)$\|backlog.md$')" ]; echo $?)"

echo "code outcomes"
put_code
run perf-verify-scout-i1
sleep 0.3
check "on PATH: opens report" "$([ "$RC" -eq 0 ] && has "$OUT" "OPENED" && grep -q "report.md" "$CODELOG"; echo $?)"
: >"$CODELOG"
run perf-mainthread-impl-k1
sleep 0.3
check "on PATH: opens ship folder" "$([ "$RC" -eq 0 ] && grep -qF "$WT" "$CODELOG"; echo $?)"

drop_code
echo "Linux version (microsoft-standard-WSL2)" >"$TMP/proc-wsl"
mkdir -p "$TMP/winc/Program Files/Microsoft VS Code/bin"
printf '#!/bin/sh\n' >"$TMP/winc/Program Files/Microsoft VS Code/bin/code"
chmod +x "$TMP/winc/Program Files/Microsoft VS Code/bin/code"
PV="$TMP/proc-wsl" WINC="$TMP/winc" run perf-verify-scout-i1
check "host install found: exit 3 with PATH hint" "$([ "$RC" -eq 3 ] && has "$OUT" "export PATH=" && has "$OUT" "Microsoft VS Code/bin"; echo $?)"
PV="$TMP/proc-wsl" WINC="$TMP/empty" run perf-verify-scout-i1
check "not installed on WSL: exit 4 install VS Code on Windows" "$([ "$RC" -eq 4 ] && has "$OUT" "Install it on Windows first"; echo $?)"
run perf-verify-scout-i1
check "not installed, not WSL: exit 4" "$([ "$RC" -eq 4 ] && has "$OUT" "Install it from"; echo $?)"
run --no-open perf-verify-scout-i1
check "--no-open skips the code check" "$([ "$RC" -eq 0 ]; echo $?)"

echo
echo "Passed: $PASSES, failed: $FAILS"
[ "$FAILS" -eq 0 ]
