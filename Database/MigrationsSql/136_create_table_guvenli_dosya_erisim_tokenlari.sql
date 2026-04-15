CREATE TABLE IF NOT EXISTS `guvenli_dosya_erisim_tokenlari` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `guvenli_dosya_id` bigint unsigned NOT NULL,
  `erisim_tokeni` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `kullanici_id` bigint unsigned NOT NULL,
  `hesap_tipi` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `kullanim_sayisi` int unsigned NOT NULL DEFAULT '0',
  `maksimum_kullanim_sayisi` int unsigned DEFAULT NULL,
  `gecerlilik_tarihi` timestamp NOT NULL,
  `son_erisim_tarihi` timestamp NULL DEFAULT NULL,
  `iptal_tarihi` timestamp NULL DEFAULT NULL,
  `olusturulma_tarihi` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_guvenli_token` (`erisim_tokeni`),
  KEY `idx_guvenli_token_kullanici` (`kullanici_id`,`hesap_tipi`),
  KEY `idx_guvenli_token_gecerlilik` (`gecerlilik_tarihi`),
  CONSTRAINT `fk_guvenli_token_dosya` FOREIGN KEY (`guvenli_dosya_id`) REFERENCES `guvenli_dosya_varliklari` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_guvenli_token_kullanici` FOREIGN KEY (`kullanici_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
