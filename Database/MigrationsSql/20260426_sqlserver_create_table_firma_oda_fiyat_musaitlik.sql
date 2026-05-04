-- SQL Server create/upgrade (idempotent): dbo.firma_oda_fiyat_musaitlik
-- Not: Apply-SqlServerMigrations.ps1 sadece adinda "sqlserver" gecen dosyalari uygular.

IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.firma_oda_fiyat_musaitlik
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        firma_id BIGINT NOT NULL,
        otel_id BIGINT NOT NULL,
        oda_tip_id BIGINT NOT NULL,
        tarih DATE NOT NULL,
        firma_gecelik_fiyat DECIMAL(10,2) NOT NULL,
        minimum_geceleme TINYINT NULL,
        maksimum_geceleme SMALLINT NULL,
        kapali_satis BIT NOT NULL CONSTRAINT DF_firma_ofm_kapali DEFAULT 0,
        aktif_mi BIT NOT NULL CONSTRAINT DF_firma_ofm_aktif DEFAULT 1,
        fiyat_notu NVARCHAR(255) NULL,
        guncelleyen_kullanici_id BIGINT NULL,
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_firma_ofm_guncellenme DEFAULT SYSUTCDATETIME()
    );
END

-- Kolonlar sonradan eklendiyse geriye donuk tamamla (idempotent)
IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'firma_id') IS NULL
    BEGIN
        ALTER TABLE dbo.firma_oda_fiyat_musaitlik
        ADD firma_id BIGINT NOT NULL CONSTRAINT DF_firma_ofm_firma_id DEFAULT 0 WITH VALUES;
    END

    IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'aktif_mi') IS NULL
    BEGIN
        ALTER TABLE dbo.firma_oda_fiyat_musaitlik
        ADD aktif_mi BIT NOT NULL CONSTRAINT DF_firma_ofm_aktif DEFAULT 1;
    END
END

-- Index standardi (idempotent)
IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NOT NULL
BEGIN
    -- En dogru uniqueness: firma + otel + oda + tarih (farkli otellerde ayni oda_tip_id olabilen senaryolar icin)
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_firma_oda_fiyat_musaitlik_firma_otel_oda_tarih'
          AND object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik')
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_firma_oda_fiyat_musaitlik_firma_otel_oda_tarih
        ON dbo.firma_oda_fiyat_musaitlik(firma_id, otel_id, oda_tip_id, tarih);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_firma_oda_fiyat_musaitlik_otel_room_date'
          AND object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_firma_oda_fiyat_musaitlik_otel_room_date
        ON dbo.firma_oda_fiyat_musaitlik(otel_id, oda_tip_id, tarih);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_firma_oda_fiyat_musaitlik_firma_date'
          AND object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_firma_oda_fiyat_musaitlik_firma_date
        ON dbo.firma_oda_fiyat_musaitlik(firma_id, tarih);
    END
END

