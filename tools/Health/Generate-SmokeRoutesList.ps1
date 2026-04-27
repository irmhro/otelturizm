param(
    [string]$ExtractedRoutesFile = (Join-Path $PSScriptRoot "routes-extracted-from-views.txt"),
    [string]$OutFile = (Join-Path $PSScriptRoot "routes-smoke-list.txt")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ExtractedRoutesFile)) {
    throw "Extracted routes file not found: $ExtractedRoutesFile"
}

$lines = Get-Content -LiteralPath $ExtractedRoutesFile
$routes = New-Object System.Collections.Generic.List[string]

foreach ($line in $lines) {
    $t = ("" + $line).Trim()
    if ($t.Length -eq 0) { continue }
    if ($t.StartsWith("#")) { continue }

    # sadece GET ile smoke edilecek safe sayfalar
    if ($t.StartsWith("/admin")) { continue }
    if ($t.StartsWith("/panel")) { continue }
    if ($t.StartsWith("/secure-files")) { continue }
    if ($t.StartsWith("/gelisim")) { continue }
    if ($t.Contains("/logout") -or $t.Contains("/cikis")) { continue }

    $routes.Add($t)
}

$final = @($routes | Sort-Object -Unique)
$header = @(
    "# routes-smoke-list.txt",
    "# Generated at: $(Get-Date -Format o)",
    "# Source: $ExtractedRoutesFile",
    "# Note: panel/admin/secure routes intentionally excluded",
    ""
)

$header + $final | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Host "Wrote: $OutFile"
Write-Host "Routes: $($final.Count)"

