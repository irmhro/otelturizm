SET @db = DATABASE();

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'seo_slug'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN seo_slug VARCHAR(255) NULL AFTER kampanya_adi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'sayfa_url'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN sayfa_url VARCHAR(255) NULL AFTER seo_slug'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'kisa_aciklama'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN kisa_aciklama VARCHAR(255) NULL AFTER kampanya_aciklamasi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'detay_aciklama'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN detay_aciklama LONGTEXT NULL AFTER kisa_aciklama'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'hero_gorseli'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN hero_gorseli VARCHAR(500) NULL AFTER banner_gorseli'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'kart_gorseli'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN kart_gorseli VARCHAR(500) NULL AFTER hero_gorseli'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'mobil_gorsel'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN mobil_gorsel VARCHAR(500) NULL AFTER kart_gorseli'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'meta_title'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN meta_title VARCHAR(255) NULL AFTER mobil_gorsel'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'meta_description'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN meta_description VARCHAR(500) NULL AFTER meta_title'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'canonical_url'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN canonical_url VARCHAR(500) NULL AFTER meta_description'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'kampanya_etiketi'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN kampanya_etiketi VARCHAR(100) NULL AFTER canonical_url'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'promo_badge'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN promo_badge VARCHAR(100) NULL AFTER kampanya_etiketi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'kampanya_renk_kodu'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN kampanya_renk_kodu VARCHAR(20) NULL AFTER promo_badge'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'listeleme_basligi'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN listeleme_basligi VARCHAR(255) NULL AFTER kampanya_renk_kodu'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'listeleme_aciklamasi'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN listeleme_aciklamasi VARCHAR(500) NULL AFTER listeleme_basligi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'kullanim_kosullari'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN kullanim_kosullari TEXT NULL AFTER listeleme_aciklamasi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'hedef_ilceler'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN hedef_ilceler JSON NULL AFTER hedef_sehirler'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'hedef_mahalleler'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN hedef_mahalleler JSON NULL AFTER hedef_ilceler'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'gorunurluk_durumu'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN gorunurluk_durumu ENUM(''Taslak'',''Zamanlanmış'',''Yayında'',''Pasif'',''Sona Erdi'') NOT NULL DEFAULT ''Taslak'' AFTER aktif_mi'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'partner_katilim_acik'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN partner_katilim_acik TINYINT(1) NOT NULL DEFAULT 1 AFTER gorunurluk_durumu'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'partner_katilim_baslangic'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN partner_katilim_baslangic DATETIME NULL AFTER partner_katilim_acik'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'partner_katilim_bitis'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN partner_katilim_bitis DATETIME NULL AFTER partner_katilim_baslangic'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'otomatik_sona_ersin'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN otomatik_sona_ersin TINYINT(1) NOT NULL DEFAULT 1 AFTER partner_katilim_bitis'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'siralama'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN siralama INT NOT NULL DEFAULT 0 AFTER one_cikan_kampanya'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'aktif_sayfa_vitrini'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN aktif_sayfa_vitrini TINYINT(1) NOT NULL DEFAULT 0 AFTER siralama'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND COLUMN_NAME = 'gosterim_adedi'),
    'SELECT 1',
    'ALTER TABLE kampanyalar ADD COLUMN gosterim_adedi INT UNSIGNED NOT NULL DEFAULT 0 AFTER kullanilan_adet'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND INDEX_NAME = 'uk_kampanyalar_seo_slug'),
    'SELECT 1',
    'CREATE UNIQUE INDEX uk_kampanyalar_seo_slug ON kampanyalar (seo_slug)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND INDEX_NAME = 'uk_kampanyalar_sayfa_url'),
    'SELECT 1',
    'CREATE UNIQUE INDEX uk_kampanyalar_sayfa_url ON kampanyalar (sayfa_url)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND INDEX_NAME = 'idx_kampanyalar_gorunurluk'),
    'SELECT 1',
    'CREATE INDEX idx_kampanyalar_gorunurluk ON kampanyalar (gorunurluk_durumu, aktif_mi, baslangic_tarihi, bitis_tarihi)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'kampanyalar' AND INDEX_NAME = 'idx_kampanyalar_partner_katilim'),
    'SELECT 1',
    'CREATE INDEX idx_kampanyalar_partner_katilim ON kampanyalar (partner_katilim_acik, partner_katilim_baslangic, partner_katilim_bitis)'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
