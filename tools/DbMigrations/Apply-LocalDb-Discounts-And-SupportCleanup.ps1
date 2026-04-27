param(
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;"
)

$ErrorActionPreference = "Stop"

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

function Invoke-SqlQuery {
    param([Parameter(Mandatory = $true)][string]$Sql)

    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 120
        $cmd.CommandText = $Sql
        $da = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
        $ds = New-Object System.Data.DataSet
        [void]$da.Fill($ds)
        foreach ($t in $ds.Tables) {
            if ($t.Rows.Count -gt 0) {
                $t | Format-Table -AutoSize | Out-String | Write-Host
            }
        }
    }
    finally {
        $conn.Close()
    }
}

function Invoke-SqlFile {
    param([Parameter(Mandatory = $true)][string]$Path)

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

$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$discountSql = Join-Path $root "Database\MigrationsSql\20260427_sqlserver_expand_fiyat_indirimleri_to_50_remove_test.sql"
$supportSql  = Join-Path $root "Database\MigrationsSql\20260427_sqlserver_cleanup_destek_makaleleri_brand_terms.sql"
$supportSlugSql  = Join-Path $root "Database\MigrationsSql\20260427_sqlserver_cleanup_destek_makaleleri_slug_and_brand_terms.sql"

Write-Host "APPLY: $discountSql"
Invoke-SqlFile -Path $discountSql

Write-Host "APPLY: $supportSql"
Invoke-SqlFile -Path $supportSql

Write-Host "APPLY: $supportSlugSql"
Invoke-SqlFile -Path $supportSlugSql

Write-Host "VERIFY: counts"
$verify = @"
SET NOCOUNT ON;
SELECT COUNT(*) AS aktif_adet FROM dbo.fiyat_indirimleri WHERE aktif_mi=1;
SELECT COUNT(*) AS marka_gecen FROM dbo.destek_makaleleri WHERE baslik LIKE N'%Booking%' OR baslik LIKE N'%Airbnb%' OR baslik LIKE N'%Expedia%';
SELECT COUNT(*) AS slug_kalinti FROM dbo.destek_makaleleri WHERE seo_slug LIKE 'booking-%' OR seo_slug LIKE 'airbnb-%' OR seo_slug LIKE 'expedia-%';
"@
Invoke-SqlQuery -Sql $verify

Write-Host "Tamamlandi."

