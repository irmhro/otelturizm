SET @schema_name := DATABASE();

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND COLUMN_NAME = 'konusma_turu'),
    'SELECT 1',
    "ALTER TABLE `mesaj_konusmalari` ADD COLUMN `konusma_turu` enum('Otel','Firma','Destek') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Otel' AFTER `konu_basligi`");
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND COLUMN_NAME = 'firma_id'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD COLUMN `firma_id` bigint unsigned NULL AFTER `otel_id`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND COLUMN_NAME = 'firma_kullanici_id'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD COLUMN `firma_kullanici_id` bigint unsigned NULL AFTER `firma_id`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND COLUMN_NAME = 'firma_okunmamis_sayisi'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD COLUMN `firma_okunmamis_sayisi` int unsigned NOT NULL DEFAULT 0 AFTER `otel_okunmamis_sayisi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND COLUMN_NAME = 'firma_son_okuma_tarihi'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD COLUMN `firma_son_okuma_tarihi` timestamp NULL DEFAULT NULL AFTER `otel_son_okuma_tarihi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

ALTER TABLE `mesaj_konusmalari`
    MODIFY COLUMN `otel_id` bigint unsigned NULL,
    MODIFY COLUMN `konu_kategorisi` enum('Rezervasyon','Özel İstek','İptal/Değişiklik','Ödeme','Giriş/Çıkış','Oda','Ulaşım','Firma','Belge','Diğer') COLLATE utf8mb4_unicode_ci DEFAULT 'Diğer',
    MODIFY COLUMN `son_mesaj_gonderen` enum('Misafir','Otel','Firma','Sistem','Destek') COLLATE utf8mb4_unicode_ci DEFAULT NULL;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND INDEX_NAME = 'idx_firma_id'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD KEY `idx_firma_id` (`firma_id`)');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND INDEX_NAME = 'idx_firma_mesaj_okunmamis'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD KEY `idx_firma_mesaj_okunmamis` (`firma_id`,`firma_okunmamis_sayisi`)');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND CONSTRAINT_NAME = 'fk_mesaj_konusma_firma'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD CONSTRAINT `fk_mesaj_konusma_firma` FOREIGN KEY (`firma_id`) REFERENCES `firmalar` (`id`) ON DELETE SET NULL');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesaj_konusmalari' AND CONSTRAINT_NAME = 'fk_mesaj_konusma_firma_kullanici'),
    'SELECT 1',
    'ALTER TABLE `mesaj_konusmalari` ADD CONSTRAINT `fk_mesaj_konusma_firma_kullanici` FOREIGN KEY (`firma_kullanici_id`) REFERENCES `users` (`id`) ON DELETE SET NULL');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
