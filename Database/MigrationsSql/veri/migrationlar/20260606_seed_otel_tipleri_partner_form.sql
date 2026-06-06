-- Partner kayit formu otel tipi kodlari (idempotent)
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.OTEL_TIPLERI', N'U') IS NULL
BEGIN
    RAISERROR(N'OTEL_TIPLERI tablosu bulunamadi.', 16, 1);
    RETURN;
END;

DECLARE @t TABLE (KOD nvarchar(60) NOT NULL, TIP_ADI nvarchar(100) NOT NULL, SIRALAMA int NOT NULL);
INSERT INTO @t (KOD, TIP_ADI, SIRALAMA) VALUES
(N'otel', N'Otel', 10),
(N'sehir-oteli', N'Şehir Oteli', 20),
(N'butik-otel', N'Butik Otel', 30),
(N'apart-otel', N'Apart Otel', 40),
(N'pansiyon', N'Pansiyon', 50),
(N'hostel', N'Hostel', 60),
(N'motel', N'Motel', 70),
(N'resort', N'Resort', 80),
(N'tatil-koyu', N'Tatil Köyü', 90),
(N'termal-otel', N'Termal Otel', 100),
(N'spa-oteli', N'SPA Oteli', 110),
(N'is-oteli', N'İş Oteli', 120),
(N'havaalani-oteli', N'Havaalanı Oteli', 130),
(N'deniz-oteli', N'Deniz Oteli', 140),
(N'dag-oteli', N'Dağ Oteli', 150),
(N'kayak-oteli', N'Kayak Oteli', 160),
(N'bungalov', N'Bungalov', 170),
(N'villa', N'Villa', 180),
(N'apart-daire', N'Apart Daire', 190),
(N'rezidans', N'Rezidans', 200),
(N'konukevi', N'Konukevi', 210),
(N'ciftlik-evi', N'Çiftlik Evi', 220),
(N'tas-otel', N'Taş Otel', 230),
(N'magara-otel', N'Mağara Otel', 240),
(N'kamp-alani', N'Kamp Alanı', 250),
(N'glamping', N'Glamping', 260);

INSERT INTO [dbo].[OTEL_TIPLERI] ([KOD], [TIP_ADI], [ACIKLAMA], [AKTIF_MI], [SIRALAMA], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
SELECT s.KOD, s.TIP_ADI, N'Partner basvuru formu otel tipi', 1, s.SIRALAMA, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @t s
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_TIPLERI] t WHERE LOWER(LTRIM(RTRIM(t.[KOD]))) = LOWER(LTRIM(RTRIM(s.KOD))));

UPDATE t
SET [TIP_ADI] = s.TIP_ADI, [AKTIF_MI] = 1, [SIRALAMA] = s.SIRALAMA, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
FROM [dbo].[OTEL_TIPLERI] t
INNER JOIN @t s ON LOWER(LTRIM(RTRIM(t.[KOD]))) = LOWER(LTRIM(RTRIM(s.KOD)));

-- Eski HOTEL kodu kalsin; partner form 'otel' slug kullanir
UPDATE [dbo].[OTEL_TIPLERI] SET [AKTIF_MI] = 1 WHERE [KOD] = N'HOTEL';

SELECT [KOD], [TIP_ADI], [AKTIF_MI] FROM [dbo].[OTEL_TIPLERI] WHERE [AKTIF_MI] = 1 ORDER BY [SIRALAMA], [ID];
