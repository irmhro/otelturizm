# Yayin: D:\otelturizm\23.05.2026\
$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Csproj = Join-Path $ProjectRoot 'otelturizm.csproj'
$PublishDir = Join-Path $ProjectRoot '23.05.2026'

if (Test-Path $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

Write-Host "Build + Publish Release -> $PublishDir"
dotnet publish $Csproj -c Release -o $PublishDir --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$info = @"
Otelturizm yayin paketi
========================
Klasor: 23.05.2026
Tarih:  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Konfig: Release

Demo gorseller: wwwroot/uploads/images/{otelId}/hotel|rooms/
DB seed: 20260523_seed_istanbul_10_ilce_oteller.sql
         20260523_seed_demo_otel_medya_ve_ozellikler.sql
"@
$info | Set-Content -Encoding UTF8 (Join-Path $PublishDir 'YAYIN-BILGISI.txt')
Write-Host "Tamam: $PublishDir"
