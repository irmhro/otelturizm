-- Idempotent: yayında oteller için enlem/boylam backfill (mahalle → ilçe → il) — UTF-8
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.oteller', N'U') IS NULL RETURN;

-- 1) mahalle_id varsa MAHALLELER koordinatları
IF COL_LENGTH(N'dbo.oteller', N'mahalle_id') IS NOT NULL
   AND OBJECT_ID(N'dbo.mahalleler', N'U') IS NOT NULL
BEGIN
    UPDATE o
    SET
        o.enlem = m.enlem,
        o.boylam = m.boylam,
        o.guncellenme_tarihi = SYSUTCDATETIME()
    FROM dbo.oteller o
    INNER JOIN dbo.mahalleler m ON m.id = o.mahalle_id
    WHERE o.yayin_durumu = N'Yayında'
      AND (o.enlem IS NULL OR o.boylam IS NULL)
      AND m.enlem IS NOT NULL
      AND m.boylam IS NOT NULL
      AND m.aktif_mi = 1;
END;

-- 2) ilce_id ile ILCELER koordinatları
IF OBJECT_ID(N'dbo.ilceler', N'U') IS NOT NULL
BEGIN
    UPDATE o
    SET
        o.enlem = ic.enlem,
        o.boylam = ic.boylam,
        o.guncellenme_tarihi = SYSUTCDATETIME()
    FROM dbo.oteller o
    INNER JOIN dbo.ilceler ic ON ic.id = o.ilce_id
    WHERE o.yayin_durumu = N'Yayında'
      AND (o.enlem IS NULL OR o.boylam IS NULL)
      AND ic.enlem IS NOT NULL
      AND ic.boylam IS NOT NULL
      AND ic.aktif_mi = 1;
END;

-- 3) sehir_id ile ILLER koordinatları
IF OBJECT_ID(N'dbo.iller', N'U') IS NOT NULL
BEGIN
    UPDATE o
    SET
        o.enlem = il.enlem,
        o.boylam = il.boylam,
        o.guncellenme_tarihi = SYSUTCDATETIME()
    FROM dbo.oteller o
    INNER JOIN dbo.iller il ON il.id = o.sehir_id
    WHERE o.yayin_durumu = N'Yayında'
      AND (o.enlem IS NULL OR o.boylam IS NULL)
      AND il.enlem IS NOT NULL
      AND il.boylam IS NOT NULL
      AND il.aktif_mi = 1;
END;

PRINT N'20260610_seed_otel_koordinat_backfill tamamlandı.';
