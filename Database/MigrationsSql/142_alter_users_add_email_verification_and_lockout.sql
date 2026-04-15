SET @schema_name := DATABASE();

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'users'
          AND COLUMN_NAME = 'basarisiz_giris_sayisi'
    ),
    'SELECT 1;',
    'ALTER TABLE users ADD COLUMN basarisiz_giris_sayisi SMALLINT UNSIGNED NOT NULL DEFAULT 0 AFTER email_dogrulama_tarihi;'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'users'
          AND COLUMN_NAME = 'son_basarisiz_giris_tarihi'
    ),
    'SELECT 1;',
    'ALTER TABLE users ADD COLUMN son_basarisiz_giris_tarihi TIMESTAMP NULL DEFAULT NULL AFTER basarisiz_giris_sayisi;'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'users'
          AND COLUMN_NAME = 'giris_kilit_bitis_tarihi'
    ),
    'SELECT 1;',
    'ALTER TABLE users ADD COLUMN giris_kilit_bitis_tarihi TIMESTAMP NULL DEFAULT NULL AFTER son_basarisiz_giris_tarihi;'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'users'
          AND COLUMN_NAME = 'email_dogrulama_son_gonderim_tarihi'
    ),
    'SELECT 1;',
    'ALTER TABLE users ADD COLUMN email_dogrulama_son_gonderim_tarihi TIMESTAMP NULL DEFAULT NULL AFTER giris_kilit_bitis_tarihi;'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'users'
          AND INDEX_NAME = 'idx_users_email_verification_lockout'
    ),
    'SELECT 1;',
    'CREATE INDEX idx_users_email_verification_lockout ON users (eposta, hesap_durumu, email_dogrulama_tarihi, giris_kilit_bitis_tarihi);'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
