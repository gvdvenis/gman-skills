# Session-start hook: hands the use-plain-language rules to the agent.
# Usage: session-start.ps1 claude|copilot
param([ValidateSet('claude', 'copilot')][string]$Harness = 'claude')

$skill = Get-Content -Raw -Encoding utf8 (Join-Path $PSScriptRoot '..\SKILL.md')
$body = ($skill -split '(?m)^---\s*$', 3)[2].Trim()

[Console]::OutputEncoding = [Text.Encoding]::UTF8
if ($Harness -eq 'copilot') {
    @{ additionalContext = $body } | ConvertTo-Json -Compress
} else {
    $body
}
