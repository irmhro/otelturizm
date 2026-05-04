param(
    [string]$ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;',
    [string]$MigrationsDir = (Join-Path -Path (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path -ChildPath 'Database\MigrationsSql'),
    [string[]]$Tables = @(),
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

function Invoke-Sql {
    param([Parameter(Mandatory = $true)][string]$Sql)

    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 120
        $cmd.CommandText = $Sql
        [void]$cmd.ExecuteNonQuery()
    }
    finally {
        $conn.Close()
    }
}

function Invoke-Scalar {
    param(
        [Parameter(Mandatory = $true)][string]$Sql
    )

    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 120
        $cmd.CommandText = $Sql
        return $cmd.ExecuteScalar()
    }
    finally {
        $conn.Close()
    }
}

function Invoke-SqlFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $sql = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($sql)) { return }

    # Basit GO bolme (SQLCMD/SSMS tarzi)
    $batches = $sql -split '^\s*GO\s*$', 0, 'Multiline'
    foreach ($batch in $batches) {
        if (-not [string]::IsNullOrWhiteSpace($batch)) {
            Invoke-Sql -Sql $batch
        }
    }
}

function Test-IsSqlServerScript {
    param([Parameter(Mandatory = $true)][string]$Sql)

    # MySQL/MariaDB kokulari (local MSSQL icin pas gecilecek)
    $mysqlSignals = @(
        'SET NAMES',
        'utf8mb4',
        'ENGINE=',
        'AUTO_INCREMENT',
        'UNSIGNED',
        'COLLATE ',
        'CHARSET=',
        'LOCK TABLES',
        'UNLOCK TABLES',
        'DELIMITER',
        '/*!',
        '`'
    )

    foreach ($s in $mysqlSignals) {
        if ($Sql -match [regex]::Escape($s)) { return $false }
    }

    # MSSQL/T-SQL icin pozitif sinyaller (en az biri olmali)
    $mssqlSignals = @(
        'OBJECT_ID(',
        'dbo.',
        'SET ANSI_NULLS',
        'SET QUOTED_IDENTIFIER'
    )

    foreach ($s in $mssqlSignals) {
        if ($Sql -match [regex]::Escape($s)) { return $true }
    }

    return $false
}

function Get-TouchedTables {
    param([Parameter(Mandatory = $true)][string]$Sql)

    $tables = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)

    $patternCreateAlter = '(?im)(?:CREATE|ALTER)\s+TABLE\s+(?:\[\s*dbo\s*\]\.)?(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?'
    $patternObjectId = '(?im)OBJECT_ID\s*\(\s*N?''(?:\[\s*dbo\s*\]\.)?(?:dbo\.)?\[?([A-Za-z0-9_]+)\]?'''

    $rx1 = [regex]::new($patternCreateAlter, [System.Text.RegularExpressions.RegexOptions]::Compiled)
    $rx2 = [regex]::new($patternObjectId, [System.Text.RegularExpressions.RegexOptions]::Compiled)

    foreach ($m in $rx1.Matches($Sql)) { [void]$tables.Add($m.Groups[1].Value) }
    foreach ($m in $rx2.Matches($Sql)) { [void]$tables.Add($m.Groups[1].Value) }

    return @($tables)
}

# schema_migrations tablosu (repo standardi ile uyumlu, idempotent)
$ensureSchema = @'
IF OBJECT_ID(N''dbo.schema_migrations'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.schema_migrations
    (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        dosya_adi NVARCHAR(255) NOT NULL,
        uygulanma_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_schema_migrations_uygulanma_tarihi DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_schema_migrations_dosya_adi ON dbo.schema_migrations(dosya_adi);
END;
'@

$migrationNameColumn = 'dosya_adi'
if (-not $WhatIf) {
    Invoke-Sql -Sql $ensureSchema

    $hasDosyaAdi = [int](Invoke-Scalar -Sql "SELECT CASE WHEN COL_LENGTH(N'dbo.schema_migrations', N'dosya_adi') IS NOT NULL THEN 1 ELSE 0 END;")
    $hasScriptName = [int](Invoke-Scalar -Sql "SELECT CASE WHEN COL_LENGTH(N'dbo.schema_migrations', N'script_name') IS NOT NULL THEN 1 ELSE 0 END;")

    if ($hasDosyaAdi -eq 1) { $migrationNameColumn = 'dosya_adi' }
    elseif ($hasScriptName -eq 1) { $migrationNameColumn = 'script_name' }
    else {
        throw 'dbo.schema_migrations bulundu ama isim kolonu (dosya_adi / script_name) yok. Tabloyu duzeltin veya yeniden olusturun.'
    }
}

Write-Host ('MigrationsDir: ' + $MigrationsDir)
if ($Tables -and $Tables.Count -gt 0) {
    Write-Host ('Tables filter: ' + ($Tables -join ', '))
}
if ($WhatIf) {
    Write-Host 'WhatIf: sadece listeleme (uygulama yok)'
}

$files = Get-ChildItem -LiteralPath $MigrationsDir -Filter '*.sql' | Sort-Object Name

$candidates = @()
foreach ($f in $files) {
    $sql = Get-Content -LiteralPath $f.FullName -Raw
    if ([string]::IsNullOrWhiteSpace($sql)) { continue }
    if (-not (Test-IsSqlServerScript -Sql $sql)) { continue }

    $touched = Get-TouchedTables -Sql $sql
    $candidates += [pscustomobject]@{
        File = $f
        Tables = $touched
    }
}

if (-not $candidates -or $candidates.Count -eq 0) {
    Write-Host 'MSSQL uyumlu migration bulunamadi.'
    exit 0
}

if ($Tables -and $Tables.Count -gt 0) {
    $set = New-Object System.Collections.Generic.HashSet[string] ([StringComparer]::OrdinalIgnoreCase)
    foreach ($t in $Tables) {
        if (-not [string]::IsNullOrWhiteSpace($t)) { [void]$set.Add($t.Trim()) }
    }

    $candidates = $candidates | Where-Object {
        foreach ($tt in $_.Tables) { if ($set.Contains($tt)) { return $true } }
        return $false
    }
}

if ($WhatIf) {
    $candidates | ForEach-Object {
        $name = $_.File.Name
        $tablesTxt = if ($_.Tables -and $_.Tables.Count -gt 0) { ($_.Tables -join ', ') } else { '(tablo tespit edilemedi)' }
        Write-Host ('PLAN  ' + $name + '  -  ' + $tablesTxt)
    }
    exit 0
}

foreach ($c in $candidates) {
    $name = $c.File.Name

    $checkSql = ('SELECT COUNT(*) FROM dbo.schema_migrations WHERE ' + $migrationNameColumn + ' = @name;')
    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $checkSql
        $param = $cmd.Parameters.Add('@name', [System.Data.SqlDbType]::NVarChar, 260)
        $param.Value = $name
        $exists = [int]$cmd.ExecuteScalar()
    }
    finally {
        $conn.Close()
    }

    if ($exists -gt 0) {
        Write-Host ('SKIP  ' + $name)
        continue
    }

    $tablesTxt = if ($c.Tables -and $c.Tables.Count -gt 0) { ($c.Tables -join ', ') } else { 'unknown' }
    Write-Host ('APPLY ' + $name + '  -  tables: ' + $tablesTxt)
    Invoke-SqlFile -Path $c.File.FullName

    $insertSql = ('INSERT INTO dbo.schema_migrations(' + $migrationNameColumn + ') VALUES (@name);')
    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $insertSql
        $param = $cmd.Parameters.Add('@name', [System.Data.SqlDbType]::NVarChar, 260)
        $param.Value = $name
        [void]$cmd.ExecuteNonQuery()
    }
    finally {
        $conn.Close()
    }
}

Write-Host 'Tamamlandi.'

