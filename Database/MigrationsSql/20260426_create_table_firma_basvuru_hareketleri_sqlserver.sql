IF OBJECT_ID(N'dbo.firma_basvuru_hareketleri', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.firma_basvuru_hareketleri
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        firma_id BIGINT NOT NULL,
        onceki_durum NVARCHAR(40) NULL,
        yeni_durum NVARCHAR(40) NOT NULL,
        hareket_tipi NVARCHAR(80) NOT NULL,
        aciklama NVARCHAR(MAX) NULL,
        islem_yapan_kullanici_id BIGINT NULL,
        islem_kaynagi NVARCHAR(50) NOT NULL CONSTRAINT DF_firma_basvuru_hareketleri_kaynak DEFAULT 'system',
        ip_adresi NVARCHAR(64) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_firma_basvuru_hareketleri_olusturulma DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_firma_basvuru_hareketleri_firma_tarih
        ON dbo.firma_basvuru_hareketleri (firma_id, olusturulma_tarihi DESC);

    CREATE INDEX IX_firma_basvuru_hareketleri_durum_tarih
        ON dbo.firma_basvuru_hareketleri (yeni_durum, olusturulma_tarihi DESC);
END

