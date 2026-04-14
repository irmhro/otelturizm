CREATE TABLE IF NOT EXISTS `partner_destek_mesajlari` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `talep_id` bigint unsigned NOT NULL,
  `gonderen_kullanici_id` bigint unsigned DEFAULT NULL,
  `gonderen_tipi` enum('Partner','Admin','Sistem') COLLATE utf8mb4_unicode_ci NOT NULL,
  `mesaj` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `ek_dosya_yolu` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `okundu_mu` tinyint(1) DEFAULT '0',
  `olusturulma_tarihi` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_partner_destek_mesaj_talep` (`talep_id`,`olusturulma_tarihi`),
  KEY `idx_partner_destek_mesaj_gonderen` (`gonderen_kullanici_id`),
  CONSTRAINT `fk_partner_destek_mesaj_talep` FOREIGN KEY (`talep_id`) REFERENCES `partner_destek_talepleri` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_partner_destek_mesaj_user` FOREIGN KEY (`gonderen_kullanici_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
