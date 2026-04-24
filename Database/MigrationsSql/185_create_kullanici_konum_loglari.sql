IF OBJECT_ID(N'dbo.kullanici_konum_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.kullanici_konum_loglari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id BIGINT NULL,
        enlem DECIMAL(10,7) NOT NULL,
        boylam DECIMAL(10,7) NOT NULL,
        kaynak NVARCHAR(50) NULL,
        kullanici_ajan NVARCHAR(500) NULL,
        ip_adresi NVARCHAR(64) NULL,
        kayit_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_kullanici_konum_loglari_kayit_tarihi DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_kullanici_konum_loglari_user_id_kayit_tarihi
        ON dbo.kullanici_konum_loglari (user_id, kayit_tarihi DESC);
END
