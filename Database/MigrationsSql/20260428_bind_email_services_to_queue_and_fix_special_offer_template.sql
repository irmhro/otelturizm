SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.bildirim_loglari', 'email_servis_kodu') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari
        ADD email_servis_kodu NVARCHAR(80) NULL;
END;
GO

IF COL_LENGTH('dbo.bildirim_loglari', 'gonderen_eposta_override') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari
        ADD gonderen_eposta_override NVARCHAR(320) NULL;
END;
GO

UPDATE dbo.bildirim_loglari
SET email_servis_kodu = N'default_smtp'
WHERE tur = N'E-posta'
  AND email_servis_kodu IS NULL;
GO

UPDATE dbo.bildirim_sablonlari
SET icerik = N'Views/Email/Ozel Teklif.cshtml'
WHERE sablon_kodu = N'ozel_teklif'
  AND tur = N'E-posta';
GO

UPDATE dbo.bildirim_sablonlari
SET icerik = N'Views/Email/Rezervasyon Onaylandi.cshtml'
WHERE sablon_kodu = N'rezervasyon_onay'
  AND tur = N'E-posta';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE script_name = N'20260428_bind_email_services_to_queue_and_fix_special_offer_template.sql')
BEGIN
    INSERT INTO dbo.schema_migrations (script_name, checksum, applied_at)
    VALUES (N'20260428_bind_email_services_to_queue_and_fix_special_offer_template.sql', N'20260428bindemailsvcqueue0000000000000000', SYSUTCDATETIME());
END;
