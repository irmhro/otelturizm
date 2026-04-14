CREATE TABLE roller (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    rol_kodu VARCHAR(30) NOT NULL UNIQUE COMMENT 'Sistemde kullanılacak sabit kod',
    rol_adi VARCHAR(50) NOT NULL COMMENT 'Görünen ad',
    departman VARCHAR(50) NOT NULL COMMENT 'Yönetim, Operasyon, Finans, Satış, Destek, IT, Hukuk',
    seviye TINYINT UNSIGNED NOT NULL COMMENT '1-99 arası yetki seviyesi',
    ust_rol_id SMALLINT UNSIGNED NULL COMMENT 'Hiyerarşi için üst rol',
    varsayilan_mi TINYINT(1) DEFAULT 0 COMMENT 'Yeni kullanıcıya otomatik atanır mı?',
    aciklama VARCHAR(255),
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    INDEX idx_departman (departman),
    INDEX idx_seviye (seviye),
    INDEX idx_rol_kodu (rol_kodu),
    FOREIGN KEY (ust_rol_id) REFERENCES roller(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kurumsal rol ve departman tanımları';

