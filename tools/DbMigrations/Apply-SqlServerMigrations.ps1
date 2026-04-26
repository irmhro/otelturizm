param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [string]$MigrationsDir = (Join-Path -Path (Resolve-Path (Join-Path $PSScriptRoot "..\\..")).Path -ChildPath "Database\\MigrationsSql")
)

$ErrorActionPreference = "Stop"

function Invoke-Sql {
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
        [void]$cmd.ExecuteNonQuery()
    }
    finally {
        $conn.Close()
    }
}

function Invoke-SqlFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )
    $sql = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($sql)) { return }

    # Basit GO bölme (SQLCMD/SSMS tarzı)
    $batches = $sql -split "^\s*GO\s*$", 0, "Multiline"
    foreach ($batch in $batches) {
        if (-not [string]::IsNullOrWhiteSpace($batch)) {
            Invoke-Sql -Sql $batch
        }
    }
}

Write-Host "MigrationsDir: $MigrationsDir"

# schema_migrations tablosu (idempotent)
$ensureSchema = @"
IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schema_migrations (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        script_name NVARCHAR(260) NOT NULL UNIQUE,
        applied_at_utc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
"@
Invoke-Sql -Sql $ensureSchema

$files = Get-ChildItem -LiteralPath $MigrationsDir -Filter "*.sql" | Sort-Object Name
$sqlServerFiles = $files | Where-Object { $_.Name -match "sqlserver" }

if (-not $sqlServerFiles -or $sqlServerFiles.Count -eq 0) {
    Write-Host "SQL Server migration bulunamadi (dosya adi 'sqlserver' icermeli)."
    exit 0
}

foreach ($f in $sqlServerFiles) {
    $name = $f.Name
    $checkSql = "SELECT COUNT(*) FROM dbo.schema_migrations WHERE script_name = @name;"

    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $checkSql
        $param = $cmd.Parameters.Add("@name",[System.Data.SqlDbType]::NVarChar,260)
        $param.Value = $name
        $exists = [int]$cmd.ExecuteScalar()
    }
    finally {
        $conn.Close()
    }

    if ($exists -gt 0) {
        Write-Host "SKIP  $name"
        continue
    }

    Write-Host "APPLY $name"
    Invoke-SqlFile -Path $f.FullName

    $checksum = (Get-FileHash -Algorithm SHA256 -LiteralPath $f.FullName).Hash
    $insertSql = @"
IF COL_LENGTH(N'dbo.schema_migrations', N'checksum') IS NOT NULL
BEGIN
    INSERT INTO dbo.schema_migrations(script_name, checksum) VALUES (@name, @checksum);
END
ELSE
BEGIN
    INSERT INTO dbo.schema_migrations(script_name) VALUES (@name);
END
"@
    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $insertSql
        $param = $cmd.Parameters.Add("@name",[System.Data.SqlDbType]::NVarChar,260)
        $param.Value = $name
        $param2 = $cmd.Parameters.Add("@checksum",[System.Data.SqlDbType]::NVarChar,128)
        $param2.Value = $checksum
        [void]$cmd.ExecuteNonQuery()
    }
    finally {
        $conn.Close()
    }
}

Write-Host "Tamamlandi."

