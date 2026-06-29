# Auto-updates .phase-reports-index.md whenever Claude writes a phase-N-*.html file.
# Invoked by .claude/settings.json PostToolUse hook on Write tool calls.
# Reads tool input JSON from stdin.

$raw = [Console]::In.ReadToEnd()
try { $obj = $raw | ConvertFrom-Json } catch { exit 0 }
$fp = $obj.tool_input.file_path
if (-not $fp) { exit 0 }

# Only act on docs/phase-*.html files
$fname = Split-Path $fp -Leaf
if ($fname -notmatch '^phase-\d+.*\.html$') { exit 0 }

# Locate the index file (repo root / .phase-reports-index.md)
$repoRoot = Split-Path (Split-Path $fp -Parent) -Parent
$indexPath = Join-Path $repoRoot '.phase-reports-index.md'
if (-not (Test-Path $indexPath)) { exit 0 }

# Build the new table row
$relPath = 'docs/' + $fname
$phase   = if ($fname -match 'phase-(\d+)') { $Matches[1] } else { '?' }
$summary = if ($fname -match 'phase-\d+-([^.]+)') { ($Matches[1] -replace '-', ' ') } else { $fname }
$ts      = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$row     = "| $ts | $phase | $fname | $summary | [View]($relPath) |"

# Append only if not already present
$content = Get-Content $indexPath -Raw -Encoding utf8
if ($content -notmatch [regex]::Escape($fname)) {
    $lines = $content.TrimEnd() -split "`n"
    $lines += $row
    Set-Content $indexPath ($lines -join "`n") -NoNewline -Encoding utf8
}

exit 0
