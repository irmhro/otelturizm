param(
    [string]$ViewsRoot = "d:\otelturizm\Views",
    [string]$OutFile = (Join-Path $PSScriptRoot "inline-script-inventory.txt")
)

$ErrorActionPreference = "Stop"

$files = Get-ChildItem -LiteralPath $ViewsRoot -Recurse -Filter *.cshtml -File
$hits = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw

    # Inline <script> ... </script> that is NOT purely external src reference.
    foreach ($m in [regex]::Matches($content, '<script(?![^>]*\bsrc\s*=)[^>]*>(?<body>[\s\S]*?)</script>', 'IgnoreCase')) {
        $rawBody = $m.Groups["body"].Value
        if ($null -eq $rawBody) { $rawBody = "" }
        $body = $rawBody.Trim()
        if ($body.Length -eq 0) { continue }

        $hasNonce = [regex]::IsMatch($m.Value, '\bnonce\s*=\s*"', 'IgnoreCase')
        $hasTypeJsonLd = [regex]::IsMatch($m.Value, '\btype\s*=\s*"application/ld\+json"', 'IgnoreCase')
        $preview = $body -replace '\s+', ' '
        if ($preview.Length -gt 180) { $preview = $preview.Substring(0, 180) + "…" }

        $hits.Add([pscustomobject]@{
            File = $file.FullName
            HasNonce = $hasNonce
            IsJsonLd = $hasTypeJsonLd
            Preview = $preview
        })
    }
}

$header = @(
    "# Inline <script> inventory"
    "# Generated at: $(Get-Date -Format o)"
    "# ViewsRoot: $ViewsRoot"
    ""
)

$lines = $hits |
    Sort-Object File |
    ForEach-Object {
        ("{0}`tNonce={1}`tJsonLd={2}`t{3}" -f $_.File, $_.HasNonce, $_.IsJsonLd, $_.Preview)
    }

$header + $lines | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Host "Inline scripts found: $($hits.Count)"
Write-Host "Wrote: $OutFile"

