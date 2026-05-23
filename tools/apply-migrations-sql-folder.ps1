<#
.SYNOPSIS
  Database/MigrationsSql altindaki tum *.sql dosyalarini dosya adina gore sirali sqlcmd ile calistirir (yerel/onayli ortam).

.PARAMETER Server
  Ornek: . veya .\SQLEXPRESS veya 185.111.244.246

.PARAMETER Database
  Ornek: otelturizm_local

.PARAMETER UserId
  Bos birakilirsa -E (Windows Authentication) kullanilir.

.PARAMETER WhatIf
  Sadece listeler, sqlcmd calistirmaz.

.NOTES
  - Canli DB: once tam yedek; scriptlerin idempotent oldugundan emin olun.
  - Hata durumunda sqlcmd -b ile durur.
#>
param(
    [Parameter(Mandatory = $true)][string]$Server,
    [Parameter(Mandatory = $true)][string]$Database,
    [string]$UserId = "",
    [string]$Password = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path (Join-Path $repoRoot "otelturizm.csproj"))) {
    throw "otelturizm.csproj bulunamadi. Script tools klasorunde calistirilmali: $PSScriptRoot"
}
$migrationsDir = Join-Path $repoRoot "Database\MigrationsSql"
if (-not (Test-Path $migrationsDir)) {
    throw "Klasor bulunamadi: $migrationsDir"
}

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    throw "sqlcmd bulunamadi. SQL Server Command Line Tools kurulu olmali."
}

$orderedDirs = @(
    (Join-Path $migrationsDir "tablo\migrationlar"),
    (Join-Path $migrationsDir "constraints"),
    (Join-Path $migrationsDir "veri\migrationlar")
)
$files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
foreach ($dir in $orderedDirs) {
    if (-not (Test-Path $dir)) { continue }
    Get-ChildItem -Path $dir -Filter "*.sql" -File | Sort-Object Name | ForEach-Object { $files.Add($_) }
}
if ($files.Count -eq 0) {
    throw "Hic .sql bulunamadi (tablo/migrationlar, constraints, veri/migrationlar): $migrationsDir"
}

Write-Host "Toplam $($files.Count) script. Hedef: $Server / $Database"
foreach ($f in $files) {
    Write-Host ">> $($f.Name)"
    if ($WhatIf) { continue }
    if ($UserId) {
        $env:SQLCMDPASSWORD = $Password
        try {
            & sqlcmd.exe -S $Server -d $Database -U $UserId -I -b -i $f.FullName
        }
        finally {
            Remove-Item Env:SQLCMDPASSWORD -ErrorAction SilentlyContinue
        }
    }
    else {
        & sqlcmd.exe -S $Server -d $Database -E -I -b -i $f.FullName
    }
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd hata: $($f.Name) (exit $LASTEXITCODE)"
    }
}
Write-Host "Tamamlandi."
