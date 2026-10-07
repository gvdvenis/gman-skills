---
name: vsreview
description: Firstmate helper skill that opens a finished crewmate task in VS Code (the ship's local copy on its branch, or the scout's report) and says what is worth reviewing and where to start. Requires Firstmate and the VS Code `code` command. Use when the user invokes /vsreview [task-id], or asks to open a worker's branch, changes or report in VS Code.
user-invocable: true
---

# vsreview

Firstmate helper skill. It reads Firstmate's task records, opens a finished ship's local copy (on its branch) or a scout's report in VS Code, then tells the user what is worth reviewing.

Requires [Firstmate](https://github.com/kunchenguid/firstmate) and VS Code with the `code` command on PATH.

## Steps

1. Run `scripts/open.sh [<task-id>]`, with the path relative to this skill's folder. The argument is what the user typed; it may be a part of an id (`/vsreview mainthread` matches `perf-mainthread-impl-k1`). Without one, the script picks the only finished task.
   Add `--no-open` only to preview without launching VS Code.
2. Read the exit code:
   - 2: the id was missing, ambiguous or unknown. The script listed the candidate tasks (finished first, each with its backlog title). Ask the user which one, then run it again.
   - 3 or 4: the script already printed what to do (put the host's VS Code `bin` folder on PATH, or install VS Code on Windows first). Relay that in plain words and stop; never edit `~/.bashrc` or install anything without being asked.
   - 5: the task has nothing to open (no report, or the local copy is gone). Say so and stop.
   - 6: Firstmate was not found. Tell the user this skill needs Firstmate (https://github.com/kunchenguid/firstmate) and that `FM_HOME=/path/to/firstmate` points it at an install in another place. Stop.
3. On success, give the user a short review guide in plain language:
   - what the change does in one or two sentences, from the commit messages and the worker's report if one is listed;
   - 3 to 5 files in the order to read them: the file where the main logic lives first, then its callers, and the new or changed tests last, taking the changed-files list as the signal for where the weight is;
   - one line on what the tests assert, and one on anything the worker's report flags as unverified or risky;
   - the diff command from the output, so the user can run it in the VS Code terminal.
   Do not paste the script's raw output.
4. End with the pending decision for that task (merge, push, leave) if one is open.

## Notes

- The script finds the Firstmate home from `$FM_HOME`, else the current folder or a parent that has `state/` and `bin/fm-session-start.sh`, else `~/firstmate`.
- The script only reads Firstmate records and git state and launches `code` in the background; it changes nothing in the Firstmate home or the repository.
- A ship's folder is the worker's own local copy: do not touch it while the user reviews, and do not clean it up until the user has decided.
