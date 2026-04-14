CREATE TABLE iller (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    plaka_kodu SMALLINT UNSIGNED NOT NULL,
    il_adi VARCHAR(100) NOT NULL,
    seo_slug VARCHAR(120) NOT NULL,
    bolge VARCHAR(50) NULL,
    enlem DECIMAL(10,8) NULL,
    boylam DECIMAL(11,8) NULL,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uk_iller_plaka (plaka_kodu),
    UNIQUE KEY uk_iller_slug (seo_slug),
    INDEX idx_iller_ad (il_adi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Il ana tablosu (koordinat destekli)';
