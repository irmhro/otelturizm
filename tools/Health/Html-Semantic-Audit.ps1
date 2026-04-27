param(
    [string]$ViewsRoot = "d:\otelturizm\Views",
    [string]$OutFile = (Join-Path $PSScriptRoot "html-semantic-audit.txt")
)

$ErrorActionPreference = "Stop"

$files = Get-ChildItem -LiteralPath $ViewsRoot -Recurse -Filter *.cshtml -File
$issues = New-Object System.Collections.Generic.List[string]

foreach ($f in $files) {
    $text = Get-Content -LiteralPath $f.FullName -Raw

    # duplicate id="..."
    $ids = @{}
    foreach ($m in [regex]::Matches($text, '\bid\s*=\s*"(?<id>[^"]+)"', 'IgnoreCase')) {
        $id = $m.Groups["id"].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($id)) { continue }
        if (-not $ids.ContainsKey($id)) { $ids[$id] = 0 }
        $ids[$id] = $ids[$id] + 1
    }
    foreach ($k in $ids.Keys) {
        if ($ids[$k] -gt 1) {
            $issues.Add("$($f.FullName) :: DUPLICATE_ID :: $k :: count=$($ids[$k])")
        }
    }

    # <img> missing alt (ignore Razor partials with dynamic)
    foreach ($m in [regex]::Matches($text, '<img\b(?<attrs>[^>]+)>', 'IgnoreCase')) {
        $attrs = $m.Groups["attrs"].Value
        if ($attrs -match '\balt\s*=\s*"') { continue }
        if ($attrs -match '\balt\s*=\s*@') { continue }
        $issues.Add("$($f.FullName) :: IMG_MISSING_ALT")
    }
}

$header = @(
    "# HTML semantic audit (basic)",
    "# Generated at: $(Get-Date -Format o)",
    "# Checks: duplicate id, img missing alt",
    ""
)

$header + ($issues | Sort-Object) | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Host "Issues: $($issues.Count)"
Write-Host "Wrote: $OutFile"

if ($issues.Count -gt 0) { exit 2 }

