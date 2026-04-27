param(
    [string]$ViewsRoot = "d:\otelturizm\Views",
    [string]$OutFile = (Join-Path $PSScriptRoot "hero-img-lazy-audit.txt")
)

$ErrorActionPreference = "Stop"

$files = Get-ChildItem -LiteralPath $ViewsRoot -Recurse -Filter *.cshtml -File
$hits = New-Object 'System.Collections.Generic.List[object]'

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw

    # Basit heuristik: "hero" sınıfı geçen img tag'lerinde loading attr yoksa raporla.
    foreach ($m in [regex]::Matches($content, '<img[^>]*class\s*=\s*"[^"]*hero[^"]*"[^>]*>', 'IgnoreCase')) {
        $tag = $m.Value
        if (-not [regex]::IsMatch($tag, '\bloading\s*=', 'IgnoreCase')) {
            $preview = ($tag -replace '\s+', ' ')
            if ($preview.Length -gt 240) { $preview = $preview.Substring(0, 240) + "…" }
            $hits.Add([pscustomobject]@{ File=$file.FullName; Tag=$preview })
        }
    }
}

$header = @(
    "# Hero img lazy-loading audit"
    "# Generated at: $(Get-Date -Format o)"
    "# ViewsRoot: $ViewsRoot"
    ""
)
$lines = $hits | Sort-Object File | ForEach-Object { ("{0}`t{1}" -f $_.File, $_.Tag) }

$header + $lines | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Host "Hero img hits (missing loading=...): $($hits.Count)"
Write-Host "Wrote: $OutFile"

