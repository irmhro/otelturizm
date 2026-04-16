-- Eksik rol kodlarini ekle (varsa tekrar eklemez)

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama)
SELECT 'satis_yoneticisi', 'Satis Yoneticisi', 'Satis', 75, 0, 'Satis ekibi yonetimi'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM roller WHERE rol_kodu = 'satis_yoneticisi');

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama)
SELECT 'pazarlama_yoneticisi', 'Pazarlama Yoneticisi', 'Pazarlama', 75, 0, 'Pazarlama ekip ve kampanya yonetimi'
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM roller WHERE rol_kodu = 'pazarlama_yoneticisi');

-- Rol atamalari
INSERT INTO kullanici_rolleri (kullanici_id, rol_id, atama_tarihi)
SELECT u.id, r.id, GETDATE()
FROM users u
JOIN roller r ON r.rol_kodu = 'satis_yoneticisi'
LEFT JOIN kullanici_rolleri kr ON kr.kullanici_id = u.id AND kr.rol_id = r.id
WHERE u.eposta = 'satis.yonetici@otelturizm.com'
  AND kr.kullanici_id IS NULL;

INSERT INTO kullanici_rolleri (kullanici_id, rol_id, atama_tarihi)
SELECT u.id, r.id, GETDATE()
FROM users u
JOIN roller r ON r.rol_kodu = 'pazarlama_yoneticisi'
LEFT JOIN kullanici_rolleri kr ON kr.kullanici_id = u.id AND kr.rol_id = r.id
WHERE u.eposta = 'pazarlama.yonetici@otelturizm.com'
  AND kr.kullanici_id IS NULL;
