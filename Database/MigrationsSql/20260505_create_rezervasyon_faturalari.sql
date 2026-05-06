/*
  Rezervasyon bazlı misafir faturası yükleme (partner/firma).
  Güvenli dosya ile ilişkilidir; şema yoksa özellik pasif kalır (kod tablo varlığını kontrol eder).
*/

IF OBJECT_ID(N'dbo.rezervasyon_faturalari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rezervasyon_faturalari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        rezervasyon_id BIGINT NOT NULL,
        otel_id BIGINT NOT NULL,
        yukleyen_kullanici_id BIGINT NOT NULL,
        guvenli_dosya_id BIGINT NOT NULL,
        dosya_adi NVARCHAR(260) NULL,
        mime_tipi NVARCHAR(120) NULL,
        aciklama NVARCHAR(500) NULL,
        olusturulma_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_rezervasyon_faturalari_olustur DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_rezervasyon_faturalari_rez ON dbo.rezervasyon_faturalari(rezervasyon_id);
    CREATE INDEX IX_rezervasyon_faturalari_otel ON dbo.rezervasyon_faturalari(otel_id, olusturulma_tarihi DESC);

    ALTER TABLE dbo.rezervasyon_faturalari
        ADD CONSTRAINT FK_rezervasyon_faturalari_rez
        FOREIGN KEY (rezervasyon_id) REFERENCES dbo.rezervasyonlar(id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyon_faturalari') AND name = N'UX_rezervasyon_faturalari_rez')
BEGIN
    CREATE UNIQUE INDEX UX_rezervasyon_faturalari_rez ON dbo.rezervasyon_faturalari(rezervasyon_id);
END;

