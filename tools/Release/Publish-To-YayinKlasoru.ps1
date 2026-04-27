# Otelturizm Release yayini -> yayinlanacakderlenmisdosyalar/publish-<timestamp>/
# Calistir: powershell -File tools\Release\Publish-To-YayinKlasoru.ps1 (repo kokunden)
$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Csproj = Join-Path $ProjectRoot 'otelturizmnew.csproj'
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
