CREATE TABLE IF NOT EXISTS `otel_istatistikleri` (
  `id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `otel_id` bigint unsigned NOT NULL,
  `istatistik_tarihi` date NOT NULL,
  `rezervasyon_sayisi` int unsigned DEFAULT '0',
  `iptal_sayisi` int unsigned DEFAULT '0',
  `doluluk_orani` decimal(5,2) DEFAULT '0.00',
  `brut_gelir` decimal(12,2) DEFAULT '0.00',
  `net_gelir` decimal(12,2) DEFAULT '0.00',
  `ortalama_puan` decimal(3,2) DEFAULT '0.00',
  `yorum_sayisi` int unsigned DEFAULT '0',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_otel_istatistikleri_tarih` (`otel_id`,`istatistik_tarihi`),
  KEY `idx_otel_istatistikleri_tarih` (`istatistik_tarihi`),
  CONSTRAINT `fk_otel_istatistikleri_otel` FOREIGN KEY (`otel_id`) REFERENCES `oteller` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
