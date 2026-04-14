CREATE TABLE bildirim_sablonlari (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    sablon_kodu VARCHAR(50) NOT NULL UNIQUE,
    sablon_adi VARCHAR(100) NOT NULL,
    
    tur ENUM('E-posta', 'SMS', 'Push Notification', 'Sistem İçi') NOT NULL,
    
    -- Çoklu Dil Desteği
    dil VARCHAR(5) NOT NULL DEFAULT 'tr',
    
    konu VARCHAR(200) NULL COMMENT 'E-posta konusu',
    baslik VARCHAR(100) NULL COMMENT 'Push bildirim başlığı',
    icerik TEXT NOT NULL,
    
    -- Değişkenler
    degiskenler JSON NULL COMMENT '["{ad_soyad}", "{rezervasyon_no}", "{otel_adi}"]',
    
    aktif_mi TINYINT(1) DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE KEY uk_kod_dil (sablon_kodu, dil),
    INDEX idx_tur (tur),
    INDEX idx_dil (dil),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm bildirim şablonları';

