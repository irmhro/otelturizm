INSERT INTO rol_yetkileri (rol_id, yetki_id, izin_var)
SELECT (SELECT id FROM roller WHERE rol_kodu = 'super_admin'), id, 1 FROM yetkiler;

