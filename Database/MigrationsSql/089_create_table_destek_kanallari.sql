CREATE TABLE IF NOT EXISTS `destek_kanallari` (
    `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `kanal_adi` VARCHAR(120) NOT NULL,
    `kanal_turu` VARCHAR(40) NOT NULL,
    `ikon` VARCHAR(80) NOT NULL,
    `aciklama` VARCHAR(255) NOT NULL,
    `buton_metin` VARCHAR(120) NOT NULL,
    `baglanti_url` VARCHAR(255) NOT NULL,
    `ek_bilgi` VARCHAR(180) NULL,
    `renk_tonu` VARCHAR(30) NOT NULL DEFAULT 'primary',
    `siralama` INT NOT NULL DEFAULT 0,
    `aktif_mi` TINYINT(1) NOT NULL DEFAULT 1,
    `olusturulma_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `guncellenme_tarihi` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_destek_kanallari_tur` (`kanal_turu`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_turkish_ci;
