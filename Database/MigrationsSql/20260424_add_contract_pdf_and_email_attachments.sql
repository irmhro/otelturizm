/*
  2026-04-24
  Sözleşmeler için PDF dosyası saklama ve e-posta ekleri altyapısı.

  - bildirim_loglari: ekler_json (email ekleri listesi)
  - sozlesme_dosyalari: sözleşmeye bağlı dosyalar (pdf vb.)
*/

IF COL_LENGTH('dbo.bildirim_loglari', 'ekler_json') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari
    ADD ekler_json NVARCHAR(MAX) NULL;
END
GO

IF OBJECT_ID('dbo.sozlesme_dosyalari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.sozlesme_dosyalari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        sozlesme_id BIGINT NOT NULL,
        dosya_tipi NVARCHAR(40) NOT NULL, -- pdf
        dosya_adi NVARCHAR(250) NULL,
        dosya_yolu NVARCHAR(500) NOT NULL, -- /uploads/contracts/{contractId}/{file}.pdf veya tam URL
        mime_tipi NVARCHAR(120) NULL,
        olusturan_kullanici_id BIGINT NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_sozlesme_dosyalari_olusturulma DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_sozlesme_dosyalari_sozlesme_id ON dbo.sozlesme_dosyalari(sozlesme_id);
    CREATE INDEX IX_sozlesme_dosyalari_tipi ON dbo.sozlesme_dosyalari(dosya_tipi);
END
GO

