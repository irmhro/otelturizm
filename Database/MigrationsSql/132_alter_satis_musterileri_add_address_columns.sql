SET @has_customer_district := (
    SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'satis_musterileri' AND COLUMN_NAME = 'ilce'
);
SET @sql := IF(@has_customer_district = 0,
    'ALTER TABLE satis_musterileri ADD COLUMN ilce VARCHAR(100) NULL AFTER sehir, ADD COLUMN mahalle VARCHAR(120) NULL AFTER ilce, ADD COLUMN adres TEXT NULL AFTER mahalle;',
    'SELECT 1;'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
