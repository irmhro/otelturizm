SET @schema_name := DATABASE();

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'kvkk_onay_tarihi'),
    'SELECT 1',
    'ALTER TABLE users ADD COLUMN kvkk_onay_tarihi TIMESTAMP NULL AFTER telefon_dogrulama_tarihi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'pazarlama_izni'),
    'SELECT 1',
    'ALTER TABLE users ADD COLUMN pazarlama_izni TINYINT(1) NOT NULL DEFAULT 0 AFTER kvkk_onay_tarihi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'kayit_kaynagi'),
    'SELECT 1',
    'ALTER TABLE users ADD COLUMN kayit_kaynagi VARCHAR(50) NULL AFTER pazarlama_izni'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
