-- Legacy aktarimda partner_id = 1 gelen kayitlari,
-- otel id'si bazli partner sahipligine cevirir.

-- 1) Var olan partner kayitlarinin kullanici FK'si icin eksik kullanicilari tamamla
INSERT INTO kullanicilar (
    id, ad_soyad, eposta, telefon, sifre, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi
)
SELECT
    p.kullanici_id,
    CONCAT('Partner Kullanici ', p.kullanici_id),
    CONCAT('partner-kullanici-', p.kullanici_id, '@otelturizm.com'),
    CONCAT('+90', LPAD(p.kullanici_id, 10, '0')),
    'LEGACY_MIGRATION',
    1,
    'tr',
    'TRY',
    'Turkiye',
    GETDATE()
FROM partner_detaylari p
LEFT JOIN kullanicilar k ON k.id = p.kullanici_id
WHERE k.id IS NULL;

-- 2) Legacy otel id'leri icin kullanici kaydi yoksa olustur
INSERT INTO kullanicilar (
    id, ad_soyad, eposta, telefon, sifre, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi
)
SELECT
    o.id,
    CONCAT('Legacy Partner ', o.id),
    CONCAT('legacy-partner-', o.id, '@otelturizm.com'),
    CONCAT('+90', LPAD(o.id + 500000, 10, '0')),
    'LEGACY_MIGRATION',
    1,
    'tr',
    'TRY',
    'Turkiye',
    GETDATE()
FROM oteller o
LEFT JOIN kullanicilar k ON k.id = o.id
WHERE o.id IN (2,3,4,6,7,9,11,14,15,17,18,20,21,22,23,24,25,26,27,28,29,30)
  AND k.id IS NULL;

-- 3) Legacy otel id'leri icin partner_detaylari kaydi yoksa olustur
INSERT INTO partner_detaylari (
    id,
    kullanici_id,
    firma_unvani,
    firma_turu,
    vergi_dairesi,
    vergi_numarasi,
    fatura_adresi,
    fatura_il,
    fatura_ilce,
    yetkili_ad_soyad,
    yetkili_tc_no,
    yetkili_telefon,
    yetkili_eposta,
    banka_adi,
    iban,
    hesap_sahibi_adi,
    onay_durumu,
    onay_tarihi
)
SELECT
    o.id,
    o.id,
    CONCAT(o.otel_adi, ' Isletmesi'),
    'Limited Şirketi',
    'Legacy Vergi Dairesi',
    CONCAT('L', LPAD(o.id, 9, '0')),
    o.tam_adres,
    o.sehir,
    o.ilce,
    CONCAT('Yetkili ', o.id),
    LPAD(o.id, 11, '0'),
    CONCAT('+90', LPAD(o.id + 600000, 10, '0')),
    CONCAT('yetkili-', o.id, '@otelturizm.com'),
    'Is Bankasi',
    CONCAT('TR', LPAD(o.id, 24, '0')),
    CONCAT(o.otel_adi, ' Isletmesi'),
    'Onaylandi',
    GETDATE()
FROM oteller o
LEFT JOIN partner_detaylari p ON p.id = o.id
WHERE o.id IN (2,3,4,6,7,9,11,14,15,17,18,20,21,22,23,24,25,26,27,28,29,30)
  AND p.id IS NULL;

-- 4) Partner eslestirmesi: otel id -> partner_id
UPDATE oteller
SET partner_id = id
WHERE id IN (2,3,4,6,7,9,11,14,15,17,18,20,21,22,23,24,25,26,27,28,29,30);
