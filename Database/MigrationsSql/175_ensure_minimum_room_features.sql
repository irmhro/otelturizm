SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.oda_tipleri', N'U') IS NULL
    OR OBJECT_ID(N'dbo.oda_ozellikleri', N'U') IS NULL
    OR OBJECT_ID(N'dbo.oda_tipi_ozellikleri', N'U') IS NULL
BEGIN
    RETURN;
END;

;WITH base_features AS
(
    SELECT id, ozellik_adi
    FROM dbo.oda_ozellikleri
    WHERE aktif_mi = 1
      AND ozellik_adi IN
      (
          N'Klima',
          N'Saç Kurutma Makinesi',
          N'Düz Ekran TV',
          N'Elektrikli Su Isıtıcısı',
          N'Minibar',
          N'Çalışma Masası',
          N'Oturma Alanı',
          N'Gardırop / Dolap',
          N'Balkon',
          N'Kablosuz İnternet',
          N'Özel Banyo'
      )
),
room_candidates AS
(
    SELECT
        ot.id AS oda_tip_id,
        bf.id AS ozellik_id,
        ROW_NUMBER() OVER
        (
            PARTITION BY ot.id
            ORDER BY
                CASE
                    WHEN ot.oda_adi LIKE N'%Aile%' AND bf.ozellik_adi IN (N'Oturma Alanı', N'Kablosuz İnternet', N'Özel Banyo', N'Minibar', N'Düz Ekran TV') THEN 0
                    WHEN ot.oda_adi LIKE N'%Deluxe%' AND bf.ozellik_adi IN (N'Çalışma Masası', N'Balkon', N'Minibar', N'Düz Ekran TV', N'Özel Banyo') THEN 0
                    WHEN ot.oda_adi LIKE N'%Standart%' AND bf.ozellik_adi IN (N'Klima', N'Saç Kurutma Makinesi', N'Düz Ekran TV', N'Elektrikli Su Isıtıcısı', N'Minibar') THEN 0
                    ELSE 1
                END,
                bf.id
        ) AS tercih_sirasi
    FROM dbo.oda_tipleri ot
    CROSS JOIN base_features bf
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.oda_tipi_ozellikleri oto
        WHERE oto.oda_tip_id = ot.id
          AND oto.ozellik_id = bf.id
    )
),
room_counts AS
(
    SELECT
        ot.id AS oda_tip_id,
        COUNT(oto.ozellik_id) AS ozellik_sayisi
    FROM dbo.oda_tipleri ot
    LEFT JOIN dbo.oda_tipi_ozellikleri oto ON oto.oda_tip_id = ot.id
    GROUP BY ot.id
),
features_to_insert AS
(
    SELECT
        rc.oda_tip_id,
        rc.ozellik_id,
        ROW_NUMBER() OVER (PARTITION BY rc.oda_tip_id ORDER BY rc.tercih_sirasi, rc.ozellik_id) AS eklenecek_sira,
        ISNULL(rct.ozellik_sayisi, 0) AS mevcut_sayi
    FROM room_candidates rc
    LEFT JOIN room_counts rct ON rct.oda_tip_id = rc.oda_tip_id
)
INSERT INTO dbo.oda_tipi_ozellikleri (oda_tip_id, ozellik_id, miktar)
SELECT
    fi.oda_tip_id,
    fi.ozellik_id,
    1
FROM features_to_insert fi
WHERE fi.eklenecek_sira <= (5 - fi.mevcut_sayi)
  AND fi.mevcut_sayi < 5;
