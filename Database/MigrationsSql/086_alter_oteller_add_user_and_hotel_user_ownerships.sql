SET @col_hotel_user_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'oteller'
      AND COLUMN_NAME = 'user_id'
);

SET @col_hotel_user_sql := IF(
    @col_hotel_user_exists = 0,
    'ALTER TABLE oteller ADD COLUMN user_id BIGINT UNSIGNED NULL AFTER partner_id',
    'SELECT 1'
);
PREPARE stmt FROM @col_hotel_user_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_hotel_user_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'oteller'
      AND INDEX_NAME = 'idx_oteller_user_id'
);

SET @idx_hotel_user_sql := IF(
    @idx_hotel_user_exists = 0,
    'ALTER TABLE oteller ADD INDEX idx_oteller_user_id (user_id)',
    'SELECT 1'
);
PREPARE stmt FROM @idx_hotel_user_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_hotel_user_exists := (
    SELECT COUNT(*)
    FROM information_schema.REFERENTIAL_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND CONSTRAINT_NAME = 'fk_oteller_user'
);

SET @fk_hotel_user_sql := IF(
    @fk_hotel_user_exists = 0,
    'ALTER TABLE oteller ADD CONSTRAINT fk_oteller_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL',
    'SELECT 1'
);
PREPARE stmt FROM @fk_hotel_user_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS otel_kullanici_sahiplikleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    user_id BIGINT UNSIGNED NOT NULL,
    partner_id BIGINT UNSIGNED NOT NULL,
    rol ENUM('owner', 'manager', 'staff') NOT NULL DEFAULT 'owner',
    ana_sorumlu_mu TINYINT(1) NOT NULL DEFAULT 0,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE KEY uk_otel_kullanici_sahiplik (otel_id, user_id),
    INDEX idx_otel_kullanici_sahiplik_otel_id (otel_id),
    INDEX idx_otel_kullanici_sahiplik_user_id (user_id),
    INDEX idx_otel_kullanici_sahiplik_partner_id (partner_id),

    CONSTRAINT fk_otel_kullanici_sahiplik_otel FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    CONSTRAINT fk_otel_kullanici_sahiplik_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_otel_kullanici_sahiplik_partner FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Bir otelin birden fazla partner kullanicisi tarafindan owner/manager/staff olarak yonetilmesini saglar';

INSERT INTO otel_kullanici_sahiplikleri (otel_id, user_id, partner_id, rol, ana_sorumlu_mu, aktif_mi, olusturulma_tarihi)
SELECT
    o.id,
    up.user_id,
    o.partner_id,
    up.rol,
    up.ana_hesap_mi,
    up.aktif_mi,
    NOW()
FROM oteller o
JOIN users_partner up
    ON up.partner_id = o.partner_id
WHERE NOT EXISTS (
    SELECT 1
    FROM otel_kullanici_sahiplikleri oku
    WHERE oku.otel_id = o.id
      AND oku.user_id = up.user_id
);

UPDATE oteller o
JOIN (
    SELECT
        oku.otel_id,
        SUBSTRING_INDEX(
            GROUP_CONCAT(oku.user_id ORDER BY oku.ana_sorumlu_mu DESC, oku.rol = 'owner' DESC, oku.id ASC),
            ',',
            1
        ) AS birincil_user_id
    FROM otel_kullanici_sahiplikleri oku
    WHERE oku.aktif_mi = 1
    GROUP BY oku.otel_id
) x ON x.otel_id = o.id
SET o.user_id = x.birincil_user_id;
