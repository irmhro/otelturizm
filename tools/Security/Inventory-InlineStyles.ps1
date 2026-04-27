param(
    [string]$ViewsRoot = "d:\otelturizm\Views",
    [string]$OutFile = (Join-Path $PSScriptRoot "inline-style-inventory.txt")
)

$ErrorActionPreference = "Stop"

function Add-Hit([System.Collections.Generic.List[object]]$hits, [string]$file, [string]$kind, [string]$preview) {
    if ([string]::IsNullOrWhiteSpace($preview)) { return }
    $p = $preview -replace '\s+', ' '
    if ($p.Length -gt 220) { $p = $p.Substring(0, 220) + "…" }
    $hits.Add([pscustomobject]@{
        File = $file
        Kind = $kind
        Preview = $p
    })
}

$files = Get-ChildItem -LiteralPath $ViewsRoot -Recurse -Filter *.cshtml -File
$hits = New-Object 'System.Collections.Generic.List[object]'

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw

    foreach ($m in [regex]::Matches($content, '<style[^>]*>(?<body>[\s\S]*?)</style>', 'IgnoreCase')) {
        $body = $m.Groups["body"].Value
        if ($null -eq $body) { $body = "" }
        $body = $body.Trim()
        if ($body.Length -eq 0) { continue }
        Add-Hit $hits $file.FullName "style-tag" $body
    }

    foreach ($m in [regex]::Matches($content, '\sstyle\s*=\s*"(?<s>[^"]+)"', 'IgnoreCase')) {
        $s = $m.Groups["s"].Value
        if ($null -eq $s) { $s = "" }
        $s = $s.Trim()
        if ($s.Length -eq 0) { continue }
        Add-Hit $hits $file.FullName "style-attr" $s
    }
}

$header = @(
    "# Inline style inventory"
    "# Generated at: $(Get-Date -Format o)"
    "# ViewsRoot: $ViewsRoot"
    ""
)

$lines = $hits |
    Sort-Object File, Kind |
    ForEach-Object { ("{0}`t{1}`t{2}" -f $_.File, $_.Kind, $_.Preview) }

$header + $lines | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Host "Inline style hits: $($hits.Count)"
Write-Host "Wrote: $OutFile"

