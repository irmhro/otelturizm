SET @db = DATABASE();

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND COLUMN_NAME = 'kaynak_url'),
    'SELECT 1',
    'ALTER TABLE user_favori_oteller ADD COLUMN kaynak_url VARCHAR(500) NULL AFTER kaynak_sayfa'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND COLUMN_NAME = 'cihaz_tipi'),
    'SELECT 1',
    'ALTER TABLE user_favori_oteller ADD COLUMN cihaz_tipi VARCHAR(50) NULL AFTER kaynak_url'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND COLUMN_NAME = 'ip_adresi'),
    'SELECT 1',
    'ALTER TABLE user_favori_oteller ADD COLUMN ip_adresi VARCHAR(45) NULL AFTER cihaz_tipi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND COLUMN_NAME = 'aktif_mi'),
    'SELECT 1',
    'ALTER TABLE user_favori_oteller ADD COLUMN aktif_mi TINYINT(1) NOT NULL DEFAULT 1 AFTER ip_adresi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND COLUMN_NAME = 'son_islem_tarihi'),
    'SELECT 1',
    'ALTER TABLE user_favori_oteller ADD COLUMN son_islem_tarihi TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP AFTER aktif_mi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND COLUMN_NAME = 'kaldirilma_tarihi'),
    'SELECT 1',
    'ALTER TABLE user_favori_oteller ADD COLUMN kaldirilma_tarihi TIMESTAMP NULL AFTER son_islem_tarihi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND INDEX_NAME = 'idx_user_favori_user_active'),
    'SELECT 1',
    'CREATE INDEX idx_user_favori_user_active ON user_favori_oteller (user_id, aktif_mi, olusturulma_tarihi)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favori_oteller' AND INDEX_NAME = 'idx_user_favori_otel_active'),
    'SELECT 1',
    'CREATE INDEX idx_user_favori_otel_active ON user_favori_oteller (otel_id, aktif_mi)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
