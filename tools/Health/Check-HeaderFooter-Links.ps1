param(
    [string]$BaseUrl = "https://localhost:7223",
    [string]$HeaderFile = "d:\otelturizm\Views\Anasayfa\_AnasayfaHeader.cshtml",
    [string]$FooterFile = "d:\otelturizm\Views\Anasayfa\_AnasayfaFooter.cshtml",
    [int]$TimeoutSec = 20
)

$ErrorActionPreference = "Stop"

try {
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls13
} catch { }
try {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
} catch { }

function Extract-Links([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return @() }
    $content = Get-Content -LiteralPath $path -Raw
    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)

    foreach ($m in [regex]::Matches($content, '<a[^>]+href\s*=\s*"(?<u>/[^"]+)"', 'IgnoreCase')) {
        $u = $m.Groups["u"].Value.Trim()
        if ($u.Contains("@") -or $u.Contains("{") -or $u.Contains("}")) { continue }
        $u = $u.Split("#")[0]
        $u = $u.Split("?")[0]
        if ($u.StartsWith("/assets/") -or $u.StartsWith("/lib/") -or $u.StartsWith("/uploads/")) { continue }
        [void]$set.Add($u)
    }
    return @($set) | Sort-Object
}

$links = @()
$links += Extract-Links $HeaderFile
$links += Extract-Links $FooterFile
$links = @($links | Sort-Object -Unique)

if ($links.Count -eq 0) {
    Write-Host "No internal links found."
    exit 0
}

$results = New-Object System.Collections.Generic.List[object]
foreach ($route in $links) {
    $url = ($BaseUrl.TrimEnd("/") + $route)
    try {
        $resp = Invoke-WebRequest -UseBasicParsing -Uri $url -Method GET -TimeoutSec $TimeoutSec
        $results.Add([pscustomobject]@{ Route = $route; Status = [int]$resp.StatusCode })
    } catch {
        $results.Add([pscustomobject]@{ Route = $route; Status = -1 })
    }
}

$bad = $results | Where-Object { $_.Status -lt 200 -or $_.Status -ge 400 }
$ok = $results | Where-Object { $_.Status -ge 200 -and $_.Status -lt 400 }

Write-Host ""
Write-Host "Header/Footer links OK : $($ok.Count)"
Write-Host "Header/Footer links BAD: $($bad.Count)"
Write-Host ""

$results | Sort-Object Status, Route | Format-Table -AutoSize | Out-String | Write-Host

if ($bad.Count -gt 0) { exit 2 }

