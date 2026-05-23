# Yerel LocalDB'ye Database migration'larini uygular (canli sunucu kullanilmaz).
$ErrorActionPreference = "Stop"
$Root = "D:\otelturizm"
$Server = "(localdb)\MSSQLLocalDB"
$Database = "otelturizm_2026db"

function Invoke-SqlFile([string]$Path) {
    if (-not (Test-Path $Path)) { return }
    Write-Host ">> $([IO.Path]::GetFileName($Path))"
    sqlcmd -S $Server -d $Database -E -I -f 65001 -b -i $Path | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $Path" }
}

Write-Host "Bootstrap..."
sqlcmd -S $Server -E -Q "IF DB_ID(N'$Database') IS NULL CREATE DATABASE [$Database];" -b
Invoke-SqlFile "$Root\Database\Bootstrap\001_create_otelturizm_database.sql"

$tabloDir = "$Root\Database\MigrationsSql\tablo\migrationlar"
$veriDir = "$Root\Database\MigrationsSql\veri\migrationlar"

Get-ChildItem "$tabloDir\*.sql" -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object {
    Invoke-SqlFile $_.FullName
}

foreach ($tail in @("900_foreign_keys.sql", "901_indexes.sql", "902_triggers.sql")) {
    $p = Join-Path "$Root\Database\MigrationsSql\constraints" $tail
    if (Test-Path $p) {
        try { Invoke-SqlFile $p } catch { Write-Warning "Atlandi (hata): $tail - $($_.Exception.Message)" }
    }
}

Get-ChildItem "$veriDir\*.sql" -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object {
    try { Invoke-SqlFile $_.FullName } catch { Write-Warning "Atlandi: $($_.Name) - $($_.Exception.Message)" }
}

$count = sqlcmd -S $Server -d $Database -E -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0;" -h -1 -W
Write-Host "Yerel tablo sayisi: $($count.Trim())"
Write-Host "Tamamlandi."
