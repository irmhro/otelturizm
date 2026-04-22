-- Onayli yorumlardan oteller ozet puanlarini yeniden hesaplar (tum oteller).
-- onay_durumu LIKE N'Onaylan%' ile uyumlu.

;WITH agg AS (
    SELECT
        y.otel_id,
        COUNT(*) AS cnt,
        CAST(ROUND(AVG(CAST(CASE
            WHEN y.genel_puan <= 5 THEN CAST(y.genel_puan AS DECIMAL(9, 4)) * 2
            WHEN y.genel_puan <= 10 THEN CAST(y.genel_puan AS DECIMAL(9, 4))
            ELSE 10 END AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_genel,
        CAST(ROUND(AVG(CAST(CASE
            WHEN y.konum_puani <= 5 THEN CAST(y.konum_puani AS DECIMAL(9, 4)) * 2
            WHEN y.konum_puani <= 10 THEN CAST(y.konum_puani AS DECIMAL(9, 4))
            ELSE 10 END AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_konum,
        CAST(ROUND(AVG(CAST(CASE
            WHEN y.konfor_puani <= 5 THEN CAST(y.konfor_puani AS DECIMAL(9, 4)) * 2
            WHEN y.konfor_puani <= 10 THEN CAST(y.konfor_puani AS DECIMAL(9, 4))
            ELSE 10 END AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_konfor,
        CAST(ROUND(AVG(CAST(CASE
            WHEN y.fiyat_performans_puani <= 5 THEN CAST(y.fiyat_performans_puani AS DECIMAL(9, 4)) * 2
            WHEN y.fiyat_performans_puani <= 10 THEN CAST(y.fiyat_performans_puani AS DECIMAL(9, 4))
            ELSE 10 END AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_fp
    FROM yorumlar AS y
    WHERE y.onay_durumu LIKE N'Onaylan%'
    GROUP BY y.otel_id
)
UPDATE o
SET
    o.toplam_yorum_sayisi = agg.cnt,
    o.ortalama_puan = agg.avg_genel,
    o.konum_puani = agg.avg_konum,
    o.konfor_puani = agg.avg_konfor,
    o.fiyat_performans_puani = agg.avg_fp
FROM oteller AS o
INNER JOIN agg ON agg.otel_id = o.id;
