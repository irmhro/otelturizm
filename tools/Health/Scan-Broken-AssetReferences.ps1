param(
    [string]$RepoRoot = "d:\otelturizm",
    [string]$OutFile = (Join-Path $PSScriptRoot "broken-asset-references.txt")
)

$ErrorActionPreference = "Stop"

function Resolve-AssetPath([string]$url) {
    $u = ("" + $url).Trim()
    if ($u.StartsWith("~/")) {
        $rel = $u.Substring(2).Replace("/", "\")
        return (Join-Path $RepoRoot "wwwroot\$rel")
    }
    if ($u.StartsWith("/")) {
        $rel = $u.Substring(1).Replace("/", "\")
        return (Join-Path $RepoRoot "wwwroot\$rel")
    }
    return $null
}

$targets = @()
$targets += Get-ChildItem -LiteralPath (Join-Path $RepoRoot "Views") -Recurse -Filter *.cshtml -File -ErrorAction SilentlyContinue
$targets += Get-ChildItem -LiteralPath (Join-Path $RepoRoot "wwwroot") -Recurse -Include *.css,*.js -File -ErrorAction SilentlyContinue

$missing = New-Object System.Collections.Generic.List[string]

foreach ($file in $targets) {
    $text = Get-Content -LiteralPath $file.FullName -Raw

    foreach ($m in [regex]::Matches($text, '(href|src)\s*=\s*"(?<u>~?/[^"]+)"', 'IgnoreCase')) {
        $u = $m.Groups["u"].Value
        if ($u.StartsWith("http", [System.StringComparison]::OrdinalIgnoreCase)) { continue }
        if ($u.Contains("@") -or $u.Contains("{") -or $u.Contains("}")) { continue }
        if ($u.Contains("?")) { $u = $u.Split("?")[0] }
        if ($u.Contains("#")) { $u = $u.Split("#")[0] }
        $path = Resolve-AssetPath $u
        if ($path -and -not (Test-Path -LiteralPath $path)) {
            $missing.Add("$($file.FullName) :: $u :: $path")
        }
    }
}

$header = @(
    "# Broken asset references",
    "# Generated at: $(Get-Date -Format o)",
    ""
)
$header + ($missing | Sort-Object) | Set-Content -LiteralPath $OutFile -Encoding UTF8

Write-Host "Missing refs: $($missing.Count)"
Write-Host "Wrote: $OutFile"

if ($missing.Count -gt 0) { exit 2 }

