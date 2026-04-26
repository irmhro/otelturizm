IF OBJECT_ID(N'dbo.otel_koordinat_degisim_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.otel_koordinat_degisim_loglari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        admin_kullanici_id BIGINT NOT NULL,
        admin_ad_soyad NVARCHAR(160) NULL,
        otel_id BIGINT NOT NULL,
        otel_adi NVARCHAR(250) NULL,
        onceki_enlem DECIMAL(10,7) NULL,
        onceki_boylam DECIMAL(10,7) NULL,
        yeni_enlem DECIMAL(10,7) NULL,
        yeni_boylam DECIMAL(10,7) NULL,
        ip_adresi NVARCHAR(64) NULL,
        notlar NVARCHAR(500) NULL,
        kayit_tarihi DATETIME2 NOT NULL CONSTRAINT DF_otel_koordinat_degisim_loglari_kayit_tarihi DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_otel_koordinat_degisim_loglari_otel_id_kayit_tarihi
        ON dbo.otel_koordinat_degisim_loglari (otel_id, kayit_tarihi DESC);

    CREATE INDEX IX_otel_koordinat_degisim_loglari_admin_id_kayit_tarihi
        ON dbo.otel_koordinat_degisim_loglari (admin_kullanici_id, kayit_tarihi DESC);
END

