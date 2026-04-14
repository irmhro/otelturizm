CREATE TABLE IF NOT EXISTS `destek_makaleleri` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `destek_kategori_id` BIGINT UNSIGNED NOT NULL,
    `baslik` VARCHAR(180) NOT NULL,
    `seo_slug` VARCHAR(180) NOT NULL,
    `ozet` VARCHAR(300) NULL,
    `icerik` LONGTEXT NOT NULL,
    `ikon` VARCHAR(80) NULL,
    `one_cikan_mi` TINYINT(1) NOT NULL DEFAULT 0,
    `yardim_merkezinde_goster` TINYINT(1) NOT NULL DEFAULT 1,
    `siralama` INT NOT NULL DEFAULT 0,
    `durum` TINYINT(1) NOT NULL DEFAULT 1,
    `olusturulma_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `guncellenme_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_destek_makaleleri_seo_slug` (`seo_slug`),
    KEY `idx_destek_makaleleri_kategori` (`destek_kategori_id`),
    CONSTRAINT `fk_destek_makaleleri_kategori` FOREIGN KEY (`destek_kategori_id`) REFERENCES `destek_kategorileri` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_turkish_ci;
