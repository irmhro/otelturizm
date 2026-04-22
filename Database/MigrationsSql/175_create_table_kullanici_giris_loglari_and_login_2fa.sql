IF OBJECT_ID('dbo.kullanici_giris_loglari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.kullanici_giris_loglari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kullanici_id BIGINT NOT NULL,
        hesap_tipi NVARCHAR(20) NOT NULL CONSTRAINT DF_kullanici_giris_loglari_hesap DEFAULT 'user',
        ip_adresi NVARCHAR(80) NULL,
        user_agent NVARCHAR(500) NULL,
        cihaz_etiketi NVARCHAR(150) NULL,
        giris_tarihi DATETIME2 NOT NULL CONSTRAINT DF_kullanici_giris_loglari_giris DEFAULT SYSUTCDATETIME(),
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_kullanici_giris_loglari_created DEFAULT SYSUTCDATETIME()
    );
    ALTER TABLE dbo.kullanici_giris_loglari ADD CONSTRAINT FK_kullanici_giris_loglari_users FOREIGN KEY (kullanici_id) REFERENCES dbo.users(id) ON DELETE CASCADE;
    CREATE INDEX IX_kullanici_giris_loglari_user ON dbo.kullanici_giris_loglari(kullanici_id, giris_tarihi DESC);
END;

IF OBJECT_ID('dbo.kullanici_giris_2fa_tokenlari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.kullanici_giris_2fa_tokenlari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kullanici_id BIGINT NOT NULL,
        telefon_e164 NVARCHAR(32) NOT NULL,
        dogrulama_kodu_hash NVARCHAR(128) NOT NULL,
        deneme_sayisi SMALLINT NOT NULL CONSTRAINT DF_kullanici_giris_2fa_deneme DEFAULT 0,
        maksimum_deneme SMALLINT NOT NULL CONSTRAINT DF_kullanici_giris_2fa_max DEFAULT 5,
        kullanildi_mi BIT NOT NULL CONSTRAINT DF_kullanici_giris_2fa_kullanildi DEFAULT 0,
        kullanilma_tarihi DATETIME2 NULL,
        gecerlilik_suresi DATETIME2 NOT NULL,
        ip_adresi NVARCHAR(80) NULL,
        user_agent NVARCHAR(500) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_kullanici_giris_2fa_created DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_kullanici_giris_2fa_updated DEFAULT SYSUTCDATETIME()
    );
    ALTER TABLE dbo.kullanici_giris_2fa_tokenlari ADD CONSTRAINT FK_kullanici_giris_2fa_tokenlari_users FOREIGN KEY (kullanici_id) REFERENCES dbo.users(id) ON DELETE CASCADE;
    CREATE INDEX IX_kullanici_giris_2fa_user ON dbo.kullanici_giris_2fa_tokenlari(kullanici_id, olusturulma_tarihi DESC);
    CREATE INDEX IX_kullanici_giris_2fa_phone ON dbo.kullanici_giris_2fa_tokenlari(telefon_e164, olusturulma_tarihi DESC);
END;

