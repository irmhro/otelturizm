INSERT INTO mahalleler (il_id, ilce_id, mahalle_adi, seo_slug, posta_kodu, enlem, boylam)
SELECT 34, ic.id, 'Esentepe', 'esentepe-kartal', '34870', 40.90660000, 29.17670000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 25
UNION ALL
SELECT 34, ic.id, 'Yeni Sahra', 'yeni-sahra-atasehir', '34746', 40.98910000, 29.07750000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 3
UNION ALL
SELECT 34, ic.id, 'Zumrutevler', 'zumrutevler-maltepe', '34852', 40.93560000, 29.15120000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 27
UNION ALL
SELECT 34, ic.id, 'Seyhli', 'seyhli-pendik', '34906', 40.90920000, 29.27550000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 28
UNION ALL
SELECT 34, ic.id, 'Ferhatpasa', 'ferhatpasa-atasehir', '34888', 40.97940000, 29.18490000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 3
UNION ALL
SELECT 34, ic.id, 'Osmanaga', 'osmanaga-kadikoy', '34714', 40.99050000, 29.03080000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 23
UNION ALL
SELECT 34, ic.id, 'Bahcelievler', 'bahcelievler-pendik', '34893', 40.87980000, 29.25510000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 28
UNION ALL
SELECT 34, ic.id, 'Bostanci', 'bostanci-kadikoy', '34744', 40.95330000, 29.09560000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 23
UNION ALL
SELECT 34, ic.id, 'Mehmet Akif', 'mehmet-akif-umraniye', '34774', 41.02270000, 29.13140000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 37
UNION ALL
SELECT 34, ic.id, 'Pasalimani', 'pasalimani-uskudar', '34676', 41.02860000, 29.02470000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 38
UNION ALL
SELECT 34, ic.id, 'Karliktepe', 'karliktepe-kartal', '34870', 40.90150000, 29.17720000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 25
UNION ALL
SELECT 34, ic.id, 'Inkilap', 'inkilap-umraniye', '34768', 41.01760000, 29.09140000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 37
UNION ALL
SELECT 34, ic.id, 'Osmangazi', 'osmangazi-sancaktepe', '34887', 40.98610000, 29.23230000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 29
UNION ALL
SELECT 34, ic.id, 'Kurfalli', 'kurfalli-sile', '34983', 41.16520000, 29.59420000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 34
UNION ALL
SELECT 34, ic.id, 'Postane', 'postane-tuzla', '34940', 40.82070000, 29.30480000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 36
UNION ALL
SELECT 34, ic.id, 'Kaynarca', 'kaynarca-pendik', '34890', 40.87250000, 29.26040000 FROM ilceler ic WHERE ic.il_id = 34 AND ic.dis_kod = 28
UNION ALL
SELECT 41, ic.id, 'Istasyon', 'istasyon-gebze', '41400', 40.79300000, 29.43040000 FROM ilceler ic WHERE ic.il_id = 41 AND ic.dis_kod = 40
UNION ALL
SELECT 41, ic.id, 'Eskihisar', 'eskihisar-gebze', '41400', 40.78170000, 29.43140000 FROM ilceler ic WHERE ic.il_id = 41 AND ic.dis_kod = 40;
