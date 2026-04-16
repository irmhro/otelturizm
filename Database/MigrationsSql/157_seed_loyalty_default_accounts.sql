INSERT INTO kullanici_sadakat_hesaplari
(kullanici_id, toplam_puan, kullanilabilir_puan, bu_yil_kazanilan_puan, bu_yil_kullanilan_puan, mevcut_seviye_id, sonraki_seviye_id, puan_gecerlilik_tarihi, olusturulma_tarihi, guncellenme_tarihi)
SELECT u.id,
       0,
       0,
       0,
       0,
       (SELECT TOP (1) id FROM sadakat_seviyeleri WHERE kod = 'BRONZE'),
       (SELECT TOP (1) id FROM sadakat_seviyeleri WHERE kod = 'SILVER'),
       DATEADD(DAY, 365, CAST(GETDATE() AS DATE)),
       SYSDATETIME(),
       SYSDATETIME()
FROM users u
LEFT JOIN kullanici_sadakat_hesaplari h ON h.kullanici_id = u.id
WHERE h.id IS NULL;

INSERT INTO kullanici_puan_hareketleri
(kullanici_id, sadakat_hesap_id, hareket_tipi, baslik, aciklama, puan_degisim, puan_bakiye_sonrasi, durum, islem_tarihi, gecerlilik_tarihi, olusturulma_tarihi)
SELECT u.id,
       h.id,
       'Hosgeldin',
       'Hos geldin bonusu',
       'Sadakat merkezi acilis puani',
       100,
       100,
       'Tamamlandi',
       SYSDATETIME(),
       DATEADD(DAY, 365, SYSDATETIME()),
       SYSDATETIME()
FROM users u
INNER JOIN kullanici_sadakat_hesaplari h ON h.kullanici_id = u.id
LEFT JOIN kullanici_puan_hareketleri ph ON ph.kullanici_id = u.id
WHERE ph.id IS NULL;

;WITH x AS (
    SELECT kullanici_id,
           COALESCE(SUM(CASE WHEN puan_degisim > 0 THEN puan_degisim ELSE 0 END), 0) AS toplam_kazanc,
           COALESCE(ABS(SUM(CASE WHEN puan_degisim < 0 THEN puan_degisim ELSE 0 END)), 0) AS toplam_kullanim
    FROM kullanici_puan_hareketleri
    GROUP BY kullanici_id
),
current_tier AS (
    SELECT x.kullanici_id,
           s.id,
           ROW_NUMBER() OVER (PARTITION BY x.kullanici_id ORDER BY s.minimum_puan DESC) AS rn
    FROM x
    INNER JOIN sadakat_seviyeleri s
        ON x.toplam_kazanc - x.toplam_kullanim >= s.minimum_puan
       AND (s.maximum_puan IS NULL OR x.toplam_kazanc - x.toplam_kullanim <= s.maximum_puan)
),
next_tier AS (
    SELECT x.kullanici_id,
           s2.id,
           ROW_NUMBER() OVER (PARTITION BY x.kullanici_id ORDER BY s2.minimum_puan ASC) AS rn
    FROM x
    INNER JOIN sadakat_seviyeleri s2
        ON s2.minimum_puan > x.toplam_kazanc - x.toplam_kullanim
)
UPDATE h
SET h.toplam_puan = x.toplam_kazanc,
    h.kullanilabilir_puan = CASE WHEN x.toplam_kazanc - x.toplam_kullanim > 0 THEN x.toplam_kazanc - x.toplam_kullanim ELSE 0 END,
    h.bu_yil_kazanilan_puan = x.toplam_kazanc,
    h.bu_yil_kullanilan_puan = x.toplam_kullanim,
    h.mevcut_seviye_id = COALESCE(ct.id, h.mevcut_seviye_id),
    h.sonraki_seviye_id = nt.id,
    h.guncellenme_tarihi = SYSDATETIME()
FROM kullanici_sadakat_hesaplari h
INNER JOIN x ON x.kullanici_id = h.kullanici_id
LEFT JOIN current_tier ct ON ct.kullanici_id = h.kullanici_id AND ct.rn = 1
LEFT JOIN next_tier nt ON nt.kullanici_id = h.kullanici_id AND nt.rn = 1;
