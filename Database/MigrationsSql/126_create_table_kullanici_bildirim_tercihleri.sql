CREATE TABLE IF NOT EXISTS `kullanici_bildirim_tercihleri` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `kullanici_id` bigint unsigned NOT NULL,
  `rezervasyon_eposta` tinyint(1) NOT NULL DEFAULT 1,
  `rezervasyon_push` tinyint(1) NOT NULL DEFAULT 1,
  `checkin_hatirlatma` tinyint(1) NOT NULL DEFAULT 1,
  `iptal_degisim` tinyint(1) NOT NULL DEFAULT 1,
  `kampanya_eposta` tinyint(1) NOT NULL DEFAULT 0,
  `kampanya_sms` tinyint(1) NOT NULL DEFAULT 0,
  `sistem_bildirimi` tinyint(1) NOT NULL DEFAULT 1,
  `olusturulma_tarihi` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `guncellenme_tarihi` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_kullanici_bildirim_tercihleri_user` (`kullanici_id`),
  CONSTRAINT `fk_kullanici_bildirim_tercihleri_user` FOREIGN KEY (`kullanici_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `kullanici_bildirim_tercihleri` (`kullanici_id`)
SELECT `id`
FROM `users`
WHERE `rol` = 'user'
ON DUPLICATE KEY UPDATE `kullanici_id` = `kullanici_id`;
