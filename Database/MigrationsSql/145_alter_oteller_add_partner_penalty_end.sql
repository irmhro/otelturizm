SET @schema_name := DATABASE();

SET @has_partner_penalty_end := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @schema_name
      AND TABLE_NAME = 'oteller'
      AND COLUMN_NAME = 'partner_ceza_bitis_tarihi'
);

SET @sql := IF(
    @has_partner_penalty_end = 0,
    'ALTER TABLE oteller ADD COLUMN partner_ceza_bitis_tarihi TIMESTAMP NULL DEFAULT NULL AFTER guncellenme_tarihi',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_partner_penalty_index := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @schema_name
      AND TABLE_NAME = 'oteller'
      AND INDEX_NAME = 'idx_oteller_partner_ceza_bitis'
);

SET @sql := IF(
    @has_partner_penalty_index = 0,
    'ALTER TABLE oteller ADD INDEX idx_oteller_partner_ceza_bitis (partner_ceza_bitis_tarihi)',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
