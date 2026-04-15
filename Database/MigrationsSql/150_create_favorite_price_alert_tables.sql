SET @db = DATABASE();

CREATE TABLE IF NOT EXISTS user_favorite_price_alerts (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    user_id BIGINT UNSIGNED NOT NULL,
    otel_id BIGINT UNSIGNED NOT NULL,
    hedef_maksimum_fiyat DECIMAL(12,2) NOT NULL,
    baslangic_tarihi DATE NOT NULL,
    bitis_tarihi DATE NOT NULL,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    son_tetiklenen_tarih DATETIME NULL,
    son_tetiklenen_fiyat DECIMAL(12,2) NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_user_hotel_alert (user_id, otel_id),
    KEY idx_alert_hotel_active_dates (otel_id, aktif_mi, baslangic_tarihi, bitis_tarihi, hedef_maksimum_fiyat),
    KEY idx_alert_user_active (user_id, aktif_mi),
    CONSTRAINT fk_alert_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_alert_hotel FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS user_favorite_price_alert_jobs (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    otel_id BIGINT UNSIGNED NOT NULL,
    tarih_baslangic DATE NOT NULL,
    tarih_bitis DATE NOT NULL,
    tetikleyen_kullanici_id BIGINT UNSIGNED NULL,
    durum VARCHAR(24) NOT NULL DEFAULT 'Pending',
    son_islenen_alert_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
    islenen_kayit_sayisi INT UNSIGNED NOT NULL DEFAULT 0,
    deneme_sayisi INT UNSIGNED NOT NULL DEFAULT 0,
    hata_mesaji VARCHAR(500) NULL,
    planli_calisma_tarihi DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_alert_job_status_plan (durum, planli_calisma_tarihi, id),
    KEY idx_alert_job_hotel_status (otel_id, durum, id),
    CONSTRAINT fk_alert_job_hotel FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET @sql = IF(
    EXISTS(SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'user_favorite_price_alert_jobs' AND COLUMN_NAME = 'son_islenen_alert_id'),
    'SELECT 1',
    'ALTER TABLE user_favorite_price_alert_jobs ADD COLUMN son_islenen_alert_id BIGINT UNSIGNED NOT NULL DEFAULT 0 AFTER durum'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
