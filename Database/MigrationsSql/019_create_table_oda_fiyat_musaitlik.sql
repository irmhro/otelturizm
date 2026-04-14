CREATE TABLE oda_fiyat_musaitlik (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    
    tarih DATE NOT NULL,
    
    -- Fiyatlandırma
    gecelik_fiyat DECIMAL(10,2) NOT NULL,
    indirimli_fiyat DECIMAL(10,2) NULL,
    kampanya_id BIGINT UNSIGNED NULL COMMENT 'Uygulanan kampanya varsa',
    
    -- Müsaitlik
    toplam_oda_sayisi SMALLINT UNSIGNED NOT NULL COMMENT 'O gün satılabilir oda sayısı',
    satilan_oda_sayisi SMALLINT UNSIGNED DEFAULT 0 COMMENT 'Rezerve edilmiş oda sayısı',
    bloke_oda_sayisi SMALLINT UNSIGNED DEFAULT 0 COMMENT 'Bakım/arıza nedeniyle bloke',
    minimum_geceleme TINYINT UNSIGNED DEFAULT 1,
    maksimum_geceleme SMALLINT UNSIGNED DEFAULT 30,
    
    -- Kısıtlamalar
    kapali_satis TINYINT(1) DEFAULT 0 COMMENT 'Satışa kapalı gün',
    sadece_gunubirlik TINYINT(1) DEFAULT 0,
    
    -- Kurallar
    iptal_politikasi_override JSON NULL COMMENT 'Bu güne özel iptal koşulları',
    
    guncellenme_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (id),
    UNIQUE KEY uk_oda_tip_tarih (oda_tip_id, tarih),
    INDEX idx_tarih (tarih),
    INDEX idx_kampanya (kampanya_id),
    INDEX idx_musaitlik (oda_tip_id, tarih, toplam_oda_sayisi, satilan_oda_sayisi),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda bazlı günlük fiyat ve müsaitlik - Aylık partition önerilir';


