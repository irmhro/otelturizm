CREATE TABLE departmanlar (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    departman_kodu VARCHAR(30) NOT NULL UNIQUE,
    departman_adi VARCHAR(50) NOT NULL,
    ust_departman_id SMALLINT UNSIGNED NULL,
    yonetici_rol_id SMALLINT UNSIGNED NULL COMMENT 'Bu departmanın varsayılan yönetici rolü',
    bina_kat VARCHAR(20) NULL,
    dahili_telefon VARCHAR(10) NULL,
    aciklama VARCHAR(255),
    aktif_mi TINYINT(1) DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (ust_departman_id) REFERENCES departmanlar(id) ON DELETE SET NULL,
    FOREIGN KEY (yonetici_rol_id) REFERENCES roller(id) ON DELETE SET NULL,
    
    INDEX idx_departman_kodu (departman_kodu),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Şirket departman hiyerarşisi';

