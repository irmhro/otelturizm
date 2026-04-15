SET @schema_name := DATABASE();

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'gonderen_firma_id'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `gonderen_firma_id` bigint unsigned NULL AFTER `gonderen_otel_id`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'gonderen_firma_kullanici_id'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `gonderen_firma_kullanici_id` bigint unsigned NULL AFTER `gonderen_firma_id`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'duzenlendi_mi'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `duzenlendi_mi` tinyint(1) NOT NULL DEFAULT 0 AFTER `duzenlenme_tarihi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'duzenleyen_kullanici_id'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `duzenleyen_kullanici_id` bigint unsigned NULL AFTER `duzenlendi_mi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'misafir_gizlendi_mi'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `misafir_gizlendi_mi` tinyint(1) NOT NULL DEFAULT 0 AFTER `silinme_tarihi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'firma_gizlendi_mi'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `firma_gizlendi_mi` tinyint(1) NOT NULL DEFAULT 0 AFTER `misafir_gizlendi_mi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'otel_gizlendi_mi'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `otel_gizlendi_mi` tinyint(1) NOT NULL DEFAULT 0 AFTER `firma_gizlendi_mi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'silinme_nedeni'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `silinme_nedeni` varchar(255) NULL AFTER `otel_gizlendi_mi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND COLUMN_NAME = 'silinme_gorunum_metni'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD COLUMN `silinme_gorunum_metni` varchar(255) NULL AFTER `silinme_nedeni`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

ALTER TABLE `mesajlar`
    MODIFY COLUMN `gonderen_turu` enum('Misafir','Otel','Firma','Sistem','Destek') COLLATE utf8mb4_unicode_ci NOT NULL,
    MODIFY COLUMN `mesaj_tipi` enum('Metin','Resim','Dosya','Konum','Teklif','Belge','Sistem Bildirimi') COLLATE utf8mb4_unicode_ci DEFAULT 'Metin';

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND INDEX_NAME = 'idx_mesajlar_firma_gonderen'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD KEY `idx_mesajlar_firma_gonderen` (`gonderen_firma_id`)');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND CONSTRAINT_NAME = 'fk_mesajlar_gonderen_firma'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD CONSTRAINT `fk_mesajlar_gonderen_firma` FOREIGN KEY (`gonderen_firma_id`) REFERENCES `firmalar` (`id`) ON DELETE SET NULL');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'mesajlar' AND CONSTRAINT_NAME = 'fk_mesajlar_gonderen_firma_kullanici'),
    'SELECT 1',
    'ALTER TABLE `mesajlar` ADD CONSTRAINT `fk_mesajlar_gonderen_firma_kullanici` FOREIGN KEY (`gonderen_firma_kullanici_id`) REFERENCES `users` (`id`) ON DELETE SET NULL');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
