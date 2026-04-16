DECLARE @admin_user_id BIGINT = (SELECT TOP (1) id FROM users WHERE eposta = 'root@otelturizm.com');

INSERT INTO firmalar
(
    firma_kodu, firma_adi, firma_turu, vergi_no, vergi_dairesi, ticaret_sicil_no, mersis_no,
    firma_eposta, firma_telefon, web_sitesi, sektor, calisan_sayisi, aylik_seyahat_butcesi,
    varsayilan_para_birimi, acik_adres, sehir, ilce, posta_kodu,
    yetkili_ad_soyad, yetkili_unvani, yetkili_eposta, yetkili_telefon,
    onay_durumu, basvuru_tarihi, onay_sureci_baslama_tarihi, onay_tarihi, reddedilme_tarihi,
    onaylayan_kullanici_id, onay_notu, aktif_mi, giris_izni_aktif_mi, planlanan_onay_suresi_saat,
    kayit_kaynagi, sozlesme_onay_tarihi, kvkk_onay_tarihi, notlar
)
SELECT
    'OTLTRZM-FRM-0901', 'Marmara Satinalma Cozumleri A.S.', 'Anonim Şirketi', '9000000001', 'Kadikoy', '2026-MSC-001', '9000000000000001',
    'kurumsal@marmarasatinalma.com', '02169000001', 'https://marmarasatinalma.com', 'Tedarik', 185, 420000.00,
    'TRY', 'Kosuyolu Mah. Bagdat Cad. No:18 Kadikoy/Istanbul', 'Istanbul', 'Kadikoy', '34718',
    'Burcu Aksoy', 'Satinalma Direktoru', 'onayli.firma.test@otelturizm.com', '05559000001',
    'Onaylandı', DATEADD(DAY, -3, SYSDATETIME()), DATEADD(DAY, -2, SYSDATETIME()), DATEADD(DAY, -1, SYSDATETIME()), NULL,
    @admin_user_id, 'Kurumsal hesap onaylandi ve giris yetkisi acildi.', 1, 1, 12,
    'seed_firma_application', DATEADD(DAY, -3, SYSDATETIME()), DATEADD(DAY, -3, SYSDATETIME()), 'Onayli test firmasi'
WHERE NOT EXISTS (SELECT 1 FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0901');

INSERT INTO firmalar
(
    firma_kodu, firma_adi, firma_turu, vergi_no, vergi_dairesi, ticaret_sicil_no, mersis_no,
    firma_eposta, firma_telefon, web_sitesi, sektor, calisan_sayisi, aylik_seyahat_butcesi,
    varsayilan_para_birimi, acik_adres, sehir, ilce, posta_kodu,
    yetkili_ad_soyad, yetkili_unvani, yetkili_eposta, yetkili_telefon,
    onay_durumu, basvuru_tarihi, onay_sureci_baslama_tarihi, onay_tarihi, reddedilme_tarihi,
    onaylayan_kullanici_id, onay_notu, aktif_mi, giris_izni_aktif_mi, planlanan_onay_suresi_saat,
    kayit_kaynagi, sozlesme_onay_tarihi, kvkk_onay_tarihi, notlar
)
SELECT
    'OTLTRZM-FRM-0902', 'Anadolu Proje ve Lojistik Ltd.', 'Limited Şirketi', '9000000002', 'Umraniye', '2026-APL-002', '9000000000000002',
    'kurumsal@anadoluproje.com', '02169000002', 'https://anadoluproje.com', 'Lojistik', 74, 165000.00,
    'TRY', 'Ataturk Mah. Alemdagi Cad. No:42 Umraniye/Istanbul', 'Istanbul', 'Umraniye', '34764',
    'Deniz Ucar', 'Operasyon Muduru', 'bekleyen.firma.test@otelturizm.com', '05559000002',
    'Beklemede', DATEADD(HOUR, -8, SYSDATETIME()), DATEADD(HOUR, -6, SYSDATETIME()), NULL, NULL,
    NULL, 'Belgeler inceleniyor.', 1, 0, 24,
    'seed_firma_application', DATEADD(HOUR, -8, SYSDATETIME()), DATEADD(HOUR, -8, SYSDATETIME()), 'Bekleyen test firmasi'
WHERE NOT EXISTS (SELECT 1 FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0902');

INSERT INTO firmalar
(
    firma_kodu, firma_adi, firma_turu, vergi_no, vergi_dairesi, ticaret_sicil_no, mersis_no,
    firma_eposta, firma_telefon, web_sitesi, sektor, calisan_sayisi, aylik_seyahat_butcesi,
    varsayilan_para_birimi, acik_adres, sehir, ilce, posta_kodu,
    yetkili_ad_soyad, yetkili_unvani, yetkili_eposta, yetkili_telefon,
    onay_durumu, basvuru_tarihi, onay_sureci_baslama_tarihi, onay_tarihi, reddedilme_tarihi,
    onaylayan_kullanici_id, onay_notu, aktif_mi, giris_izni_aktif_mi, planlanan_onay_suresi_saat,
    kayit_kaynagi, sozlesme_onay_tarihi, kvkk_onay_tarihi, notlar
)
SELECT
    'OTLTRZM-FRM-0903', 'Kuzey Organizasyon ve Etkinlik Hizmetleri', 'Danışmanlık Şirketi', '9000000003', 'Besiktas', '2026-KOE-003', '9000000000000003',
    'info@kuzeyorganizasyon.com', '02169000003', 'https://kuzeyorganizasyon.com', 'Etkinlik', 26, 88000.00,
    'TRY', 'Levent Mah. Buyukdere Cad. No:55 Besiktas/Istanbul', 'Istanbul', 'Besiktas', '34330',
    'Selin Tunca', 'Kurucu Ortak', 'reddedilen.firma.test@otelturizm.com', '05559000003',
    'Reddedildi', DATEADD(DAY, -5, SYSDATETIME()), DATEADD(DAY, -4, SYSDATETIME()), NULL, DATEADD(DAY, -3, SYSDATETIME()),
    @admin_user_id, 'Vergi belge dogrulamasi tamamlanamadi.', 1, 0, 24,
    'seed_firma_application', DATEADD(DAY, -5, SYSDATETIME()), DATEADD(DAY, -5, SYSDATETIME()), 'Reddedilen test firmasi'
WHERE NOT EXISTS (SELECT 1 FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0903');

INSERT INTO firmalar
(
    firma_kodu, firma_adi, firma_turu, vergi_no, vergi_dairesi, ticaret_sicil_no, mersis_no,
    firma_eposta, firma_telefon, web_sitesi, sektor, calisan_sayisi, aylik_seyahat_butcesi,
    varsayilan_para_birimi, acik_adres, sehir, ilce, posta_kodu,
    yetkili_ad_soyad, yetkili_unvani, yetkili_eposta, yetkili_telefon,
    onay_durumu, basvuru_tarihi, onay_sureci_baslama_tarihi, onay_tarihi, reddedilme_tarihi,
    onaylayan_kullanici_id, onay_notu, aktif_mi, giris_izni_aktif_mi, planlanan_onay_suresi_saat,
    kayit_kaynagi, sozlesme_onay_tarihi, kvkk_onay_tarihi, notlar
)
SELECT
    'OTLTRZM-FRM-0904', 'Delta Kurumsal Seyahat Cozumleri', 'Turizm Şirketi', '9000000004', 'Sisli', '2026-DKS-004', '9000000000000004',
    'iletisim@deltakurumsal.com', '02169000004', 'https://deltakurumsal.com', 'Turizm', 310, 910000.00,
    'TRY', 'Mecidiyekoy Mah. Buyukdere Cad. No:77 Sisli/Istanbul', 'Istanbul', 'Sisli', '34387',
    'Mert Eren', 'Kurumsal Kanal Direktoru', 'askida.firma.test@otelturizm.com', '05559000004',
    'Askıda', DATEADD(DAY, -12, SYSDATETIME()), DATEADD(DAY, -10, SYSDATETIME()), DATEADD(DAY, -9, SYSDATETIME()), NULL,
    @admin_user_id, 'Ek sozlesme evraki bekleniyor.', 1, 0, 48,
    'seed_firma_application', DATEADD(DAY, -12, SYSDATETIME()), DATEADD(DAY, -12, SYSDATETIME()), 'Askida test firmasi'
WHERE NOT EXISTS (SELECT 1 FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0904');

DECLARE @firma_onayli BIGINT = (SELECT TOP (1) id FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0901');
DECLARE @firma_bekleyen BIGINT = (SELECT TOP (1) id FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0902');
DECLARE @firma_reddedilen BIGINT = (SELECT TOP (1) id FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0903');
DECLARE @firma_askida BIGINT = (SELECT TOP (1) id FROM firmalar WHERE firma_kodu = 'OTLTRZM-FRM-0904');

INSERT INTO users
(ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani, harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi)
SELECT 'Burcu Aksoy', 'onayli.firma.test@otelturizm.com', '05559000001', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '1585'), 2), 'firma_admin', @firma_onayli, 'Satinalma', 'Satinalma Direktoru', 12500.00, 0, 'FRM-901', 1, 1, 'tr', 'TRY', 'Türkiye', SYSDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'onayli.firma.test@otelturizm.com');

INSERT INTO users
(ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani, harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi)
SELECT 'Deniz Ucar', 'bekleyen.firma.test@otelturizm.com', '05559000002', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '1585'), 2), 'firma_admin', @firma_bekleyen, 'Operasyon', 'Operasyon Muduru', 9800.00, 1, 'FRM-902', 1, 1, 'tr', 'TRY', 'Türkiye', SYSDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'bekleyen.firma.test@otelturizm.com');

INSERT INTO users
(ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani, harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi)
SELECT 'Selin Tunca', 'reddedilen.firma.test@otelturizm.com', '05559000003', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '1585'), 2), 'firma_admin', @firma_reddedilen, 'Yonetim', 'Kurucu Ortak', 7800.00, 1, 'FRM-903', 1, 1, 'tr', 'TRY', 'Türkiye', SYSDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'reddedilen.firma.test@otelturizm.com');

INSERT INTO users
(ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani, harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi)
SELECT 'Mert Eren', 'askida.firma.test@otelturizm.com', '05559000004', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '1585'), 2), 'firma_admin', @firma_askida, 'Kurumsal Satis', 'Kurumsal Kanal Direktoru', 15000.00, 1, 'FRM-904', 1, 1, 'tr', 'TRY', 'Türkiye', SYSDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'askida.firma.test@otelturizm.com');

UPDATE users SET firma_id = @firma_onayli, rol = 'firma_admin', departman = 'Satinalma', gorev_unvani = 'Satinalma Direktoru', firma_yonetici_mi = 1 WHERE eposta = 'onayli.firma.test@otelturizm.com';
UPDATE users SET firma_id = @firma_bekleyen, rol = 'firma_admin', departman = 'Operasyon', gorev_unvani = 'Operasyon Muduru', firma_yonetici_mi = 1 WHERE eposta = 'bekleyen.firma.test@otelturizm.com';
UPDATE users SET firma_id = @firma_reddedilen, rol = 'firma_admin', departman = 'Yonetim', gorev_unvani = 'Kurucu Ortak', firma_yonetici_mi = 1 WHERE eposta = 'reddedilen.firma.test@otelturizm.com';
UPDATE users SET firma_id = @firma_askida, rol = 'firma_admin', departman = 'Kurumsal Satis', gorev_unvani = 'Kurumsal Kanal Direktoru', firma_yonetici_mi = 1 WHERE eposta = 'askida.firma.test@otelturizm.com';

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_onayli, NULL, 'Beklemede', 'Basvuru Alindi', 'Test firma basvurusu sisteme alindi.', NULL, 'seed_firma_application'
WHERE @firma_onayli IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_onayli AND hareket_tipi = 'Basvuru Alindi');

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_onayli, 'Beklemede', 'Onaylandı', 'Onaylandi', 'Belgeler dogrulandi, firma hesabi aktif edildi.', @admin_user_id, 'seed_firma_application'
WHERE @firma_onayli IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_onayli AND hareket_tipi = 'Onaylandi');

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_bekleyen, NULL, 'Beklemede', 'Basvuru Alindi', 'Evrak kontrolu bekleniyor.', NULL, 'seed_firma_application'
WHERE @firma_bekleyen IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_bekleyen AND hareket_tipi = 'Basvuru Alindi');

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_bekleyen, 'Beklemede', 'Beklemede', 'Incelemeye Alindi', 'Finans ve vergi evraklari inceleniyor.', @admin_user_id, 'seed_firma_application'
WHERE @firma_bekleyen IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_bekleyen AND hareket_tipi = 'Incelemeye Alindi');

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_reddedilen, NULL, 'Beklemede', 'Basvuru Alindi', 'Basvuru olusturuldu.', NULL, 'seed_firma_application'
WHERE @firma_reddedilen IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_reddedilen AND hareket_tipi = 'Basvuru Alindi');

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_reddedilen, 'Beklemede', 'Reddedildi', 'Reddedildi', 'Vergi belge dogrulamasi basarisiz.', @admin_user_id, 'seed_firma_application'
WHERE @firma_reddedilen IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_reddedilen AND hareket_tipi = 'Reddedildi');

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_askida, NULL, 'Beklemede', 'Basvuru Alindi', 'Basvuru olusturuldu.', NULL, 'seed_firma_application'
WHERE @firma_askida IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_askida AND hareket_tipi = 'Basvuru Alindi');

INSERT INTO firma_basvuru_hareketleri (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi)
SELECT @firma_askida, 'Onaylandı', 'Askıda', 'Askida', 'Ek sozlesme evragi bekleniyor.', @admin_user_id, 'seed_firma_application'
WHERE @firma_askida IS NOT NULL AND NOT EXISTS (SELECT 1 FROM firma_basvuru_hareketleri WHERE firma_id = @firma_askida AND hareket_tipi = 'Askida');
