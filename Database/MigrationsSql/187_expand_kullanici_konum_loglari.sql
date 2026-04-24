IF OBJECT_ID(N'dbo.kullanici_konum_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.kullanici_konum_loglari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id BIGINT NULL,
        session_key NVARCHAR(120) NULL,
        enlem DECIMAL(10,7) NOT NULL,
        boylam DECIMAL(10,7) NOT NULL,
        yaricap_km INT NULL,
        gorunen_otel_sayisi INT NULL,
        arama_metni NVARCHAR(250) NULL,
        arama_bolgesi NVARCHAR(200) NULL,
        kaynak NVARCHAR(50) NULL,
        kullanici_ajan NVARCHAR(500) NULL,
        ip_adresi NVARCHAR(64) NULL,
        cihaz_tipi NVARCHAR(50) NULL,
        cihaz_modeli NVARCHAR(120) NULL,
        platform NVARCHAR(80) NULL,
        tarayici NVARCHAR(80) NULL,
        telefon_ipucu NVARCHAR(80) NULL,
        sayfa_url NVARCHAR(500) NULL,
        kayit_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_kullanici_konum_loglari_kayit_tarihi DEFAULT SYSUTCDATETIME()
    );
END

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'session_key') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD session_key NVARCHAR(120) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'yaricap_km') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD yaricap_km INT NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'gorunen_otel_sayisi') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD gorunen_otel_sayisi INT NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'arama_metni') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD arama_metni NVARCHAR(250) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'arama_bolgesi') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD arama_bolgesi NVARCHAR(200) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'cihaz_tipi') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD cihaz_tipi NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'cihaz_modeli') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD cihaz_modeli NVARCHAR(120) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'platform') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD platform NVARCHAR(80) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'tarayici') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD tarayici NVARCHAR(80) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'telefon_ipucu') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD telefon_ipucu NVARCHAR(80) NULL;

IF COL_LENGTH('dbo.kullanici_konum_loglari', 'sayfa_url') IS NULL
    ALTER TABLE dbo.kullanici_konum_loglari ADD sayfa_url NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_kullanici_konum_loglari_user_id_kayit_tarihi' AND object_id = OBJECT_ID(N'dbo.kullanici_konum_loglari'))
BEGIN
    CREATE INDEX IX_kullanici_konum_loglari_user_id_kayit_tarihi
        ON dbo.kullanici_konum_loglari (user_id, kayit_tarihi DESC);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_kullanici_konum_loglari_session_key_kayit_tarihi' AND object_id = OBJECT_ID(N'dbo.kullanici_konum_loglari'))
BEGIN
    CREATE INDEX IX_kullanici_konum_loglari_session_key_kayit_tarihi
        ON dbo.kullanici_konum_loglari (session_key, kayit_tarihi DESC);
END
