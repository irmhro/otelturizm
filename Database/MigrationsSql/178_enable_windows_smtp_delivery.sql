SET NOCOUNT ON;

UPDATE email_services
SET
    aktif_mi = 1,
    varsayilan_mi = CASE WHEN servis_kodu = 'default_smtp' THEN 1 ELSE varsayilan_mi END,
    test_modu = 0,
    guvenlik_tipi = CASE
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(guvenlik_tipi, ''))), '') IS NULL THEN 'SSL'
        ELSE guvenlik_tipi
    END,
    baglanti_zaman_asimi_saniye = CASE
        WHEN baglanti_zaman_asimi_saniye IS NULL OR baglanti_zaman_asimi_saniye < 60 THEN 60
        ELSE baglanti_zaman_asimi_saniye
    END,
    metadata = JSON_MODIFY(
        JSON_MODIFY(
            CASE
                WHEN ISJSON(metadata) = 1 THEN metadata
                ELSE '{}'
            END,
            '$.pickup_directory',
            'C:\\inetpub\\mailroot\\Pickup'
        ),
        '$.transport_mode',
        'smtp'
    ),
    guncellenme_tarihi = SYSUTCDATETIME()
WHERE servis_kodu = 'default_smtp';

IF OBJECT_ID('dbo.schema_migrations', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM dbo.schema_migrations
    WHERE script_name = '178_enable_windows_smtp_delivery.sql'
)
BEGIN
    INSERT INTO dbo.schema_migrations (script_name, checksum, applied_at)
    VALUES ('178_enable_windows_smtp_delivery.sql', 'manual-update', SYSUTCDATETIME());
END;
