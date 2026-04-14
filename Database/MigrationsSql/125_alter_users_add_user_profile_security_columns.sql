SET @schema_name := DATABASE();

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'tc_kimlik_no'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `tc_kimlik_no` varchar(11) NULL AFTER `telefon`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'dogum_tarihi'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `dogum_tarihi` date NULL AFTER `tc_kimlik_no`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'cinsiyet'),
    'SELECT 1',
    "ALTER TABLE `users` ADD COLUMN `cinsiyet` enum('Erkek','Kadın','Belirtmek İstemiyorum') NULL AFTER `dogum_tarihi`");
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'uyruk'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `uyruk` varchar(50) NULL AFTER `cinsiyet`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'adres'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `adres` text NULL AFTER `uyruk`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'sehir'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `sehir` varchar(100) NULL AFTER `adres`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'ilce'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `ilce` varchar(100) NULL AFTER `sehir`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'posta_kodu'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `posta_kodu` varchar(10) NULL AFTER `ilce`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'tercih_edilen_oda_tipi'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `tercih_edilen_oda_tipi` varchar(50) NULL AFTER `posta_kodu`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'yatak_tercihi'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `yatak_tercihi` varchar(50) NULL AFTER `tercih_edilen_oda_tipi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'konusulan_diller'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `konusulan_diller` varchar(200) NULL AFTER `yatak_tercihi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'seyahat_amaci'),
    'SELECT 1',
    "ALTER TABLE `users` ADD COLUMN `seyahat_amaci` enum('İş','Tatil','Her İkisi') NULL AFTER `konusulan_diller`");
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'ozel_istekler'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `ozel_istekler` text NULL AFTER `seyahat_amaci`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'iki_asamali_dogrulama_aktif_mi'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `iki_asamali_dogrulama_aktif_mi` tinyint(1) NOT NULL DEFAULT 0 AFTER `ozel_istekler`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema_name AND TABLE_NAME = 'users' AND COLUMN_NAME = 'profil_tamamlanma_tarihi'),
    'SELECT 1',
    'ALTER TABLE `users` ADD COLUMN `profil_tamamlanma_tarihi` timestamp NULL DEFAULT NULL AFTER `iki_asamali_dogrulama_aktif_mi`');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
