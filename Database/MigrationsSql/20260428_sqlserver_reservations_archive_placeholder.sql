/*
  Paket 236–237: Soğuk arşiv hedef tablosu (minimal placeholder).
  Üretim şeması rezervasyonlar ile uyumlu olacak şekilde genişletilmelidir.
*/

IF OBJECT_ID(N'dbo.rezervasyonlar_archive', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.rezervasyonlar_archive (
        id BIGINT NOT NULL PRIMARY KEY,
        olusturulma_tarihi DATETIME2 NULL,
        durum NVARCHAR(64) NULL,
        arsiv_tarihi_utc DATETIME2 NOT NULL CONSTRAINT DF_rezervasyonlar_archive_arsiv DEFAULT SYSUTCDATETIME()
    );
END
