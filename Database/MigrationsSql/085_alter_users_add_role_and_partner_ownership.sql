SET @col_role_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'rol'
);

SET @col_role_sql := IF(
    @col_role_exists = 0,
    'ALTER TABLE users ADD COLUMN rol ENUM(''user'', ''admin'', ''partner_owner'', ''partner_manager'', ''partner_staff'') NOT NULL DEFAULT ''user'' AFTER sifre',
    'SELECT 1'
);
PREPARE stmt FROM @col_role_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_owner_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND COLUMN_NAME = 'sahiplik_partner_id'
);

SET @col_owner_sql := IF(
    @col_owner_exists = 0,
    'ALTER TABLE users ADD COLUMN sahiplik_partner_id BIGINT UNSIGNED NULL AFTER rol',
    'SELECT 1'
);
PREPARE stmt FROM @col_owner_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_role_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND INDEX_NAME = 'idx_users_rol'
);

SET @idx_role_sql := IF(
    @idx_role_exists = 0,
    'ALTER TABLE users ADD INDEX idx_users_rol (rol)',
    'SELECT 1'
);
PREPARE stmt FROM @idx_role_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_owner_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
      AND INDEX_NAME = 'idx_users_sahiplik_partner_id'
);

SET @idx_owner_sql := IF(
    @idx_owner_exists = 0,
    'ALTER TABLE users ADD INDEX idx_users_sahiplik_partner_id (sahiplik_partner_id)',
    'SELECT 1'
);
PREPARE stmt FROM @idx_owner_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.REFERENTIAL_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND CONSTRAINT_NAME = 'fk_users_sahiplik_partner'
);

SET @fk_sql := IF(
    @fk_exists = 0,
    'ALTER TABLE users ADD CONSTRAINT fk_users_sahiplik_partner FOREIGN KEY (sahiplik_partner_id) REFERENCES partner_detaylari(id) ON DELETE SET NULL',
    'SELECT 1'
);
PREPARE stmt FROM @fk_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE users u
JOIN users_partner up
    ON up.user_id = u.id
   AND up.aktif_mi = 1
SET
    u.rol = CASE up.rol
        WHEN 'owner' THEN 'partner_owner'
        WHEN 'manager' THEN 'partner_manager'
        ELSE 'partner_staff'
    END,
    u.sahiplik_partner_id = up.partner_id;

UPDATE users u
SET u.rol = 'admin'
WHERE EXISTS (
    SELECT 1
    FROM kullanici_rolleri kr
    JOIN roller r ON r.id = kr.rol_id
    WHERE kr.kullanici_id = u.id
      AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > NOW())
);
