param(
    [string]$BaseUrl = "https://localhost:7223",
    [string]$RoutesFile = (Join-Path $PSScriptRoot "routes-smoke-list.txt"),
    [int]$TimeoutSec = 20
)

$ErrorActionPreference = "Stop"

# Localhost dev sertifikası PowerShell'de hata verebilir; smoke test için doğrulamayı bypass ediyoruz.
try {
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls13
} catch { }
try {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
} catch { }

function Normalize-RouteLine([string]$line) {
    $raw = ("" + $line).Trim()
    if ($raw.Length -eq 0) { return $null }
    if ($raw.StartsWith("#")) { return $null }
    $noComment = $raw.Split("#")[0].Trim()
    if ($noComment.Length -eq 0) { return $null }
    return $noComment
}

$routes = Get-Content -LiteralPath $RoutesFile | ForEach-Object { Normalize-RouteLine $_ } | Where-Object { $_ }
if (-not $routes -or $routes.Count -eq 0) {
    Write-Host "Route listesi bos."
    exit 1
}

$results = New-Object System.Collections.Generic.List[object]

foreach ($route in $routes) {
    $url = ($BaseUrl.TrimEnd("/") + $route)
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $resp = Invoke-WebRequest -UseBasicParsing -Uri $url -Method GET -TimeoutSec $TimeoutSec
        $sw.Stop()
        $results.Add([pscustomobject]@{
            Route = $route
            Status = [int]$resp.StatusCode
            Ms = [int]$sw.ElapsedMilliseconds
        })
    }
    catch {
        $results.Add([pscustomobject]@{
            Route = $route
            Status = -1
            Ms = -1
        })
    }
}

$ok = $results | Where-Object { $_.Status -ge 200 -and $_.Status -lt 400 }
$bad = $results | Where-Object { $_.Status -lt 0 -or $_.Status -ge 400 }

Write-Host ""
Write-Host "OK  : $($ok.Count)"
Write-Host "BAD : $($bad.Count)"
Write-Host ""

$results | Sort-Object Status, Route | Format-Table -AutoSize | Out-String | Write-Host

if ($bad.Count -gt 0) {
    exit 2
}

