-- Misafir yorumlari: 1-10 oda/konum/fiyat/personel, seyahat profili, genel memnuniyet (1-5).
-- Legacy 1-5 alanlari korunur; bos yeni sutunlar eski degerlerden geriye donuk uretilir.

IF COL_LENGTH(N'dbo.yorumlar', N'seyahat_profili') IS NULL
BEGIN
    ALTER TABLE dbo.yorumlar ADD seyahat_profili NVARCHAR(40) NULL;
END;

IF COL_LENGTH(N'dbo.yorumlar', N'memnuniyet_seviyesi') IS NULL
BEGIN
    ALTER TABLE dbo.yorumlar ADD memnuniyet_seviyesi TINYINT NULL;
END;

IF COL_LENGTH(N'dbo.yorumlar', N'genel_puan_10') IS NULL
BEGIN
    ALTER TABLE dbo.yorumlar ADD genel_puan_10 TINYINT NULL;
END;

IF COL_LENGTH(N'dbo.yorumlar', N'puan_oda_10') IS NULL
BEGIN
    ALTER TABLE dbo.yorumlar ADD puan_oda_10 TINYINT NULL;
    ALTER TABLE dbo.yorumlar ADD puan_konum_10 TINYINT NULL;
    ALTER TABLE dbo.yorumlar ADD puan_fiyat_10 TINYINT NULL;
    ALTER TABLE dbo.yorumlar ADD puan_personel_10 TINYINT NULL;
END;

EXEC(N'
UPDATE dbo.yorumlar
SET
    genel_puan_10 = COALESCE(
        genel_puan_10,
        CAST(CASE
            WHEN genel_puan <= 5 THEN CAST(genel_puan AS SMALLINT) * 2
            WHEN genel_puan <= 10 THEN CAST(genel_puan AS SMALLINT)
            ELSE 10
        END AS TINYINT)),
    puan_oda_10 = COALESCE(
        puan_oda_10,
        CAST(CASE
            WHEN konfor_puani <= 5 THEN CAST(konfor_puani AS SMALLINT) * 2
            WHEN konfor_puani <= 10 THEN CAST(konfor_puani AS SMALLINT)
            ELSE 10
        END AS TINYINT)),
    puan_konum_10 = COALESCE(
        puan_konum_10,
        CAST(CASE
            WHEN konum_puani <= 5 THEN CAST(konum_puani AS SMALLINT) * 2
            WHEN konum_puani <= 10 THEN CAST(konum_puani AS SMALLINT)
            ELSE 10
        END AS TINYINT)),
    puan_fiyat_10 = COALESCE(
        puan_fiyat_10,
        CAST(CASE
            WHEN fiyat_performans_puani <= 5 THEN CAST(fiyat_performans_puani AS SMALLINT) * 2
            WHEN fiyat_performans_puani <= 10 THEN CAST(fiyat_performans_puani AS SMALLINT)
            ELSE 10
        END AS TINYINT)),
    puan_personel_10 = COALESCE(
        puan_personel_10,
        CAST(CASE
            WHEN personel_puani <= 5 THEN CAST(personel_puani AS SMALLINT) * 2
            WHEN personel_puani <= 10 THEN CAST(personel_puani AS SMALLINT)
            ELSE 10
        END AS TINYINT))
WHERE genel_puan_10 IS NULL
   OR puan_oda_10 IS NULL
   OR puan_konum_10 IS NULL
   OR puan_fiyat_10 IS NULL
   OR puan_personel_10 IS NULL;
');
