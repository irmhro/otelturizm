SET @has_guest_city := (
    SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'rezervasyonlar' AND COLUMN_NAME = 'misafir_sehir'
);
SET @sql := IF(@has_guest_city = 0,
    'ALTER TABLE rezervasyonlar ADD COLUMN misafir_sehir VARCHAR(100) NULL AFTER misafir_telefon, ADD COLUMN misafir_ilce VARCHAR(100) NULL AFTER misafir_sehir, ADD COLUMN misafir_mahalle VARCHAR(120) NULL AFTER misafir_ilce, ADD COLUMN misafir_adres TEXT NULL AFTER misafir_mahalle, ADD COLUMN rezervasyon_taslagi_id BIGINT UNSIGNED NULL AFTER satis_musteri_id, ADD COLUMN satis_onaylayan_kullanici_id BIGINT UNSIGNED NULL AFTER firma_onaylayan_kullanici_id, ADD COLUMN satis_onay_tarihi TIMESTAMP NULL AFTER satis_onaylayan_kullanici_id;',
    'SELECT 1;'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
