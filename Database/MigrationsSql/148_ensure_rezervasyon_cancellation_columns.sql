SET @schema_name := DATABASE();

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'rezervasyonlar'
          AND COLUMN_NAME = 'iptal_tarihi'
    ),
    'SELECT 1',
    'ALTER TABLE rezervasyonlar ADD COLUMN iptal_tarihi TIMESTAMP NULL'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'rezervasyonlar'
          AND COLUMN_NAME = 'iptal_nedeni'
    ),
    'SELECT 1',
    'ALTER TABLE rezervasyonlar ADD COLUMN iptal_nedeni VARCHAR(500) NULL'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'rezervasyonlar'
          AND COLUMN_NAME = 'iptal_eden'
    ),
    'SELECT 1',
    "ALTER TABLE rezervasyonlar ADD COLUMN iptal_eden ENUM('Misafir','Otel','Platform') NULL"
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = @schema_name
          AND TABLE_NAME = 'rezervasyonlar'
          AND INDEX_NAME = 'idx_rezervasyonlar_iptal_tarihi'
    ),
    'SELECT 1',
    'ALTER TABLE rezervasyonlar ADD INDEX idx_rezervasyonlar_iptal_tarihi (iptal_tarihi)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
