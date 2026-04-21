IF COL_LENGTH('dbo.users', 'telefon_e164') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD telefon_e164 NVARCHAR(32) NULL;
END;

IF COL_LENGTH('dbo.users', 'telefon_dogrulama_kanali') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD telefon_dogrulama_kanali NVARCHAR(30) NULL;
END;

IF COL_LENGTH('dbo.users', 'telefon_dogrulama_durumu') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD telefon_dogrulama_durumu NVARCHAR(30) NULL;
END;

IF COL_LENGTH('dbo.users', 'telefon_son_dogrulama_istek_tarihi') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD telefon_son_dogrulama_istek_tarihi DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.users', 'telefon_son_sahiplik_teyit_tarihi') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD telefon_son_sahiplik_teyit_tarihi DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.users', 'telefon_degistirilme_tarihi') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD telefon_degistirilme_tarihi DATETIME2 NULL;
END;

EXEC sys.sp_executesql N'
UPDATE dbo.users
SET telefon_e164 = CASE
        WHEN telefon IS NULL OR LTRIM(RTRIM(telefon)) = '''' THEN NULL
        WHEN telefon_e164 IS NOT NULL AND LTRIM(RTRIM(telefon_e164)) <> '''' THEN telefon_e164
        ELSE
            CASE
                WHEN LEFT(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(telefon, '' '', ''''), ''-'', ''''), ''('', ''''), '')'', ''''), ''+'', ''''), 2) = ''90''
                    THEN ''+'' + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(telefon, '' '', ''''), ''-'', ''''), ''('', ''''), '')'', ''''), ''+'', '''')
                WHEN LEFT(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(telefon, '' '', ''''), ''-'', ''''), ''('', ''''), '')'', ''''), ''+'', ''''), 1) = ''0''
                    THEN ''+9'' + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(telefon, '' '', ''''), ''-'', ''''), ''('', ''''), '')'', ''''), ''+'', '''')
                ELSE ''+90'' + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(telefon, '' '', ''''), ''-'', ''''), ''('', ''''), '')'', ''''), ''+'', '''')
            END
    END,
    telefon_dogrulama_kanali = CASE
        WHEN telefon_dogrulama_kanali IS NULL AND telefon_dogrulama_tarihi IS NOT NULL THEN ''whatsapp''
        ELSE telefon_dogrulama_kanali
    END,
    telefon_dogrulama_durumu = CASE
        WHEN telefon_dogrulama_durumu IS NULL AND telefon_dogrulama_tarihi IS NOT NULL THEN ''Dogrulandi''
        WHEN telefon_dogrulama_durumu IS NULL AND telefon IS NOT NULL AND LTRIM(RTRIM(telefon)) <> '''' THEN ''Dogrulanmadi''
        ELSE telefon_dogrulama_durumu
    END,
    telefon_son_sahiplik_teyit_tarihi = CASE
        WHEN telefon_son_sahiplik_teyit_tarihi IS NULL THEN telefon_dogrulama_tarihi
        ELSE telefon_son_sahiplik_teyit_tarihi
    END
WHERE telefon IS NOT NULL
   OR telefon_dogrulama_tarihi IS NOT NULL;';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_telefon_e164' AND object_id = OBJECT_ID('dbo.users'))
BEGIN
    EXEC sys.sp_executesql N'CREATE INDEX IX_users_telefon_e164 ON dbo.users(telefon_e164);';
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_telefon_dogrulama_durumu' AND object_id = OBJECT_ID('dbo.users'))
BEGIN
    EXEC sys.sp_executesql N'CREATE INDEX IX_users_telefon_dogrulama_durumu ON dbo.users(telefon_dogrulama_durumu, telefon_dogrulama_tarihi);';
END;

IF OBJECT_ID('dbo.whatsapp_cloud_api_ayarlari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.whatsapp_cloud_api_ayarlari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        app_id NVARCHAR(100) NULL,
        app_secret_encrypted NVARCHAR(MAX) NULL,
        business_account_id NVARCHAR(100) NULL,
        phone_number_id NVARCHAR(100) NULL,
        permanent_access_token_encrypted NVARCHAR(MAX) NULL,
        webhook_verify_token_encrypted NVARCHAR(MAX) NULL,
        verification_template_name NVARCHAR(120) NULL,
        default_language_code NVARCHAR(20) NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_lang DEFAULT 'tr',
        otp_code_length TINYINT NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_code DEFAULT 6,
        otp_ttl_seconds INT NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_ttl DEFAULT 300,
        resend_cooldown_seconds INT NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_cooldown DEFAULT 60,
        max_attempt_count TINYINT NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_attempt DEFAULT 5,
        phone_reverify_after_days INT NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_reverify DEFAULT 180,
        reservation_phone_verification_required BIT NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_reservation DEFAULT 0,
        is_active BIT NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_active DEFAULT 0,
        test_recipient_phone_e164 NVARCHAR(32) NULL,
        last_test_message_at DATETIME2 NULL,
        created_by_user_id BIGINT NULL,
        updated_by_user_id BIGINT NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_created DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_whatsapp_cloud_api_ayarlari_updated DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.telefon_dogrulama_tokenlari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.telefon_dogrulama_tokenlari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kullanici_id BIGINT NOT NULL,
        telefon_raw NVARCHAR(32) NULL,
        telefon_e164 NVARCHAR(32) NOT NULL,
        dogrulama_kodu_hash NVARCHAR(128) NOT NULL,
        dogrulama_kanali NVARCHAR(30) NOT NULL CONSTRAINT DF_telefon_dogrulama_tokenlari_kanal DEFAULT 'whatsapp',
        meta_mesaj_id NVARCHAR(120) NULL,
        talep_durumu NVARCHAR(40) NOT NULL CONSTRAINT DF_telefon_dogrulama_tokenlari_durum DEFAULT 'Hazirlaniyor',
        deneme_sayisi SMALLINT NOT NULL CONSTRAINT DF_telefon_dogrulama_tokenlari_deneme DEFAULT 0,
        maksimum_deneme SMALLINT NOT NULL CONSTRAINT DF_telefon_dogrulama_tokenlari_max DEFAULT 5,
        kullanildi_mi BIT NOT NULL CONSTRAINT DF_telefon_dogrulama_tokenlari_kullanildi DEFAULT 0,
        kullanilma_tarihi DATETIME2 NULL,
        gecerlilik_suresi DATETIME2 NOT NULL,
        son_hata_mesaji NVARCHAR(500) NULL,
        ip_adresi NVARCHAR(80) NULL,
        user_agent NVARCHAR(500) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_telefon_dogrulama_tokenlari_created DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_telefon_dogrulama_tokenlari_updated DEFAULT SYSUTCDATETIME()
    );
    ALTER TABLE dbo.telefon_dogrulama_tokenlari ADD CONSTRAINT FK_telefon_dogrulama_tokenlari_users FOREIGN KEY (kullanici_id) REFERENCES dbo.users(id);
    CREATE INDEX IX_telefon_dogrulama_tokenlari_user ON dbo.telefon_dogrulama_tokenlari(kullanici_id, olusturulma_tarihi DESC);
    CREATE INDEX IX_telefon_dogrulama_tokenlari_message ON dbo.telefon_dogrulama_tokenlari(meta_mesaj_id);
END;

IF OBJECT_ID('dbo.kullanici_telefon_gecmisi', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.kullanici_telefon_gecmisi
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kullanici_id BIGINT NOT NULL,
        onceki_telefon_raw NVARCHAR(32) NULL,
        onceki_telefon_e164 NVARCHAR(32) NULL,
        yeni_telefon_raw NVARCHAR(32) NULL,
        yeni_telefon_e164 NVARCHAR(32) NULL,
        dogrulama_durumu NVARCHAR(40) NULL,
        degisim_nedeni NVARCHAR(255) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_kullanici_telefon_gecmisi_created DEFAULT SYSUTCDATETIME()
    );
    ALTER TABLE dbo.kullanici_telefon_gecmisi ADD CONSTRAINT FK_kullanici_telefon_gecmisi_users FOREIGN KEY (kullanici_id) REFERENCES dbo.users(id);
    CREATE INDEX IX_kullanici_telefon_gecmisi_user ON dbo.kullanici_telefon_gecmisi(kullanici_id, olusturulma_tarihi DESC);
    CREATE INDEX IX_kullanici_telefon_gecmisi_old_phone ON dbo.kullanici_telefon_gecmisi(onceki_telefon_e164);
END;

IF OBJECT_ID('dbo.whatsapp_mesaj_loglari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.whatsapp_mesaj_loglari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kullanici_id BIGINT NULL,
        telefon_e164 NVARCHAR(32) NOT NULL,
        template_name NVARCHAR(120) NOT NULL,
        meta_mesaj_id NVARCHAR(120) NULL,
        delivery_status NVARCHAR(40) NULL,
        request_payload NVARCHAR(MAX) NULL,
        response_payload NVARCHAR(MAX) NULL,
        error_code NVARCHAR(50) NULL,
        error_message NVARCHAR(500) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_whatsapp_mesaj_loglari_created DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_whatsapp_mesaj_loglari_updated DEFAULT SYSUTCDATETIME()
    );
    ALTER TABLE dbo.whatsapp_mesaj_loglari ADD CONSTRAINT FK_whatsapp_mesaj_loglari_users FOREIGN KEY (kullanici_id) REFERENCES dbo.users(id);
    CREATE INDEX IX_whatsapp_mesaj_loglari_phone ON dbo.whatsapp_mesaj_loglari(telefon_e164, olusturulma_tarihi DESC);
    CREATE INDEX IX_whatsapp_mesaj_loglari_meta ON dbo.whatsapp_mesaj_loglari(meta_mesaj_id);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.whatsapp_cloud_api_ayarlari)
BEGIN
    INSERT INTO dbo.whatsapp_cloud_api_ayarlari
    (
        app_id, business_account_id, phone_number_id, verification_template_name, default_language_code,
        otp_code_length, otp_ttl_seconds, resend_cooldown_seconds, max_attempt_count, phone_reverify_after_days,
        reservation_phone_verification_required, is_active, olusturulma_tarihi, guncellenme_tarihi
    )
    VALUES
    (
        NULL, NULL, NULL, NULL, 'tr',
        6, 300, 60, 5, 180,
        1, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
END;
