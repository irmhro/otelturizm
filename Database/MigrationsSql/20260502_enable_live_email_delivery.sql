SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.email_services', N'U') IS NULL
BEGIN
    THROW 51000, 'email_services tablosu bulunamadi. Once email servis sema migrationlarini uygulayin.', 1;
END;

IF OBJECT_ID(N'dbo.bildirim_loglari', N'U') IS NULL
BEGIN
    THROW 51001, 'bildirim_loglari tablosu bulunamadi. Once bildirim kuyruk sema migrationlarini uygulayin.', 1;
END;

IF OBJECT_ID(N'dbo.bildirim_sablonlari', N'U') IS NULL
BEGIN
    THROW 51002, 'bildirim_sablonlari tablosu bulunamadi. Once bildirim sablon migrationlarini uygulayin.', 1;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'email_servis_kodu') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD email_servis_kodu NVARCHAR(80) NULL;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'gonderen_eposta_override') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD gonderen_eposta_override NVARCHAR(320) NULL;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'sonraki_deneme_utc') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD sonraki_deneme_utc DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'deneme_sayisi') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD deneme_sayisi INT NOT NULL CONSTRAINT DF_bildirim_loglari_deneme_sayisi DEFAULT (0);
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'maksimum_deneme_sayisi') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD maksimum_deneme_sayisi INT NOT NULL CONSTRAINT DF_bildirim_loglari_maksimum_deneme_sayisi DEFAULT (5);
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'saglayici_mesaj_id') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD saglayici_mesaj_id NVARCHAR(255) NULL;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'hata_kodu') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD hata_kodu NVARCHAR(80) NULL;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'hata_mesaji') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD hata_mesaji NVARCHAR(1000) NULL;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD guncellenme_tarihi DATETIME2 NULL;
END;

DECLARE @metadata NVARCHAR(MAX) = N'{
  "username": "info@otelturizm.com",
  "imap_host": "mail.otelturizm.com",
  "imap_port": 993,
  "pop3_host": "mail.otelturizm.com",
  "pop3_port": 995,
  "smtp_host": "umay.muvhost.com",
  "smtp_port": 587,
  "transport_mode": "smtp"
}';

IF EXISTS (SELECT 1 FROM dbo.email_services WHERE servis_kodu = N'default_smtp')
BEGIN
    UPDATE dbo.email_services
    SET servis_adi = N'Otelturizm SMTP',
        saglayici = N'SMTP',
        varsayilan_mi = 1,
        aktif_mi = 1,
        gonderen_ad = N'otelturizm.com',
        gonderen_eposta = N'rezervasyon@otelturizm.com',
        yanitla_eposta = N'destek@otelturizm.com',
        smtp_host = N'umay.muvhost.com',
        smtp_port = 587,
        smtp_kullanici_adi = COALESCE(NULLIF(smtp_kullanici_adi, N''), N'info@otelturizm.com'),
        guvenlik_tipi = N'STARTTLS',
        baglanti_zaman_asimi_saniye = 60,
        test_modu = 0,
        metadata = @metadata,
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE servis_kodu = N'default_smtp';
END
ELSE
BEGIN
    INSERT INTO dbo.email_services
    (servis_kodu, servis_adi, saglayici, varsayilan_mi, aktif_mi, gonderen_ad, gonderen_eposta, yanitla_eposta, smtp_host, smtp_port, smtp_kullanici_adi, smtp_sifre, guvenlik_tipi, baglanti_zaman_asimi_saniye, test_modu, metadata, olusturulma_tarihi)
    VALUES
    (N'default_smtp', N'Otelturizm SMTP', N'SMTP', 1, 1, N'otelturizm.com', N'rezervasyon@otelturizm.com', N'destek@otelturizm.com', N'umay.muvhost.com', 587, N'info@otelturizm.com', N'', N'STARTTLS', 60, 0, @metadata, SYSUTCDATETIME());
END;

UPDATE dbo.email_services
SET varsayilan_mi = CASE WHEN servis_kodu = N'default_smtp' THEN 1 ELSE 0 END
WHERE aktif_mi = 1;

UPDATE dbo.bildirim_loglari
SET email_servis_kodu = N'default_smtp'
WHERE tur = N'E-posta'
  AND (email_servis_kodu IS NULL OR LTRIM(RTRIM(email_servis_kodu)) = N'');

-- Canlı kimlik eksikse migration'ı kırmadan test moduna al (yerel/kurulum); üretimde şifreyi doldurun.
UPDATE dbo.email_services
SET test_modu = 1,
    guncellenme_tarihi = SYSUTCDATETIME()
WHERE servis_kodu = N'default_smtp'
  AND aktif_mi = 1
  AND test_modu = 0
  AND (
      smtp_host IS NULL OR LTRIM(RTRIM(smtp_host)) = N''
      OR smtp_kullanici_adi IS NULL OR LTRIM(RTRIM(smtp_kullanici_adi)) = N''
      OR smtp_sifre IS NULL OR LTRIM(RTRIM(smtp_sifre)) = N''
  );

IF @@ROWCOUNT > 0
BEGIN
    PRINT N'UYARI: default_smtp canlı SMTP kimlik bilgisi eksik — test_modu=1 yapıldı. Üretimde smtp_sifre/host/kullanici doldurun.';
END;

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE script_name = N'20260502_enable_live_email_delivery.sql')
BEGIN
    INSERT INTO dbo.schema_migrations (script_name, checksum, applied_at)
    VALUES (N'20260502_enable_live_email_delivery.sql', N'20260502enableliveemaildelivery00000001', SYSUTCDATETIME());
END;
