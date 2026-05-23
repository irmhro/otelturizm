# Otelturizm Release yayini -> yayinlanacakderlenmisdosyalar/publish-<timestamp>/
# Calistir: powershell -File tools\Release\Publish-To-YayinKlasoru.ps1 (repo kokunden)
$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Csproj = Join-Path $ProjectRoot 'otelturizm.csproj'
if (-not (Test-Path $Csproj)) {
    throw "Proje bulunamadi: $Csproj"
}

$BaseOut = Join-Path $ProjectRoot 'yayinlanacakderlenmisdosyalar'
New-Item -ItemType Directory -Force -Path $BaseOut | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$PublishDir = Join-Path $BaseOut "publish-$stamp"
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

Write-Host "Publishing Release -> $PublishDir"
dotnet publish $Csproj -c Release -o $PublishDir --verbosity minimal
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
Otelturizm - yayin paketi
============================
Yerel olusturma: $dtLocal
UTC:             $dtUtc
Cikis klasoru:   $PublishDir
Konfigurasyon:   Release
dotnet SDK:      $dv

IIS/Kestrel icin bu klasorun icerigi uygulama kokune kopyalanabilir.
appsettings ve baglanti dizgileri ortam degiskenleri ile ayarlanmalidir.
"@
$info | Set-Content -Encoding UTF8 (Join-Path $PublishDir 'YAYIN-BILGISI.txt')

$son = @"
Son yayin klasoru: publish-$stamp
Yerel tarih/saat:   $dtLocal
Tam yol: $PublishDir
"@
$son | Set-Content -Encoding UTF8 (Join-Path $BaseOut 'SON-YAYIN.txt')

Write-Host "Tamam: $PublishDir"
