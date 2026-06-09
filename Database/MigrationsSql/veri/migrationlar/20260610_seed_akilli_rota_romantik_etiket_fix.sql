SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.AKILLI_ROTA', N'U') IS NULL
BEGIN
    RAISERROR(N'AKILLI_ROTA tablosu bulunamadi.', 16, 1);
    RETURN;
END

UPDATE dbo.AKILLI_ROTA
SET
    [ETIKET_ADI] = N'Romantik',
    [HASHTAG] = N'#Romantik',
    [ARAMA_METNI] = N'romantik, çift tatili',
    [GUNCELLENME_TARIHI] = sysutcdatetime()
WHERE [ETIKET_KODU] = N'romantik-kacamak'
  AND (
        [ETIKET_ADI] <> N'Romantik'
     OR [HASHTAG] <> N'#Romantik'
     OR [ARAMA_METNI] <> N'romantik, çift tatili'
      );
GO
