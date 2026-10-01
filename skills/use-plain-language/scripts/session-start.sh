#!/bin/sh
# Session-start hook: hands the use-plain-language rules to the agent.
# Usage: session-start.sh claude|copilot
dir=$(cd "$(dirname "$0")" && pwd)
body=$(awk 'n>=2 {print} /^---[[:space:]]*$/ {n++}' "$dir/../SKILL.md")

if [ "$1" = "copilot" ]; then
  printf '%s\n' "$body" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/\t/\\t/g' |
    awk 'BEGIN { printf "{\"additionalContext\":\"" } { printf "%s%s", (NR > 1 ? "\\n" : ""), $0 } END { print "\"}" }'
else
  printf '%s\n' "$body"
fi
