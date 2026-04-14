CREATE TABLE oda_gorselleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    
    gorsel_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NULL,
    
    baslik VARCHAR(200) NULL,
    aciklama TEXT NULL,
    
    kapak_fotografi_mi TINYINT(1) DEFAULT 0,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    
    boyut_kb INT UNSIGNED NULL,
    
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    onay_tarihi TIMESTAMP NULL,
    
    yukleyen_kullanici_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_oda_tip_id (oda_tip_id),
    INDEX idx_kapak (oda_tip_id, kapak_fotografi_mi),
    INDEX idx_onay (onay_durumu),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yukleyen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda tiplerine ait görseller';

