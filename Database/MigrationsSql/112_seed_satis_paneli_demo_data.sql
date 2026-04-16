INSERT INTO users
(
    ad_soyad,
    eposta,
    telefon,
    sifre,
    rol,
    departman,
    gorev_unvani,
    satis_ekibi,
    gunluk_satis_hedefi,
    aylik_satis_hedefi,
    dahili_numara,
    hesap_durumu,
    dil_tercihi,
    para_birimi,
    ulke,
    olusturulma_tarihi
)
SELECT
    'Merve Şahin',
    'merve.sahin@otelturizm.com',
    '05320001001',
    SHA2('1585', 256),
    'sales_agent',
    'Satış',
    'Satış Uzmanı',
    'İstanbul Satış Ekibi',
    5.00,
    320000.00,
    '4101',
    1,
    'tr',
    'TRY',
    'Türkiye',
    SYSDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM users WHERE eposta = 'merve.sahin@otelturizm.com');

INSERT INTO satis_musterileri
(
    musteri_kodu,
    ad_soyad,
    eposta,
    telefon,
    ulke,
    sehir,
    uyelik_seviyesi,
    toplam_rezervasyon_sayisi,
    toplam_harcama,
    son_rezervasyon_tarihi,
    son_talep_ozeti,
    pazarlama_izni,
    notlar,
    olusturan_sales_user_id
)
SELECT
    'SATMUST-0001',
    'Ahmet Yılmaz',
    'ahmet.yilmaz@email.com',
    '+90 532 111 22 33',
    'Türkiye',
    'İstanbul',
    'Gold',
    12,
    128500.00,
    '2026-03-15',
    'Boğaz manzaralı, kahvaltı dahil',
    1,
    'VIP aday müşteri, hızlı teklif dönüşü bekliyor.',
    (SELECT TOP (1) id FROM users WHERE eposta = 'merve.sahin@otelturizm.com')
WHERE NOT EXISTS (SELECT 1 FROM satis_musterileri WHERE musteri_kodu = 'SATMUST-0001');

INSERT INTO satis_musterileri
(
    musteri_kodu,
    ad_soyad,
    eposta,
    telefon,
    ulke,
    sehir,
    uyelik_seviyesi,
    toplam_rezervasyon_sayisi,
    toplam_harcama,
    son_rezervasyon_tarihi,
    son_talep_ozeti,
    pazarlama_izni,
    notlar,
    olusturan_sales_user_id
)
SELECT
    'SATMUST-0002',
    'Zeynep Kaya',
    'zeynep@email.com',
    '+90 533 444 55 66',
    'Türkiye',
    'Ankara',
    'Silver',
    5,
    68400.00,
    '2026-02-18',
    'Aile dostu otel, erken giriş talebi',
    1,
    'Fiyat hassasiyeti yüksek, hafta sonu teklifleri seviyor.',
    (SELECT TOP (1) id FROM users WHERE eposta = 'merve.sahin@otelturizm.com')
WHERE NOT EXISTS (SELECT 1 FROM satis_musterileri WHERE musteri_kodu = 'SATMUST-0002');

INSERT INTO satis_musterileri
(
    musteri_kodu,
    ad_soyad,
    eposta,
    telefon,
    ulke,
    sehir,
    uyelik_seviyesi,
    toplam_rezervasyon_sayisi,
    toplam_harcama,
    son_rezervasyon_tarihi,
    son_talep_ozeti,
    pazarlama_izni,
    notlar,
    olusturan_sales_user_id
)
SELECT
    'SATMUST-0003',
    'John Smith',
    'john.smith@email.com',
    '+44 7700 900111',
    'İngiltere',
    'London',
    'Platinum',
    9,
    241000.00,
    '2026-04-01',
    'Sessiz oda, transfer hizmeti',
    0,
    'Yüksek harcama potansiyeli, hızlı onay bekliyor.',
    (SELECT TOP (1) id FROM users WHERE eposta = 'merve.sahin@otelturizm.com')
WHERE NOT EXISTS (SELECT 1 FROM satis_musterileri WHERE musteri_kodu = 'SATMUST-0003');

INSERT INTO satis_musteri_notlari
(satis_musteri_id, sales_user_id, not_turu, not_metni, planlanan_geri_donus_tarihi)
SELECT
    (SELECT TOP (1) id FROM satis_musterileri WHERE musteri_kodu = 'SATMUST-0001'),
    (SELECT TOP (1) id FROM users WHERE eposta = 'merve.sahin@otelturizm.com'),
    'Çağrı',
    'Müşteri bugün için Boğaz manzaralı alternatifleri sordu. 3 gece için teklif çalışılacak.',
    DATEADD(HOUR, 2, SYSDATETIME())
WHERE NOT EXISTS (
    SELECT 1 FROM satis_musteri_notlari
    WHERE not_metni LIKE 'Müşteri bugün için Boğaz manzaralı alternatifleri sordu.%'
);

UPDATE oteller
SET rezervasyon_telefonu = '+90 212 326 11 00',
    satis_kontak_adi = 'Mehmet Demir',
    satis_kontak_telefonu = '+90 532 111 22 33',
    satis_kontak_eposta = 'mehmet.demir@otelturizm.com',
    satis_notlari = 'VIP müşteriler için uygunluk olduğunda oda yükseltme opsiyonu değerlendirilebilir.'
WHERE id = 20;

UPDATE oteller
SET rezervasyon_telefonu = '+90 216 444 02 16',
    satis_kontak_adi = 'Elif Acar',
    satis_kontak_telefonu = '+90 532 888 77 66',
    satis_kontak_eposta = 'elif.acar@otelturizm.com',
    satis_notlari = 'Kurumsal müşterilere geç çıkış opsiyonu değerlendirilebilir.'
WHERE id = 25;

UPDATE oteller
SET rezervasyon_telefonu = '+90 216 444 03 16',
    satis_kontak_adi = 'Selin Arı',
    satis_kontak_telefonu = '+90 532 666 55 44',
    satis_kontak_eposta = 'selin.ari@otelturizm.com',
    satis_notlari = 'Transfer taleplerinde hızlı dönüş sağlanıyor.'
WHERE id = 29;
