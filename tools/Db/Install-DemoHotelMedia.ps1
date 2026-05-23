# Demo otel gorselleri + DB seed (tek komut)
$ErrorActionPreference = 'Stop'
$Root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

Write-Host "1) Demo gorseller indiriliyor (Picsum -> WebP)..."
dotnet run --project (Join-Path $Root 'tools\DemoImageSeed\DemoImageSeed.csproj') -c Release -- --root=$Root
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$SeedIstanbul = Join-Path $Root 'Database\MigrationsSql\veri\migrationlar\20260523_seed_istanbul_10_ilce_oteller.sql'
$SeedMedia = Join-Path $Root 'Database\MigrationsSql\veri\migrationlar\20260523_seed_demo_otel_medya_ve_ozellikler.sql'
$Db = 'otelturizm_2026db'
$Server = '(localdb)\MSSQLLocalDB'

if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
    Write-Host "2) SQL seed uygulaniyor..."
    sqlcmd -S $Server -d $Db -I -i $SeedIstanbul -b
    if ($LASTEXITCODE -ne 0) { Write-Warning "Istanbul seed atlandi veya hata" }
    sqlcmd -S $Server -d $Db -I -i $SeedMedia -b
    if ($LASTEXITCODE -ne 0) { Write-Warning "Medya seed hata" }
    Write-Host "3) Gorsel seed tekrar (ID eslemesi)..."
    dotnet run --project (Join-Path $Root 'tools\DemoImageSeed\DemoImageSeed.csproj') -c Release -- --root=$Root
} else {
    Write-Warning "sqlcmd yok; seed dosyalarini manuel uygulayin."
}

Write-Host "Bitti."
