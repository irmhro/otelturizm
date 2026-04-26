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
        fiyat_notu NVARCHAR(255) NULL,
        guncelleyen_kullanici_id BIGINT NULL,
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_firma_ofm_guncellenme DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UX_firma_ofm_firma_room_date
        ON dbo.firma_oda_fiyat_musaitlik (firma_id, oda_tip_id, tarih);

    CREATE INDEX IX_firma_ofm_otel_room_date
        ON dbo.firma_oda_fiyat_musaitlik (otel_id, oda_tip_id, tarih);

    CREATE INDEX IX_firma_ofm_firma_date
        ON dbo.firma_oda_fiyat_musaitlik (firma_id, tarih);
END

