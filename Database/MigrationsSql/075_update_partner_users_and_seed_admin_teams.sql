-- PARTNER KULLANICILARI GUNCELLEME
-- - ad_soyad: otel adi ile uyumlu hale gelir
-- - eposta: verilen listede varsa guncellenir, yoksa mevcut deger korunur
-- - sifre: SHA2('1585', 256)

UPDATE users u
JOIN users_partner up ON up.user_id = u.id
JOIN (
    SELECT 2 AS partner_id, 'EAGLE PALACE SUITE' AS otel_adi, 'eaglepalaceonline@gmail.com' AS eposta
    UNION ALL SELECT 3, 'COMFORT INN SUITE', 'comforttsuite@gmail.com'
    UNION ALL SELECT 4, '216 PALACE SUITE', '216palace216hotel@gmail.com'
    UNION ALL SELECT 6, 'HILL SUITE', 'pendikhillsuites@gmail.com'
    UNION ALL SELECT 7, 'PASHA PALACE SUITE', 'pashapalacehotel@gmail.com'
    UNION ALL SELECT 9, 'F&B SUITE', NULL
    UNION ALL SELECT 14, 'MACITY HOTEL', 'rhisshotelmaltepe21@gmail.com'
    UNION ALL SELECT 15, 'MY VILLA', NULL
    UNION ALL SELECT 17, 'BOSTANCI SUITE', 'bostancisuite1@gmail.com'
    UNION ALL SELECT 18, '216 TREND SUITE', '216umraniyesuite@gmail.com'
    UNION ALL SELECT 20, '216 BOSPHORUS SUITE', '216bosphorus@gmail.com'
    UNION ALL SELECT 21, '216 STATION SUITE', '216station@gmail.com'
    UNION ALL SELECT 22, '216 CENTER', NULL
    UNION ALL SELECT 23, '216 RUBY SUITE', '216rubysuite@gmail.com'
    UNION ALL SELECT 24, '216 STYLE SUITE', '216stylesuite@gmail.com'
    UNION ALL SELECT 25, '216 PRESTIGE HOTEL', NULL
    UNION ALL SELECT 26, '216 STAR SUITE', '216starsuit@gmail.com'
    UNION ALL SELECT 27, '216 NORTH SUITE', NULL
    UNION ALL SELECT 29, '216 SILVER SUITE', '216silvertuzla@gmail.com'
    UNION ALL SELECT 30, '216 CASTLE SUITE', '216castlehotel@gmail.com'
) x ON x.partner_id = up.partner_id
SET
    u.ad_soyad = x.otel_adi,
    u.eposta = COALESCE(x.eposta, u.eposta),
    u.sifre = SHA2('1585', 256);

-- SUPERADMIN + DEPARTMAN YONETICI/EKIP KULLANICILARI
-- Eposta varsa tekrar acmaz; rol/departman iliskilerini tamamlar

INSERT INTO users (
    ad_soyad, eposta, telefon, sifre, hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi
)
SELECT
    x.ad_soyad,
    x.eposta,
    x.telefon,
    SHA2('1585', 256),
    1,
    'tr',
    'TRY',
    'Turkiye',
    NOW()
FROM (
    SELECT 'Super Admin Root' AS ad_soyad, 'root@otelturizm.com' AS eposta, NULL AS telefon, 'super_admin' AS rol_kodu, 'YK' AS departman_kodu
    UNION ALL SELECT 'Genel Mudur', 'genelmudur@otelturizm.com', NULL, 'genel_mudur', 'GM'
    UNION ALL SELECT 'Finans Yoneticisi', 'finans.yonetici@otelturizm.com', NULL, 'finans_yoneticisi', 'FIN'
    UNION ALL SELECT 'Muhasebe Uzmani', 'muhasebe.uzmani@otelturizm.com', NULL, 'muhasebe_uzmani', 'FIN'
    UNION ALL SELECT 'Satis Yoneticisi', 'satis.yonetici@otelturizm.com', NULL, 'satis_yoneticisi', 'SAT'
    UNION ALL SELECT 'Operasyon Yoneticisi', 'operasyon.yonetici@otelturizm.com', NULL, 'operasyon_yoneticisi', 'OPS'
    UNION ALL SELECT 'Destek Yoneticisi', 'destek.yonetici@otelturizm.com', NULL, 'destek_yoneticisi', 'DESTEK'
    UNION ALL SELECT 'Destek Uzmani', 'destek.uzmani@otelturizm.com', NULL, 'destek_uzmani', 'DESTEK'
    UNION ALL SELECT 'Pazarlama Yoneticisi', 'pazarlama.yonetici@otelturizm.com', NULL, 'pazarlama_yoneticisi', 'PAZ'
    UNION ALL SELECT 'Teknik Lider', 'teknik.lider@otelturizm.com', NULL, 'teknik_lider', 'IT'
    UNION ALL SELECT 'Yazilim Uzmanni', 'yazilim.uzmani@otelturizm.com', NULL, 'yazilim_uzmani', 'IT'
    UNION ALL SELECT 'Veritabani Yoneticisi', 'dba@otelturizm.com', NULL, 'veritabani_yoneticisi', 'IT'
    UNION ALL SELECT 'Hukuk Musaviri', 'hukuk.musaviri@otelturizm.com', NULL, 'hukuk_musaviri', 'HUK'
    UNION ALL SELECT 'IK Yoneticisi', 'ik.yonetici@otelturizm.com', NULL, 'ik_yoneticisi', 'IK'
) x
LEFT JOIN users u ON u.eposta = x.eposta
WHERE u.id IS NULL;

-- Rol atamalari
INSERT INTO kullanici_rolleri (kullanici_id, rol_id, atama_tarihi)
SELECT
    u.id,
    r.id,
    NOW()
FROM (
    SELECT 'root@otelturizm.com' AS eposta, 'super_admin' AS rol_kodu
    UNION ALL SELECT 'genelmudur@otelturizm.com', 'genel_mudur'
    UNION ALL SELECT 'finans.yonetici@otelturizm.com', 'finans_yoneticisi'
    UNION ALL SELECT 'muhasebe.uzmani@otelturizm.com', 'muhasebe_uzmani'
    UNION ALL SELECT 'satis.yonetici@otelturizm.com', 'satis_yoneticisi'
    UNION ALL SELECT 'operasyon.yonetici@otelturizm.com', 'operasyon_yoneticisi'
    UNION ALL SELECT 'destek.yonetici@otelturizm.com', 'destek_yoneticisi'
    UNION ALL SELECT 'destek.uzmani@otelturizm.com', 'destek_uzmani'
    UNION ALL SELECT 'pazarlama.yonetici@otelturizm.com', 'pazarlama_yoneticisi'
    UNION ALL SELECT 'teknik.lider@otelturizm.com', 'teknik_lider'
    UNION ALL SELECT 'yazilim.uzmani@otelturizm.com', 'yazilim_uzmani'
    UNION ALL SELECT 'dba@otelturizm.com', 'veritabani_yoneticisi'
    UNION ALL SELECT 'hukuk.musaviri@otelturizm.com', 'hukuk_musaviri'
    UNION ALL SELECT 'ik.yonetici@otelturizm.com', 'ik_yoneticisi'
) x
JOIN users u ON u.eposta = x.eposta
JOIN roller r ON r.rol_kodu = x.rol_kodu
LEFT JOIN kullanici_rolleri kr ON kr.kullanici_id = u.id AND kr.rol_id = r.id
WHERE kr.kullanici_id IS NULL;

-- Departman atamalari
INSERT INTO kullanici_departman (kullanici_id, departman_id, unvan, ise_baslama_tarihi, yonetici_mi)
SELECT
    u.id,
    d.id,
    r.rol_adi,
    CURDATE(),
    CASE WHEN r.seviye >= 70 THEN 1 ELSE 0 END
FROM (
    SELECT 'root@otelturizm.com' AS eposta, 'YK' AS departman_kodu, 'super_admin' AS rol_kodu
    UNION ALL SELECT 'genelmudur@otelturizm.com', 'GM', 'genel_mudur'
    UNION ALL SELECT 'finans.yonetici@otelturizm.com', 'FIN', 'finans_yoneticisi'
    UNION ALL SELECT 'muhasebe.uzmani@otelturizm.com', 'FIN', 'muhasebe_uzmani'
    UNION ALL SELECT 'satis.yonetici@otelturizm.com', 'SAT', 'satis_yoneticisi'
    UNION ALL SELECT 'operasyon.yonetici@otelturizm.com', 'OPS', 'operasyon_yoneticisi'
    UNION ALL SELECT 'destek.yonetici@otelturizm.com', 'DESTEK', 'destek_yoneticisi'
    UNION ALL SELECT 'destek.uzmani@otelturizm.com', 'DESTEK', 'destek_uzmani'
    UNION ALL SELECT 'pazarlama.yonetici@otelturizm.com', 'PAZ', 'pazarlama_yoneticisi'
    UNION ALL SELECT 'teknik.lider@otelturizm.com', 'IT', 'teknik_lider'
    UNION ALL SELECT 'yazilim.uzmani@otelturizm.com', 'IT', 'yazilim_uzmani'
    UNION ALL SELECT 'dba@otelturizm.com', 'IT', 'veritabani_yoneticisi'
    UNION ALL SELECT 'hukuk.musaviri@otelturizm.com', 'HUK', 'hukuk_musaviri'
    UNION ALL SELECT 'ik.yonetici@otelturizm.com', 'IK', 'ik_yoneticisi'
) x
JOIN users u ON u.eposta = x.eposta
JOIN departmanlar d ON d.departman_kodu = x.departman_kodu
JOIN roller r ON r.rol_kodu = x.rol_kodu
LEFT JOIN kullanici_departman kd ON kd.kullanici_id = u.id AND kd.departman_id = d.id
WHERE kd.kullanici_id IS NULL;
