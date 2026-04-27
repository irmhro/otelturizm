param(
    [string]$ViewsRoot = "d:\otelturizm\Views",
    [string]$OutFile = (Join-Path $PSScriptRoot "routes-extracted-from-views.txt"),
    [switch]$IncludeFormActions,
    [switch]$IncludeFetchCalls
)

$ErrorActionPreference = "Stop"

function Add-Route([System.Collections.Generic.HashSet[string]]$set, [string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return }
    $route = $value.Trim()
    if (-not $route.StartsWith("/")) { return }
    if ($route.StartsWith("//")) { return }
    if ($route.Contains("@") -or $route.Contains("{") -or $route.Contains("}")) { return }
    if ($route.StartsWith("/assets/") -or $route.StartsWith("/lib/") -or $route.StartsWith("/uploads/") -or $route.StartsWith("/js/") -or $route.StartsWith("/css/")) { return }

    $route = $route.Split("#")[0]
    $route = $route.Split("?")[0]
    $route = $route.Trim()
    if ($route.Length -eq 0) { return }

    [void]$set.Add($route)
}

$routes = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)

$files = Get-ChildItem -LiteralPath $ViewsRoot -Recurse -Filter *.cshtml -File
foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw

    # href="/..." (navigational links only)
    foreach ($m in [regex]::Matches($content, '<a[^>]+href\s*=\s*"(?<u>/[^"]+)"', 'IgnoreCase')) {
        Add-Route $routes $m.Groups["u"].Value
    }

    if ($IncludeFormActions) {
        foreach ($m in [regex]::Matches($content, '<form[^>]+action\s*=\s*"(?<u>/[^"]+)"', 'IgnoreCase')) {
            Add-Route $routes $m.Groups["u"].Value
        }
    }

    if ($IncludeFetchCalls) {
        foreach ($m in [regex]::Matches($content, 'fetch\(\s*(["''])(?<u>/[^"''\)]+)\1', 'IgnoreCase')) {
            Add-Route $routes $m.Groups["u"].Value
        }
    }
}

$final = @($routes) | Sort-Object
$header = @(
    "# Auto-extracted internal routes from Views (*.cshtml)"
    "# Generated at: $(Get-Date -Format o)"
    ""
)

$header + ($final | ForEach-Object { $_ }) | Set-Content -LiteralPath $OutFile -Encoding UTF8
Write-Host "Extracted routes: $($final.Count)"
Write-Host "Wrote: $OutFile"

