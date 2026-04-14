INSERT INTO oteller (
    id,
    otel_kodu,
    partner_id,
    otel_adi,
    otel_turu,
    yildiz_sayisi,
    ulke,
    sehir,
    ilce,
    mahalle,
    tam_adres,
    enlem,
    boylam,
    sehir_id,
    ilce_id,
    telefon_1,
    eposta,
    web_sitesi,
    check_in_saati,
    check_out_saati,
    minimum_konaklama_gecesi,
    maksimum_konaklama_gecesi,
    toplam_oda_sayisi,
    kisa_aciklama,
    konum_aciklamasi,
    varsayilan_komisyon_orani,
    yayin_durumu,
    onay_durumu,
    kapak_fotografi,
    one_cikan_otel,
    olusturulma_tarihi,
    guncellenme_tarihi
)
SELECT
    x.id,
    CONCAT('OTLTRZM_', LPAD(x.id, 6, '0')) AS otel_kodu,
    1 AS partner_id,
    x.otel_adi,
    CASE
        WHEN x.otel_tipi_id = 7 THEN 'Villa'
        WHEN x.otel_tipi_id = 1 THEN 'Otel'
        WHEN x.otel_tipi_id = 3 THEN 'Apart Otel'
        ELSE 'Otel'
    END AS otel_turu,
    x.yildiz_sayisi,
    'Turkiye' AS ulke,
    IF(x.il_id = 34, 'Istanbul', 'Kocaeli') AS sehir,
    ic.ilce_adi AS ilce,
    x.mahalle_adi AS mahalle,
    x.otel_adresi AS tam_adres,
    x.latitude AS enlem,
    x.longitude AS boylam,
    i.id AS sehir_id,
    ic.id AS ilce_id,
    IFNULL(NULLIF(x.otel_numarasi, ''), '02160000000') AS telefon_1,
    CONCAT('legacy+', x.id, '@otelturizm.com') AS eposta,
    IFNULL(NULLIF(x.web_sitesi, ''), 'https://otelturizm.com') AS web_sitesi,
    x.checkin_saati AS check_in_saati,
    x.checkout_saati AS check_out_saati,
    x.min_konaklama AS minimum_konaklama_gecesi,
    x.max_konaklama AS maksimum_konaklama_gecesi,
    20 AS toplam_oda_sayisi,
    LEFT(x.otel_aciklama, 500) AS kisa_aciklama,
    x.konum_aciklamasi,
    15.00 AS varsayilan_komisyon_orani,
    CASE WHEN x.durum = 1 AND x.is_closed = 0 THEN 'Yayında' ELSE 'Taslak' END AS yayin_durumu,
    CASE WHEN x.durum = 1 AND x.is_closed = 0 THEN 'Onaylandı' ELSE 'Beklemede' END AS onay_durumu,
    x.ana_gorsel AS kapak_fotografi,
    x.is_featured AS one_cikan_otel,
    x.created_at AS olusturulma_tarihi,
    x.updated_at AS guncellenme_tarihi
FROM (
    SELECT 2 AS id, 34 AS il_id, 25 AS ilce_id, 3 AS otel_tipi_id, 'EAGLE PALACE SUITE' AS otel_adi, 'EAGLE PALACE SUITE, Kartal bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.' AS otel_aciklama, 'Esentepe' AS mahalle_adi, 'Esentepe, Kocatepe Cd. No:16, 34870 Kartal/Istanbul' AS otel_adresi, '216-009' AS otel_numarasi, 'https://otelturizm.com' AS web_sitesi, NULL AS latitude, NULL AS longitude, 'Tesis Kartal bolgesinde konumlanmaktadir.' AS konum_aciklamasi, 1 AS durum, 0 AS is_closed, '14:00:00' AS checkin_saati, '12:00:00' AS checkout_saati, 1 AS min_konaklama, 30 AS max_konaklama, NULL AS yildiz_sayisi, 'uploads/odalar/otel-20260404125631-rxgstc.webp' AS ana_gorsel, 1 AS is_featured, '2026-04-06 05:08:25' AS updated_at, '2026-04-04 12:44:40' AS created_at
    UNION ALL SELECT 3,34,3,3,'COMFORT INN SUITE','COMFORT INN SUITE, Atasehir bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Yeni Sahra','Yeni Sahra, Dere Sk. No:3, 34746 Atasehir/Istanbul','216-014','https://otelturizm.com',NULL,NULL,'Tesis Atasehir bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404124956-ztjo6o.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 4,34,27,3,'216 PALACE SUITE','216 PALACE SUITE, Maltepe bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Zumrutevler','Zumrutevler, Basogretmen Cd. No:81, 34852 Maltepe/Istanbul','216-011','https://otelturizm.com',NULL,NULL,'Tesis Maltepe bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404125201-4yj6hc.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 6,34,28,3,'HILL SUITE','HILL SUITE, Kurtkoy bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Seyhli','Seyhli, Aksemseddin Caddesi No:4, 34906 Pendik/Istanbul','216-007','https://otelturizm.com',NULL,NULL,'Tesis resmi olarak Pendik bolgesine baglidir; operasyonel lokasyon etiketi Kurtkoy olarak kullanilmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404125544-pox7il.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 7,34,3,3,'PASHA PALACE SUITE','PASHA PALACE SUITE, Atasehir bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Ferhatpasa','Ferhatpasa Mah, Ferhatpasa Yolu Sk. No:55, 34888 Atasehir/Istanbul','216-013','https://otelturizm.com',NULL,NULL,'Tesis Atasehir bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404125043-mhsg4o.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 9,34,23,3,'F&B SUITE','F&B SUITE, Kadikoy bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Osmanaga','Osmanaga, Kusdili Cd. No:71, 34734 Kadikoy/Istanbul','216-016','https://otelturizm.com',NULL,NULL,'Tesis Kadikoy bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404124843-vazmir.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 11,34,28,3,'BLUE LIFE SUITE','BLUE LIFE SUITE, Pendik bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Bahcelievler','Bahcelievler, Seyhan Sk. No:19, 34893 Pendik/Istanbul','216-006','https://otelturizm.com',NULL,NULL,'Tesis Pendik bolgesinde konumlanmaktadir.',0,0,'14:00:00','12:00:00',1,30,NULL,NULL,1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 14,34,27,1,'MACITY HOTEL','MACITY HOTEL, Maltepe bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Zumrutevler','Zumrutevler Mah. Ataturk Cd. Camlikli Sk. No:32A, 34852 Maltepe/Istanbul','216-010','https://otelturizm.com',NULL,NULL,'Tesis Maltepe bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404125609-sigl8d.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 15,34,28,7,'MY VILLA','MY VILLA, Kaynarca bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Kaynarca','Kaynarca, Erol Kaya Cd No:261, 34890 Pendik/Istanbul','216-005','https://otelturizm.com',NULL,NULL,'Tesis resmi olarak Pendik bolgesine baglidir; operasyonel lokasyon etiketi Kaynarca olarak kullanilmaktadir.',0,0,'14:00:00','12:00:00',1,30,NULL,NULL,1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 17,34,23,3,'BOSTANCI SUITE','BOSTANCI SUITE, Bostanci bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Bostanci','Kasaplar Carsisi, eski bagdat caddesi no:13 Kadikoy/Bostanci','216-015','https://otelturizm.com',NULL,NULL,'Tesis resmi olarak Kadikoy bolgesine baglidir; operasyonel lokasyon etiketi Bostanci olarak kullanilmaktadir.',0,0,'14:00:00','12:00:00',1,30,NULL,NULL,1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 18,34,37,3,'216 TREND SUITE','216 TREND SUITE, Umraniye bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Mehmet Akif','Mehmet Akif Mahallesi Tavukcuyolu Caddesi, Dilek Sk. No:2, 34774 Umraniye/Istanbul','216-019','https://otelturizm.com',NULL,NULL,'Tesis Umraniye bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,3,'uploads/odalar/otel-20260404124106-smpx56.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 20,34,38,3,'216 BOSPHORUS SUITE','216 BOSPHORUS SUITE, Uskudar bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Pasalimani','Pasalimani Caddesi No 22 Uskudar, Istanbul Turkiye','216-018','https://otelturizm.com',NULL,NULL,'Tesis Uskudar bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404124618-9w0mra.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 21,41,40,3,'216 STATION SUITE','216 STATION SUITE, Gebze bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Istasyon','Istasyon Mahallesi 1456 Sokak No:64 Gebze, Darica','216-002','https://otelturizm.com',NULL,NULL,'Tesis Gebze bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404130253-634jfm.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 22,34,25,3,'216 CENTER','216 CENTER icin temel konaklama kaydi otomatik olarak olusturuldu.','Karliktepe','Karliktepe, Cetinkaya Sk. 5-34, 34870 Kartal/Istanbul','216-022','https://otelturizm.com',NULL,NULL,'Karliktepe, Cetinkaya Sk. 5-34, 34870 Kartal/Istanbul',0,0,'14:00:00','12:00:00',1,30,NULL,NULL,1,'2026-04-06 08:25:35','2026-04-06 08:08:25'
    UNION ALL SELECT 23,34,27,3,'216 RUBY SUITE','216 RUBY SUITE, Maltepe bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Zumrutevler','Zumrutevler, Okul Sk. No:3, 34852 Maltepe/Istanbul','216-012','https://otelturizm.com',NULL,NULL,'Tesis Maltepe bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404125127-vys0l1.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 24,34,23,3,'216 STYLE SUITE','216 STYLE SUITE, Kadikoy bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Osmanaga','Osmanaga, Gezimne Sk. No:14/1, 34714 Kadikoy/Istanbul','216-017','https://otelturizm.com',NULL,NULL,'Tesis Kadikoy bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404124743-mc5162.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 25,34,37,1,'216 PRESTIGE HOTEL','216 PRESTIGE HOTEL, Umraniye bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Inkilap','Inkilap, Kucuksu Cd. 97B, 34768 Umraniye/Istanbul','4440216','https://otelturizm.com',NULL,NULL,'Tesis Umraniye bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,3,'uploads/odalar/otel-20260404102351-ns0nxm.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 26,34,29,3,'216 STAR SUITE','216 STAR SUITE, Sancaktepe bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Osmangazi','Osmangazi, Hilal Cd. 24/A, 34887 Sancaktepe/Istanbul','216-008','https://otelturizm.com',NULL,NULL,'Tesis Sancaktepe bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,NULL,'uploads/odalar/otel-20260404125658-hsguyq.webp',1,'2026-04-06 05:08:25','2026-04-04 12:44:40'
    UNION ALL SELECT 27,34,34,3,'216 NORTH SUITE','216 NORTH SUITE, Agva bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Kurfalli','Kurfalli, Erseven Sokak No:86, 34983 Sile/Istanbul','216-001','https://otelturizm.com',NULL,NULL,'Tesis resmi olarak Sile bolgesine baglidir; operasyonel lokasyon etiketi Agva olarak kullanilmaktadir.',1,0,'14:00:00','12:00:00',1,30,3,'uploads/odalar/otel-20260404130341-lhabdd.webp',1,'2026-04-06 10:24:08','2026-04-04 12:44:40'
    UNION ALL SELECT 28,34,3,3,'216 NEAR','216 NEAR icin temel konaklama kaydi otomatik olarak olusturuldu.','Yeni Sahra','Yeni Sahra, Inonu Caddesi No:5, Atasehir/Istanbul','216-028','https://otelturizm.com',NULL,NULL,'Yeni Sahra, Inonu Caddesi No:5, Atasehir/Istanbul',0,0,'14:00:00','12:00:00',1,30,NULL,NULL,1,'2026-04-06 08:25:33','2026-04-06 08:08:25'
    UNION ALL SELECT 29,34,36,3,'216 SILVER SUITE','216 SILVER SUITE, Tuzla bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Postane','Postane, Muhacir Sk. No:5, 34940 Tuzla/Istanbul','4440216','https://otelturizm.com',NULL,NULL,'Tesis Tuzla bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,0,'uploads/odalar/otel-20260404130150-6hiywv.webp',1,'2026-04-06 05:45:07','2026-04-04 12:44:40'
    UNION ALL SELECT 30,41,40,1,'216 CASTLE SUITE','216 CASTLE SUITE, Gebze bolgesinde 216 Suites grup portfoyu icinde konumlanan konfor odakli konaklama secenegidir.','Eskihisar','Eskihisar Mah. Zeki Acar Cad No:13/A, 41400 Gebze','4440216','https://otelturizm.com',NULL,NULL,'Tesis Gebze bolgesinde konumlanmaktadir.',1,0,'14:00:00','12:00:00',1,30,0,'uploads/odalar/otel-20260404130213-tqiwtc.webp',1,'2026-04-06 05:27:37','2026-04-04 12:44:40'
) x
JOIN iller i ON i.plaka_kodu = x.il_id
JOIN ilceler ic ON ic.il_id = i.id AND ic.dis_kod = x.ilce_id;
