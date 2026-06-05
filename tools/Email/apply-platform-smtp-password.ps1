param(
    [Parameter(Mandatory = $true)]
    [string]$SmtpPassword,
    [string]$Server = "185.111.244.246",
    [string]$Database = "otelturizm_2026db",
    [string]$User = "sa",
    [string]$Password = "Nusret.34.34.-"
)

if ([string]::IsNullOrWhiteSpace($SmtpPassword)) {
    throw "SmtpPassword bos olamaz."
}

$escaped = $SmtpPassword.Replace("'", "''")
$sql = @"
SET NOCOUNT ON;
UPDATE [dbo].[EPOSTA_SERVISLERI]
SET
    [SMTP_SIFRE] = N'$escaped',
    [AKTIF_MI] = 1,
    [TEST_MODU] = 0,
    [SON_HATA_MESAJI] = NULL,
    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
WHERE [SERVIS_KODU] LIKE N'platform[_]%';
SELECT [SERVIS_KODU], [GONDEREN_EPOSTA], [AKTIF_MI], LEN([SMTP_SIFRE]) AS pwd_len
FROM [dbo].[EPOSTA_SERVISLERI]
ORDER BY [ID];
"@

sqlcmd -S $Server -d $Database -U $User -P $Password -I -f 65001 -b -Q $sql
