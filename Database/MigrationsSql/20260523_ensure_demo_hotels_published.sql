-- Idempotent: demo / seed otelleri yayında + onaylı (liste/harita görünürlüğü)
-- Önce: 20260526_fix_yayin_onay_unicode.sql (varsa)
IF OBJECT_ID(N'dbo.OTELLER', N'U') IS NULL
BEGIN
    PRINT 'OTELLER tablosu yok — atlandı';
    RETURN;
END;

UPDATE o
SET
    yayin_durumu = N'Yayında',
    onay_durumu = N'Onaylandı',
    guncelleme_tarihi = SYSUTCDATETIME()
FROM dbo.OTELLER o
WHERE (
        o.yayin_durumu IS NULL
        OR LTRIM(RTRIM(o.yayin_durumu)) = N''
        OR LOWER(REPLACE(LTRIM(RTRIM(o.yayin_durumu)), NCHAR(0x0131), N'i')) NOT IN (N'yayinda', N'yayında')
      )
  AND (
        o.eposta LIKE N'%@demo.otelturizm.local'
        OR o.eposta LIKE N'irmhro0+%@gmail.com'
        OR o.otel_kodu LIKE N'ORK-SEED-%'
        OR o.otel_kodu LIKE N'ILCE-%'
      );

PRINT CONCAT(N'Yayına alınan demo otel sayısı: ', @@ROWCOUNT);
