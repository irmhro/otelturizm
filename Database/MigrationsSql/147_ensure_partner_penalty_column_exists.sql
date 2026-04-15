SET @schema_name := DATABASE();

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'oteller'
          AND COLUMN_NAME = 'partner_ceza_bitis_tarihi'
    ),
    'SELECT 1',
    'ALTER TABLE `oteller` ADD COLUMN `partner_ceza_bitis_tarihi` datetime NULL DEFAULT NULL'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'oteller'
          AND INDEX_NAME = 'idx_oteller_partner_ceza_bitis'
    ),
    'SELECT 1',
    'ALTER TABLE `oteller` ADD INDEX `idx_oteller_partner_ceza_bitis` (`partner_ceza_bitis_tarihi`)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
