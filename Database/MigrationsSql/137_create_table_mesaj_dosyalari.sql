CREATE TABLE IF NOT EXISTS `mesaj_dosyalari` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `mesaj_id` bigint unsigned NOT NULL,
  `guvenli_dosya_id` bigint unsigned NOT NULL,
  `gosterim_adi` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `siralama` int unsigned NOT NULL DEFAULT '1',
  `aktif_mi` tinyint(1) NOT NULL DEFAULT '1',
  `olusturulma_tarihi` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_mesaj_dosya` (`mesaj_id`,`guvenli_dosya_id`),
  KEY `idx_mesaj_dosyalari_mesaj` (`mesaj_id`),
  CONSTRAINT `fk_mesaj_dosyalari_mesaj` FOREIGN KEY (`mesaj_id`) REFERENCES `mesajlar` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_mesaj_dosyalari_dosya` FOREIGN KEY (`guvenli_dosya_id`) REFERENCES `guvenli_dosya_varliklari` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
