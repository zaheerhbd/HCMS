# Stop hook: reminds Claude to update project docs, but only when it makes sense.
# Input:  hook JSON on stdin (contains stop_hook_active).
# Output: Stop hook JSON with additionalContext, or nothing (exit 0) to let Claude stop.

$raw = [Console]::In.ReadToEnd()
$hookInput = $null
try { $hookInput = $raw | ConvertFrom-Json } catch {}

# Guard 1: Claude is already continuing because of this hook - don't re-trigger (prevents the loop)
if ($hookInput -and $hookInput.stop_hook_active) { exit 0 }

# Guard 2: only remind when source code has uncommitted changes
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$srcChanges = git -C $repoRoot status --porcelain -- src 2>$null
if (-not $srcChanges) { exit 0 }

$message = 'DOCS UPDATE CHECK: Before finishing, review what happened this session. If any tasks were checked off in docs/phase-tracker.md, a phase was completed, or any module features shipped - you MUST update: (1) docs/changelog.md: add a [YYYY-MM-DD] dated bullet under the correct phase section describing what changed and why. (2) docs/project-status.md: tick off completed tasks in the phase checklist, update the phase overview table counts, and note any new blockers or decisions. (3) docs/feature-reference.md: change the Status field from Not Started to In Progress or Complete for any modules that moved. Update the Last Updated date at the top of each file you change. If nothing doc-worthy happened this session, skip all of the above - do not add empty or placeholder entries.'

@{ hookSpecificOutput = @{ hookEventName = 'Stop'; additionalContext = $message } } | ConvertTo-Json -Compress -Depth 5
exit 0
