CREATE TABLE IF NOT EXISTS `kullanici_odeme_yontemleri` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `kullanici_id` bigint unsigned NOT NULL,
  `kart_etiketi` varchar(100) NOT NULL,
  `kart_sahibi` varchar(100) NOT NULL,
  `marka` varchar(30) NOT NULL DEFAULT 'Visa',
  `son_dort_hane` char(4) NOT NULL,
  `son_kullanim_ay` tinyint unsigned NOT NULL,
  `son_kullanim_yil` smallint unsigned NOT NULL,
  `varsayilan_mi` tinyint(1) NOT NULL DEFAULT 0,
  `aktif_mi` tinyint(1) NOT NULL DEFAULT 1,
  `olusturulma_tarihi` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `guncellenme_tarihi` timestamp NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_kullanici_odeme_yontemleri_user` (`kullanici_id`),
  CONSTRAINT `fk_kullanici_odeme_yontemleri_user` FOREIGN KEY (`kullanici_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `kullanici_odeme_yontemleri`
(`kullanici_id`, `kart_etiketi`, `kart_sahibi`, `marka`, `son_dort_hane`, `son_kullanim_ay`, `son_kullanim_yil`, `varsayilan_mi`, `aktif_mi`)
SELECT u.id, 'Kişisel Kart', u.ad_soyad, 'Visa', '4242', 12, 2028, 1, 1
FROM users u
WHERE u.rol = 'user'
  AND u.eposta = 'sales.test.175455@otelturizm.com'
  AND NOT EXISTS (
      SELECT 1 FROM kullanici_odeme_yontemleri koy WHERE koy.kullanici_id = u.id
  );
