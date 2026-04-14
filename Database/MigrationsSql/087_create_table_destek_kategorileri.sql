CREATE TABLE IF NOT EXISTS `destek_kategorileri` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `kategori_adi` VARCHAR(120) NOT NULL,
    `seo_slug` VARCHAR(150) NOT NULL,
    `kategori_ikon` VARCHAR(80) NOT NULL,
    `kisa_aciklama` VARCHAR(255) NULL,
    `renk_kodu` VARCHAR(20) NOT NULL DEFAULT '#003B95',
    `siralama` INT NOT NULL DEFAULT 0,
    `durum` TINYINT(1) NOT NULL DEFAULT 1,
    `olusturulma_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `guncellenme_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_destek_kategorileri_seo_slug` (`seo_slug`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_turkish_ci;
