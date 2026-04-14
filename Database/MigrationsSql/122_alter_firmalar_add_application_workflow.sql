SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'yetkili_unvani') = 0,
    'ALTER TABLE firmalar ADD COLUMN yetkili_unvani varchar(100) NULL AFTER yetkili_ad_soyad',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'basvuru_tarihi') = 0,
    'ALTER TABLE firmalar ADD COLUMN basvuru_tarihi timestamp NULL DEFAULT CURRENT_TIMESTAMP AFTER onay_durumu',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'onay_sureci_baslama_tarihi') = 0,
    'ALTER TABLE firmalar ADD COLUMN onay_sureci_baslama_tarihi timestamp NULL DEFAULT NULL AFTER basvuru_tarihi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'onay_tarihi') = 0,
    'ALTER TABLE firmalar ADD COLUMN onay_tarihi timestamp NULL DEFAULT NULL AFTER onay_sureci_baslama_tarihi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'reddedilme_tarihi') = 0,
    'ALTER TABLE firmalar ADD COLUMN reddedilme_tarihi timestamp NULL DEFAULT NULL AFTER onay_tarihi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'onaylayan_kullanici_id') = 0,
    'ALTER TABLE firmalar ADD COLUMN onaylayan_kullanici_id bigint unsigned NULL AFTER reddedilme_tarihi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'onay_notu') = 0,
    'ALTER TABLE firmalar ADD COLUMN onay_notu text NULL AFTER onaylayan_kullanici_id',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'giris_izni_aktif_mi') = 0,
    'ALTER TABLE firmalar ADD COLUMN giris_izni_aktif_mi tinyint(1) NOT NULL DEFAULT 0 AFTER aktif_mi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'planlanan_onay_suresi_saat') = 0,
    'ALTER TABLE firmalar ADD COLUMN planlanan_onay_suresi_saat smallint unsigned NOT NULL DEFAULT 24 AFTER giris_izni_aktif_mi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'kayit_kaynagi') = 0,
    'ALTER TABLE firmalar ADD COLUMN kayit_kaynagi varchar(50) NOT NULL DEFAULT ''web_firma_register'' AFTER planlanan_onay_suresi_saat',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'sozlesme_onay_tarihi') = 0,
    'ALTER TABLE firmalar ADD COLUMN sozlesme_onay_tarihi timestamp NULL DEFAULT NULL AFTER kayit_kaynagi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND COLUMN_NAME = 'kvkk_onay_tarihi') = 0,
    'ALTER TABLE firmalar ADD COLUMN kvkk_onay_tarihi timestamp NULL DEFAULT NULL AFTER sozlesme_onay_tarihi',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND INDEX_NAME = 'idx_firmalar_access_state') = 0,
    'ALTER TABLE firmalar ADD INDEX idx_firmalar_access_state (onay_durumu, giris_izni_aktif_mi, aktif_mi)',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'firmalar' AND INDEX_NAME = 'idx_firmalar_yetkili_eposta') = 0,
    'ALTER TABLE firmalar ADD INDEX idx_firmalar_yetkili_eposta (yetkili_eposta)',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql = IF(
    (SELECT COUNT(*) FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() AND CONSTRAINT_NAME = 'fk_firmalar_onaylayan_kullanici') = 0,
    'ALTER TABLE firmalar ADD CONSTRAINT fk_firmalar_onaylayan_kullanici FOREIGN KEY (onaylayan_kullanici_id) REFERENCES users (id) ON DELETE SET NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE firmalar
SET
    basvuru_tarihi = COALESCE(basvuru_tarihi, olusturulma_tarihi, NOW()),
    onay_sureci_baslama_tarihi = CASE
        WHEN onay_durumu = 'Onaylandı' THEN COALESCE(onay_sureci_baslama_tarihi, olusturulma_tarihi, NOW())
        ELSE onay_sureci_baslama_tarihi
    END,
    onay_tarihi = CASE
        WHEN onay_durumu = 'Onaylandı' THEN COALESCE(onay_tarihi, guncellenme_tarihi, olusturulma_tarihi, NOW())
        ELSE onay_tarihi
    END,
    giris_izni_aktif_mi = CASE
        WHEN onay_durumu = 'Onaylandı' AND aktif_mi = 1 THEN 1
        ELSE giris_izni_aktif_mi
    END
WHERE 1 = 1;
