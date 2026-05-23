# Istanbul 39 ilce demo otelleri: gorsel seed + SQL (tek komut)
param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'otelturizm_2026db',
    [switch]$IncludeLegacy10Ilce
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$MigDir = Join-Path $Root 'Database\MigrationsSql\veri\migrationlar'
$DemoProj = Join-Path $Root 'tools\DemoImageSeed\DemoImageSeed.csproj'
$demoArgs = @('--root=' + $Root)
if ($Server -ne '(localdb)\MSSQLLocalDB' -or $Database -ne 'otelturizm_2026db') {
    $demoArgs += ('--conn=Server=' + $Server + ';Database=' + $Database + ';Trusted_Connection=True;TrustServerCertificate=True;')
}

function Invoke-DemoImageSeed {
    dotnet run --project $DemoProj -c Release -- @demoArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

function Invoke-SqlFile {
    param([string]$Path, [switch]$Optional)
    if (-not (Test-Path $Path)) {
        if ($Optional) {
            Write-Warning "Atlandi (dosya yok): $([IO.Path]::GetFileName($Path))"
            return
        }
        throw "SQL dosyasi bulunamadi: $Path"
    }
    Write-Host ">> $([IO.Path]::GetFileName($Path))"
    sqlcmd -S $Server -d $Database -E -I -f 65001 -b -i $Path
    if ($LASTEXITCODE -ne 0) {
        if ($Optional) {
            Write-Warning "SQL uyarisi: $([IO.Path]::GetFileName($Path)) (exit $LASTEXITCODE)"
        } else {
            throw "sqlcmd failed: $Path (exit $LASTEXITCODE)"
        }
    }
}

function Resolve-MigrationPath {
    param([string[]]$Candidates)
    foreach ($name in $Candidates) {
        $p = Join-Path $MigDir $name
        if (Test-Path $p) { return $p }
    }
    return $null
}

$IstanbulTam = Resolve-MigrationPath @(
    '20260526_seed_istanbul_ilce_oteller_tam.sql',
    '20260526_seed_istanbul_ilce_tam_oteller.sql',
    '20260526_seed_istanbul_39_ilce_oteller.sql',
    '20260526_seed_istanbul_ilce_tam.sql'
)
$MedyaOzellik = Join-Path $MigDir '20260526_seed_istanbul_ilce_medya_ozellik.sql'
$Legacy10 = Join-Path $MigDir '20260523_seed_istanbul_10_ilce_oteller.sql'

Write-Host "1) Demo gorseller (mevcut DB otelleri)..."
Invoke-DemoImageSeed

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Warning "sqlcmd yok; SQL seed dosyalarini manuel uygulayin, sonra DemoImageSeed'i --conn ile calistirin."
    exit 0
}

Write-Host "2) SQL seed (istanbul tam -> medya -> istege bagli 10 ilce)..."
if ($IstanbulTam) {
    Invoke-SqlFile -Path $IstanbulTam
} else {
    Write-Warning "Istanbul tam seed bulunamadi (20260526_*); migration dosyasini ekleyin."
}

if ($IncludeLegacy10Ilce) {
    Invoke-SqlFile -Path $Legacy10 -Optional
}

if (Test-Path $MedyaOzellik) {
    Invoke-SqlFile -Path $MedyaOzellik
} else {
    Write-Warning "Medya/ozellik seed yok: 20260526_seed_istanbul_ilce_medya_ozellik.sql"
}

Write-Host "3) Demo gorseller (DB ID eslemesi, ORK-IST + ORK-SEED)..."
Invoke-DemoImageSeed

Write-Host "Bitti. wwwroot/uploads/images altinda dosyalari kontrol edin."
