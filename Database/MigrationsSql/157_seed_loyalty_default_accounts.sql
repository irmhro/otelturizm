INSERT INTO kullanici_sadakat_hesaplari
(kullanici_id, toplam_puan, kullanilabilir_puan, bu_yil_kazanilan_puan, bu_yil_kullanilan_puan, mevcut_seviye_id, sonraki_seviye_id, puan_gecerlilik_tarihi, olusturulma_tarihi, guncellenme_tarihi)
SELECT u.id,
       0,
       0,
       0,
       0,
       (SELECT id FROM sadakat_seviyeleri WHERE kod = 'BRONZE' LIMIT 1),
       (SELECT id FROM sadakat_seviyeleri WHERE kod = 'SILVER' LIMIT 1),
       DATE_ADD(CURDATE(), INTERVAL 365 DAY),
       CURRENT_TIMESTAMP,
       CURRENT_TIMESTAMP
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
       CURRENT_TIMESTAMP,
       DATE_ADD(CURRENT_TIMESTAMP, INTERVAL 365 DAY),
       CURRENT_TIMESTAMP
FROM users u
INNER JOIN kullanici_sadakat_hesaplari h ON h.kullanici_id = u.id
LEFT JOIN kullanici_puan_hareketleri ph ON ph.kullanici_id = u.id
WHERE ph.id IS NULL;

UPDATE kullanici_sadakat_hesaplari h
JOIN (
    SELECT kullanici_id,
           COALESCE(SUM(CASE WHEN puan_degisim > 0 THEN puan_degisim ELSE 0 END), 0) AS toplam_kazanc,
           COALESCE(ABS(SUM(CASE WHEN puan_degisim < 0 THEN puan_degisim ELSE 0 END)), 0) AS toplam_kullanim
    FROM kullanici_puan_hareketleri
    GROUP BY kullanici_id
) x ON x.kullanici_id = h.kullanici_id
LEFT JOIN sadakat_seviyeleri current_tier
    ON current_tier.id = (
        SELECT s.id
        FROM sadakat_seviyeleri s
        WHERE x.toplam_kazanc - x.toplam_kullanim >= s.minimum_puan
          AND (s.maximum_puan IS NULL OR x.toplam_kazanc - x.toplam_kullanim <= s.maximum_puan)
        ORDER BY s.minimum_puan DESC
        LIMIT 1
    )
LEFT JOIN sadakat_seviyeleri next_tier
    ON next_tier.minimum_puan = (
        SELECT MIN(s2.minimum_puan)
        FROM sadakat_seviyeleri s2
        WHERE s2.minimum_puan > x.toplam_kazanc - x.toplam_kullanim
    )
SET h.toplam_puan = x.toplam_kazanc,
    h.kullanilabilir_puan = GREATEST(x.toplam_kazanc - x.toplam_kullanim, 0),
    h.bu_yil_kazanilan_puan = x.toplam_kazanc,
    h.bu_yil_kullanilan_puan = x.toplam_kullanim,
    h.mevcut_seviye_id = COALESCE(current_tier.id, h.mevcut_seviye_id),
    h.sonraki_seviye_id = next_tier.id,
    h.guncellenme_tarihi = CURRENT_TIMESTAMP;
