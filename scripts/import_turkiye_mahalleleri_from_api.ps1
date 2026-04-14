param(
    [string]$ApiBase = 'https://api.turkiyeapi.dev/v1',
    [string]$OutputSql = 'C:\laragon\www\otelturizmnew\otelturizmnew\Database\MigrationsSql\128_seed_turkiye_tum_mahalleleri_api.sql',
    [switch]$ExecuteSql
)

$ErrorActionPreference = 'Stop'

function Convert-ToSeoSlug {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }

    $map = @{
        'İ'='i'; 'I'='i'; 'ı'='i'; 'Ğ'='g'; 'ğ'='g'; 'Ü'='u'; 'ü'='u'; 'Ş'='s'; 'ş'='s'; 'Ö'='o'; 'ö'='o'; 'Ç'='c'; 'ç'='c'
    }

    $normalized = $Value.Trim()
    foreach ($key in $map.Keys) { $normalized = $normalized.Replace($key, $map[$key]) }
    $normalized = $normalized.ToLowerInvariant()
    $normalized = [regex]::Replace($normalized, '[^a-z0-9]+', '-')
    $normalized = [regex]::Replace($normalized, '-{2,}', '-')
    return $normalized.Trim('-')
}

function Escape-SqlString {
    param([AllowNull()][string]$Value)
    if ($null -eq $Value) { return 'NULL' }
    return "'" + $Value.Replace("'", "''") + "'"
}

Write-Host 'Provinces API okunuyor...'
$provinceResponse = Invoke-RestMethod "$ApiBase/provinces?limit=81"
if ($provinceResponse.status -ne 'OK') { throw 'İl verisi alınamadı.' }

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('-- Generated from api.turkiyeapi.dev on 2026-04-14')
$lines.Add('SET NAMES utf8mb4;')
$lines.Add('SET FOREIGN_KEY_CHECKS = 0;')

$totalNeighborhoods = 0
foreach ($province in $provinceResponse.data) {
    $provinceId = [int]$province.id
    $provinceName = [string]$province.name
    Write-Host "İşleniyor: $provinceName ($provinceId)"

    $districtResponse = Invoke-RestMethod "$ApiBase/districts?provinceId=$provinceId"
    if ($districtResponse.status -ne 'OK') {
        Write-Warning "Atlandı: $provinceName için ilçe verisi alınamadı."
        continue
    }

    foreach ($district in $districtResponse.data) {
        $districtId = [int]$district.id
        $districtName = [string]$district.name
        $neighborhoods = @($district.neighborhoods)
        if ($neighborhoods.Count -eq 0) { continue }

        foreach ($neighborhood in $neighborhoods) {
            $neighborhoodId = [int]$neighborhood.id
            $neighborhoodName = [string]$neighborhood.name
            $population = if ($null -ne $neighborhood.population) { [int]$neighborhood.population } else { 0 }
            $slug = Convert-ToSeoSlug "$neighborhoodName-$districtName"
            if ([string]::IsNullOrWhiteSpace($slug)) { $slug = "mahalle-$neighborhoodId" }

            $line = @"
INSERT INTO mahalleler (il_id, ilce_id, api_kodu, mahalle_adi, seo_slug, posta_kodu, enlem, boylam, nufus, aktif_mi)
SELECT il.id, ilce.id, $neighborhoodId, $(Escape-SqlString $neighborhoodName), $(Escape-SqlString $slug), NULL, NULL, NULL, $population, 1
FROM iller il
JOIN ilceler ilce ON ilce.il_id = il.id
WHERE il.plaka_kodu = $provinceId AND ilce.api_kodu = $districtId
ON DUPLICATE KEY UPDATE
    mahalle_adi = VALUES(mahalle_adi),
    seo_slug = VALUES(seo_slug),
    nufus = VALUES(nufus),
    aktif_mi = VALUES(aktif_mi),
    guncellenme_tarihi = CURRENT_TIMESTAMP;
"@
            $lines.Add($line.TrimEnd())
            $totalNeighborhoods++
        }
    }
}

$lines.Add('SET FOREIGN_KEY_CHECKS = 1;')
$dir = Split-Path -Parent $OutputSql
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
[System.IO.File]::WriteAllLines($OutputSql, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Host "SQL dosyası üretildi: $OutputSql"
Write-Host "Toplam mahalle satırı: $totalNeighborhoods"

if ($ExecuteSql) {
    $mysql = 'C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysql.exe'
    & $mysql -u root -D otelturizmnew --default-character-set=utf8mb4 --execute="source $OutputSql"
    Write-Host 'SQL başarıyla çalıştırıldı.'
}
