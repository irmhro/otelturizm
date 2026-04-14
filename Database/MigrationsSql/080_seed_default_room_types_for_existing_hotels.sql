INSERT INTO oda_tipleri (
    otel_id,
    oda_tip_kodu,
    oda_adi,
    oda_kategorisi,
    maksimum_kisi_sayisi,
    maksimum_yetiskin_sayisi,
    maksimum_cocuk_sayisi,
    yatak_tipi,
    yatak_sayisi,
    oda_metrekare,
    manzara_tipi,
    standart_gecelik_fiyat,
    toplam_oda_sayisi,
    aktif_mi,
    siralama,
    olusturulma_tarihi
)
SELECT
    o.id,
    CONCAT('STD-', LPAD(o.id, 3, '0')),
    'Standart Oda',
    'Standart',
    2,
    2,
    1,
    'Çift Kişilik',
    1,
    CASE
        WHEN o.otel_turu = 'Villa' THEN 60
        WHEN o.otel_turu = 'Tatil Köyü' THEN 42
        WHEN o.otel_turu = 'Apart Otel' THEN 34
        ELSE 28
    END,
    CASE
        WHEN o.ilce IN ('Uskudar', 'Besiktas', 'Kadikoy', 'Kartal') THEN 'Şehir'
        ELSE 'Bahçe'
    END,
    CASE
        WHEN o.yildiz_sayisi >= 5 THEN 6500.00
        WHEN o.yildiz_sayisi = 4 THEN 5200.00
        WHEN o.yildiz_sayisi = 3 THEN 4200.00
        WHEN o.otel_turu = 'Villa' THEN 7000.00
        WHEN o.otel_turu = 'Apart Otel' THEN 3100.00
        ELSE 3600.00
    END,
    GREATEST(IFNULL(o.toplam_oda_sayisi, 20), 8),
    1,
    1,
    NOW()
FROM oteller o
LEFT JOIN oda_tipleri ot
    ON ot.otel_id = o.id
WHERE ot.id IS NULL;
