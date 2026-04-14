INSERT INTO firmalar
(
  firma_kodu, firma_adi, firma_turu, vergi_no, vergi_dairesi, ticaret_sicil_no, firma_eposta, firma_telefon, web_sitesi,
  sektor, calisan_sayisi, aylik_seyahat_butcesi, varsayilan_para_birimi, acik_adres, sehir, ilce, posta_kodu,
  yetkili_ad_soyad, yetkili_eposta, yetkili_telefon, onay_durumu, aktif_mi, notlar
)
SELECT
  'OTLTRZM-FRM-0001', 'ABC Teknoloji A.Ş.', 'Anonim Şirketi', '1234567890', 'Maslak', '2026-ABCTECH-01', 'kurumsal@abcteknoloji.com', '02125550001', 'https://abcteknoloji.com',
  'Teknoloji', 420, 750000.00, 'TRY', 'Maslak Mah. Büyükdere Cad. No:123 Sarıyer/İstanbul', 'İstanbul', 'Sarıyer', '34485',
  'Ahmet Yılmaz', 'ahmet.yilmaz@abcteknoloji.com', '05320000001', 'Onaylandı', 1, 'Demo firma kaydı'
WHERE NOT EXISTS (SELECT 1 FROM firmalar WHERE vergi_no = '1234567890');

SET @firma_id = (SELECT id FROM firmalar WHERE vergi_no = '1234567890' LIMIT 1);

UPDATE users
SET rol = 'firma_admin',
    firma_id = @firma_id,
    departman = 'Seyahat Operasyonları',
    gorev_unvani = 'Firma Seyahat Yöneticisi',
    harcama_limiti = 15000.00,
    onay_gereksinimi = 0,
    personel_kodu = 'ABC-001',
    firma_yonetici_mi = 1
WHERE eposta = 'ahmet.yilmaz@abcteknoloji.com';

INSERT INTO users
(ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani, harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi)
SELECT 'Ahmet Yılmaz', 'ahmet.yilmaz@abcteknoloji.com', '05320000001', SHA2('1585',256), 'firma_admin', @firma_id, 'Seyahat Operasyonları', 'Firma Seyahat Yöneticisi', 15000.00, 0, 'ABC-001', 1, 1, 'tr', 'TRY', 'Türkiye', NOW()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'ahmet.yilmaz@abcteknoloji.com');

INSERT INTO users
(ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani, harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi)
SELECT 'Şeyma Kaya', 'seyma.kaya@abcteknoloji.com', '05320000002', SHA2('1585',256), 'firma_manager', @firma_id, 'Finans', 'Bütçe Yöneticisi', 9500.00, 1, 'ABC-002', 0, 1, 'tr', 'TRY', 'Türkiye', NOW()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'seyma.kaya@abcteknoloji.com');

INSERT INTO users
(ad_soyad, eposta, telefon, sifre, rol, firma_id, departman, gorev_unvani, harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi)
SELECT 'Emre Demir', 'emre.demir@abcteknoloji.com', '05320000003', SHA2('1585',256), 'firma_staff', @firma_id, 'Satış', 'Bölge Satış Uzmanı', 6000.00, 1, 'ABC-003', 0, 1, 'tr', 'TRY', 'Türkiye', NOW()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'emre.demir@abcteknoloji.com');

INSERT INTO firma_harcama_limitleri (firma_id, departman, kullanici_id, gecelik_limit, rezervasyon_basi_limit, aylik_limit, onay_gereksinimi, otomatik_onay_limit, aktif_mi)
SELECT @firma_id, 'Finans', (SELECT id FROM users WHERE eposta = 'seyma.kaya@abcteknoloji.com' LIMIT 1), 4500.00, 18000.00, 120000.00, 1, 7500.00, 1
WHERE NOT EXISTS (
  SELECT 1 FROM firma_harcama_limitleri WHERE firma_id = @firma_id AND departman = 'Finans'
);

INSERT INTO firma_harcama_limitleri (firma_id, departman, kullanici_id, gecelik_limit, rezervasyon_basi_limit, aylik_limit, onay_gereksinimi, otomatik_onay_limit, aktif_mi)
SELECT @firma_id, 'Satış', (SELECT id FROM users WHERE eposta = 'emre.demir@abcteknoloji.com' LIMIT 1), 3500.00, 12000.00, 80000.00, 1, 5000.00, 1
WHERE NOT EXISTS (
  SELECT 1 FROM firma_harcama_limitleri WHERE firma_id = @firma_id AND departman = 'Satış'
);

INSERT INTO firma_ozel_fiyatlar
(firma_id, otel_id, oda_tip_id, minimum_oda_sayisi, maksimum_oda_sayisi, indirim_orani, ozel_fiyat, gecerlilik_baslangic, gecerlilik_bitis, aktif_mi, oncelik_sirasi, aciklama)
SELECT @firma_id, ot.id, od.id, 5, 15, 20.00, ROUND(od.standart_gecelik_fiyat * 0.80, 2), CURDATE(), DATE_ADD(CURDATE(), INTERVAL 180 DAY), 1, 10, 'Demo firma indirimi'
FROM oteller ot
INNER JOIN oda_tipleri od ON od.otel_id = ot.id
WHERE ot.id IN (20, 25, 29)
  AND NOT EXISTS (
      SELECT 1 FROM firma_ozel_fiyatlar foz
      WHERE foz.firma_id = @firma_id AND foz.otel_id = ot.id AND foz.oda_tip_id = od.id AND foz.minimum_oda_sayisi = 5
  );
