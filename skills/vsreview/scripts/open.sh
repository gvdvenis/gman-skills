#!/usr/bin/env bash
# Open a Firstmate crewmate task's branch (or a scout's report) in VS Code and print what to review.
# Usage: open.sh [<task-id-or-part-of-it>] [--no-open]
# Reads Firstmate's records (state/<id>.meta, state/<id>.status, data/<id>/report.md,
# data/backlog.md); never creates or changes anything in the Firstmate home.
# Exit codes: 0 ok, 2 task unclear, unknown or ambiguous, 3 `code` missing but found on the
# Windows host, 4 VS Code not installed, 5 the task has nothing to open, 6 Firstmate not found.
set -u

FIRSTMATE_URL="https://github.com/kunchenguid/firstmate"

is_fm_home() { [ -d "$1/state" ] && [ -f "$1/bin/fm-session-start.sh" ]; }

find_fm_home() {
  if [ -n "${FM_HOME:-}" ]; then
    is_fm_home "$FM_HOME" && { echo "$FM_HOME"; return 0; }
    return 1
  fi
  local dir
  dir=$PWD
  while :; do
    is_fm_home "$dir" && { echo "$dir"; return 0; }
    [ "$dir" = "/" ] && break
    dir=$(dirname "$dir")
  done
  is_fm_home "$HOME/firstmate" && { echo "$HOME/firstmate"; return 0; }
  return 1
}

if ! FM_HOME=$(find_fm_home); then
  echo "This skill needs Firstmate, and no Firstmate home was found."
  echo "Get it at $FIRSTMATE_URL"
  echo "If it is installed somewhere else, run again with FM_HOME set, for example:"
  echo "  FM_HOME=/path/to/firstmate $0"
  exit 6
fi
STATE="$FM_HOME/state"
BACKLOG="$FM_HOME/data/backlog.md"

ID=""
OPEN=1
for arg in "$@"; do
  case "$arg" in
    --no-open) OPEN=0 ;;
    -*) echo "unknown flag: $arg" >&2; exit 2 ;;
    *) ID=$arg ;;
  esac
done

meta_get() { sed -n "s/^$2=//p" "$STATE/$1.meta" | head -1; }

last_status() { tail -n 1 "$STATE/$1.status" 2>/dev/null; }

is_finished() { case "$(last_status "$1")" in done*) return 0 ;; *) return 1 ;; esac; }

# One-line title from the backlog entry "- [ ] <id> - <title> (repo: ...)"; empty if none.
title_of() {
  [ -f "$BACKLOG" ] || return 0
  local line
  line=$(grep -F -m1 -e "] $1 - " "$BACKLOG" 2>/dev/null) || return 0
  line=${line#*"] $1 - "}
  # strip trailing "(repo: ...) (kind: ...)" parentheticals
  line=$(printf '%s' "$line" | sed 's/\( *([a-z]*: [^)]*)\)*$//')
  printf '%s' "$line"
}

list_candidates() {
  local heading=$1 id title
  shift
  echo "$heading" >&2
  for id in "$@"; do
    title=$(title_of "$id")
    if is_finished "$id"; then
      echo "  $id [finished]${title:+ - $title}" >&2
    else
      echo "  $id${title:+ - $title}" >&2
    fi
  done
}

# All ship and scout task ids, finished ones first.
all_ids() {
  local m id
  local done_ids=() other_ids=()
  for m in "$STATE"/*.meta; do
    [ -e "$m" ] || continue
    id=$(basename "$m" .meta)
    case "$(meta_get "$id" kind)" in ship | scout) ;; *) continue ;; esac
    if is_finished "$id"; then done_ids+=("$id"); else other_ids+=("$id"); fi
  done
  printf '%s\n' "${done_ids[@]+"${done_ids[@]}"}" "${other_ids[@]+"${other_ids[@]}"}" | sed '/^$/d'
}

mapfile -t IDS < <(all_ids)

if [ -z "$ID" ]; then
  finished=()
  for id in "${IDS[@]+"${IDS[@]}"}"; do is_finished "$id" && finished+=("$id"); done
  if [ "${#finished[@]}" -eq 1 ]; then
    ID=${finished[0]}
  elif [ "${#finished[@]}" -gt 1 ]; then
    list_candidates "Which task? Finished tasks right now:" "${finished[@]}"
    exit 2
  else
    list_candidates "Which task? No task is finished right now. Known tasks:" "${IDS[@]+"${IDS[@]}"}"
    exit 2
  fi
fi

if [ ! -f "$STATE/$ID.meta" ]; then
  needle=$(printf '%s' "$ID" | tr '[:upper:]' '[:lower:]')
  matches=()
  for id in "${IDS[@]+"${IDS[@]}"}"; do
    lower=$(printf '%s' "$id" | tr '[:upper:]' '[:lower:]')
    case "$lower" in *"$needle"*) matches+=("$id") ;; esac
  done
  if [ "${#matches[@]}" -eq 1 ]; then
    ID=${matches[0]}
  elif [ "${#matches[@]}" -gt 1 ]; then
    list_candidates "More than one task matches '$ID':" "${matches[@]}"
    exit 2
  else
    list_candidates "No task matches '$ID'. Known tasks:" "${IDS[@]+"${IDS[@]}"}"
    exit 2
  fi
fi

KIND=$(meta_get "$ID" kind)
WT=$(meta_get "$ID" worktree)
BRANCH=$(meta_get "$ID" branch)

check_code() {
  command -v code >/dev/null 2>&1 && return 0
  # VSREVIEW_PROC_VERSION and VSREVIEW_WIN_C exist so tests can fake a WSL host.
  if grep -qi microsoft "${VSREVIEW_PROC_VERSION:-/proc/version}" 2>/dev/null; then
    local p dir winc=${VSREVIEW_WIN_C:-/mnt/c}
    for p in "$winc/Program Files/Microsoft VS Code/bin/code" \
      "$winc"/Users/*/AppData/Local/Programs/Microsoft\ VS\ Code/bin/code; do
      if [ -x "$p" ]; then
        dir=$(dirname "$p")
        echo "The 'code' command is not on your PATH, but VS Code is installed on Windows at:"
        echo "  $dir"
        echo "Add it by putting this line in ~/.bashrc, then open a new shell:"
        echo "  export PATH=\"\$PATH:$dir\""
        echo "(If Windows PATH is not shared with WSL, check that appendWindowsPath is not disabled in /etc/wsl.conf.)"
        return 3
      fi
    done
    echo "VS Code was not found on the Windows host. Install it on Windows first:"
    echo "  https://code.visualstudio.com/  (tick 'Add to PATH' in the installer)"
    echo "then install its 'WSL' extension and run this again."
    return 4
  fi
  echo "The 'code' command is not on your PATH and VS Code does not seem to be installed."
  echo "Install it from https://code.visualstudio.com/ and run this again."
  return 4
}

if [ "$KIND" = "scout" ]; then
  REPORT="$FM_HOME/data/$ID/report.md"
  [ -f "$REPORT" ] || { echo "task $ID has no report to open" >&2; exit 5; }
  echo "TASK: $ID (scout)"
  echo "REPORT: $REPORT"
  if [ "$OPEN" -eq 1 ]; then
    check_code || exit $?
    nohup code "$REPORT" >/dev/null 2>&1 &
    echo "OPENED: report in VS Code"
  fi
  exit 0
fi

[ -n "$WT" ] && [ -d "$WT" ] || { echo "task $ID has no local copy to open (cleaned up already?)" >&2; exit 5; }
BASE=$(git -C "$WT" rev-parse --abbrev-ref '@{upstream}' 2>/dev/null || echo origin/HEAD)
echo "TASK: $ID ($KIND)"
echo "BRANCH: ${BRANCH:-$(git -C "$WT" branch --show-current)}"
echo "FOLDER: $WT"
echo "COMPARED WITH: $BASE"
echo "COMMITS:"
git -C "$WT" log --format='  %h %s' "$BASE..HEAD"
echo "CHANGED FILES (most changed first):"
git -C "$WT" diff --numstat "$BASE" | awk '{printf "%6d %s\n", $1+$2, $3}' | sort -rn | sed 's/^/  /'
[ -f "$FM_HOME/data/$ID/report.md" ] && echo "WORKER REPORT: $FM_HOME/data/$ID/report.md"
echo "TEST FILES TOUCHED:"
git -C "$WT" diff --name-only "$BASE" | grep -i 'test' | sed 's/^/  /' || true
echo "DIFF COMMAND: git -C \"$WT\" diff $BASE"

if [ "$OPEN" -eq 1 ]; then
  check_code || exit $?
  nohup code "$WT" >/dev/null 2>&1 &
  echo "OPENED: folder in VS Code"
fi
