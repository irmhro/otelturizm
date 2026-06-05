# LocalDB — otelturizm_2026db geri yükleme (GitHub clone sonrası)
$ErrorActionPreference = 'Stop'
$bak = Join-Path $PSScriptRoot 'otelturizm_2026db.bak'

if (-not (Test-Path $bak)) {
    throw "Yedek bulunamadi: $bak"
}

Write-Host "Geri yukleniyor: $bak"
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "RESTORE DATABASE [otelturizm_2026db] FROM DISK = N'$bak' WITH REPLACE, STATS = 10"

Write-Host "Dogrulama — FK / tablo:"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d otelturizm_2026db -Q "SELECT (SELECT COUNT(*) FROM sys.foreign_keys) AS foreign_keys, (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) AS user_tables" -W
