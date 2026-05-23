-- Idempotent: Yayin/Onay durumu Unicode normalizasyonu (Turkce dotless i U+0131)
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Yayinda nvarchar(20) = N'Yay' + NCHAR(0x0131) + N'nda';
DECLARE @Onaylandi nvarchar(20) = N'Onayland' + NCHAR(0x0131);

UPDATE [dbo].[OTELLER]
SET [YAYIN_DURUMU] = @Yayinda
WHERE [YAYIN_DURUMU] IS NOT NULL
  AND LOWER(REPLACE(LTRIM(RTRIM([YAYIN_DURUMU])), NCHAR(0x0131), N'i')) IN (N'yayinda', N'yayında', N'yayin');

UPDATE [dbo].[OTELLER]
SET [ONAY_DURUMU] = @Onaylandi
WHERE [ONAY_DURUMU] IS NOT NULL
  AND LOWER(REPLACE(LTRIM(RTRIM([ONAY_DURUMU])), NCHAR(0x0131), N'i')) IN (N'onaylandi', N'onaylandı', N'onaylanmis', N'onaylanmış', N'onayli');

DECLARE @YayindaCount int = (SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [YAYIN_DURUMU] = @Yayinda);
PRINT N'Yayin/Onay Unicode fix tamam. Yayinda: ' + CAST(@YayindaCount AS nvarchar(12));

-- Mojibake (UTF-8 i/ı as C4+B1) ve ORK-IST demo otelleri
UPDATE [dbo].[OTELLER]
SET [YAYIN_DURUMU] = @Yayinda,
    [ONAY_DURUMU] = @Onaylandi
WHERE [OTEL_KODU] LIKE N'ORK-IST-%'
   OR [OTEL_KODU] LIKE N'ORK-SEED-%'
   OR [EPOSTA] LIKE N'%@demo.otelturizm.local'
   OR [EPOSTA] LIKE N'irmhro0+%@gmail.com';
