SET @has_mahalle := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'mahalle'
);
SET @sql := IF(@has_mahalle = 0,
    'ALTER TABLE users ADD COLUMN mahalle VARCHAR(120) NULL AFTER ilce;',
    'SELECT 1;'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
