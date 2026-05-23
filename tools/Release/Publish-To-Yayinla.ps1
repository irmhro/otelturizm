# Tam Release yayini -> yayinla/publish-<yyyyMMdd-HHmmss>/
# Calistir (repo kokunden): powershell -ExecutionPolicy Bypass -File .\tools\Release\Publish-To-Yayinla.ps1
$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Csproj = Join-Path $ProjectRoot 'otelturizm.csproj'
if (-not (Test-Path $Csproj)) {
    throw "Proje bulunamadi: $Csproj"
}

$BaseOut = Join-Path $ProjectRoot 'yayinla'
New-Item -ItemType Directory -Force -Path $BaseOut | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$PublishDir = Join-Path $BaseOut "publish-$stamp"
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

Write-Host "Publishing Release (framework-dependent) -> $PublishDir"
dotnet publish $Csproj `
    -c Release `
    -o $PublishDir `
    --verbosity minimal `
    -p:PublishTrimmed=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$requiredFiles = @(
    'otelturizm.dll',
    'Views\Email\tr\RezervasyonOnaylandi.cshtml',
    'Views\Email\tr\Rezervasyon Talebi Alindi.cshtml',
    'Views\Email\tr\Partner Yeni Rezervasyon.cshtml',
    'Views\Email\tr\Rezervasyon Reddedildi.cshtml',
    'Views\Paneller\Partner\Dashboard.cshtml',
    'Views\Paneller\Partner\_PartnerSidebar.cshtml',
    'wwwroot\assets\css\paneller\partner\dashboard.css',
    'wwwroot\assets\css\paneller\partner\shell.css'
)
$missing = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $PublishDir $_)) })
if ($missing.Count -gt 0) {
    throw "Publish paketi eksik dosya uretti: $($missing -join ', ')"
}

$dtLocal = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
$dtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss')
$dv = dotnet --version 2>$null

$info = @"
Otelturizm - sunucuya yukleme paketi
=====================================
Yerel olusturma: $dtLocal
UTC:             $dtUtc
Klasor etiketi:  publish-$stamp
Tam yol:         $PublishDir
Konfigurasyon:   Release
dotnet SDK:      $dv
Trim:            kapali (web uygulamasi)

Bu klasorun TAMAMINI IIS/Kestrel uygulama kokune kopyalayin.
Baglanti dizgileri ve gizli anahtarlar ortam degiskenleri veya guvenli store ile verilmelidir.
"@
$info | Set-Content -Encoding UTF8 (Join-Path $PublishDir 'YAYIN-BILGISI.txt')

@(
    "Son yayin: publish-$stamp",
    "Yerel: $dtLocal",
    "Yol: $PublishDir"
) | Set-Content -Encoding UTF8 (Join-Path $BaseOut 'SON-YAYIN.txt')

Write-Host "Tamam: $PublishDir"
