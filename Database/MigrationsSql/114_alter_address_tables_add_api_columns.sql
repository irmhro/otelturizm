SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'iller'
      AND COLUMN_NAME = 'nufus'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE iller ADD COLUMN nufus INT UNSIGNED NULL AFTER boylam',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ilceler'
      AND COLUMN_NAME = 'api_kodu'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE ilceler ADD COLUMN api_kodu INT UNSIGNED NULL AFTER dis_kod',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ilceler'
      AND COLUMN_NAME = 'nufus'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE ilceler ADD COLUMN nufus INT UNSIGNED NULL AFTER boylam',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'mahalleler'
      AND COLUMN_NAME = 'api_kodu'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE mahalleler ADD COLUMN api_kodu INT UNSIGNED NULL AFTER ilce_id',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'mahalleler'
      AND COLUMN_NAME = 'nufus'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE mahalleler ADD COLUMN nufus INT UNSIGNED NULL AFTER boylam',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ilceler'
      AND INDEX_NAME = 'uk_ilceler_api_kodu'
);
SET @sql := IF(@idx_exists = 0,
    'ALTER TABLE ilceler ADD UNIQUE KEY uk_ilceler_api_kodu (api_kodu)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'mahalleler'
      AND INDEX_NAME = 'uk_mahalle_api_kodu'
);
SET @sql := IF(@idx_exists = 0,
    'ALTER TABLE mahalleler ADD UNIQUE KEY uk_mahalle_api_kodu (api_kodu)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
