-- Idempotent: demo / seed otelleri yayında + onaylı (liste/harita görünürlüğü)
-- Önce: 20260526_fix_yayin_onay_unicode.sql (varsa)
IF OBJECT_ID(N'dbo.OTELLER', N'U') IS NULL
BEGIN
    PRINT N'OTELLER tablosu yok — atlandı';
    RETURN;
END;

DECLARE @Yayinda nvarchar(20) = N'Yay' + NCHAR(0x0131) + N'nda';
DECLARE @Onaylandi nvarchar(20) = N'Onayland' + NCHAR(0x0131);

UPDATE o
SET
    [YAYIN_DURUMU] = @Yayinda,
    [ONAY_DURUMU] = @Onaylandi,
    [ONAY_TARIHI] = COALESCE(o.[ONAY_TARIHI], SYSUTCDATETIME())
FROM [dbo].[OTELLER] o
WHERE (
        o.[YAYIN_DURUMU] IS NULL
        OR LTRIM(RTRIM(o.[YAYIN_DURUMU])) = N''
        OR LOWER(REPLACE(LTRIM(RTRIM(o.[YAYIN_DURUMU])), NCHAR(0x0131), N'i')) NOT IN (N'yayinda', N'yayında')
      )
  AND (
        o.[EPOSTA] LIKE N'%@demo.otelturizm.local'
        OR o.[EPOSTA] LIKE N'irmhro0+%@gmail.com'
        OR o.[OTEL_KODU] LIKE N'ORK-SEED-%'
        OR o.[OTEL_KODU] LIKE N'ORK-IST-%'
        OR o.[OTEL_KODU] LIKE N'ILCE-%'
      );

PRINT CONCAT(N'Yayına alınan demo otel sayısı: ', @@ROWCOUNT);
