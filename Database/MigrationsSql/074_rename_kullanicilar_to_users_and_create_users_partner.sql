-- KULLANICI AYRIMI:
-- 1) kullanicilar tablosunu users olarak yeniden adlandir
-- 2) partner kullanicilarini users_partner tablosunda ayri tut

SET @tbl_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'kullanicilar'
);

SET @users_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'users'
);

SET @rename_sql := IF(
    @tbl_exists = 1 AND @users_exists = 0,
    'RENAME TABLE kullanicilar TO users',
    'SELECT 1'
);
PREPARE stmt FROM @rename_sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS users_partner (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    partner_id BIGINT UNSIGNED NOT NULL,
    rol ENUM('owner', 'manager', 'staff') NOT NULL DEFAULT 'owner',
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    ana_hesap_mi TINYINT(1) NOT NULL DEFAULT 1 COMMENT '1 ise partnerin ana sahibi/yetkilisi',
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_users_partner_user_partner (user_id, partner_id),
    INDEX idx_users_partner_partner_id (partner_id),
    INDEX idx_users_partner_user_id (user_id),
    CONSTRAINT fk_users_partner_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_users_partner_partner FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Partner paneline giris yapabilecek kullanicilarin ayri tablosu';

-- Mevcut partner sahiplerini users_partner tablosuna doldur
INSERT INTO users_partner (user_id, partner_id, rol, aktif_mi, ana_hesap_mi)
SELECT
    p.kullanici_id,
    p.id,
    'owner',
    1,
    1
FROM partner_detaylari p
LEFT JOIN users_partner up
    ON up.user_id = p.kullanici_id
   AND up.partner_id = p.id
WHERE up.id IS NULL;
