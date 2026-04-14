CREATE TABLE IF NOT EXISTS `sss_sorulari` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `sss_kategori_id` BIGINT UNSIGNED NOT NULL,
    `soru` VARCHAR(255) NOT NULL,
    `cevap` TEXT NOT NULL,
    `one_cikan_mi` TINYINT(1) NOT NULL DEFAULT 0,
    `siralama` INT NOT NULL DEFAULT 0,
    `aktif_mi` TINYINT(1) NOT NULL DEFAULT 1,
    `olusturulma_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `guncellenme_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_sss_sorulari_kategori` (`sss_kategori_id`),
    CONSTRAINT `fk_sss_sorulari_kategori` FOREIGN KEY (`sss_kategori_id`) REFERENCES `sss_kategorileri` (`id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_turkish_ci;
