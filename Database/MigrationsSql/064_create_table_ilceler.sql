CREATE TABLE ilceler (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    il_id BIGINT UNSIGNED NOT NULL,
    dis_kod INT UNSIGNED NULL COMMENT 'Eski sistem ilce_id eslestirme kodu',
    ilce_adi VARCHAR(100) NOT NULL,
    seo_slug VARCHAR(140) NOT NULL,
    merkez_mi TINYINT(1) NOT NULL DEFAULT 0,
    enlem DECIMAL(10,8) NULL,
    boylam DECIMAL(11,8) NULL,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_ilceler_il_slug (il_id, seo_slug),
    UNIQUE KEY uk_ilceler_il_diskod (il_id, dis_kod),
    INDEX idx_ilceler_il_id (il_id),
    INDEX idx_ilceler_ad (ilce_adi),
    CONSTRAINT fk_ilceler_il FOREIGN KEY (il_id) REFERENCES iller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Ilce tablosu (koordinat + eski sistem kod eslestirme)';
