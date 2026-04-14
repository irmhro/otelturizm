CREATE TABLE ozel_tarih_tanimlari (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    
    tur ENUM('Resmi Tatil', 'Dini Bayram', 'Özel Gün', 'Sezon Başlangıcı', 'Sezon Bitişi', 'Yılbaşı', 'Festival', 'Fuar') NOT NULL,
    ad VARCHAR(100) NOT NULL,
    
    baslangic_tarihi DATE NOT NULL,
    bitis_tarihi DATE NOT NULL,
    
    tekrar_eder_mi TINYINT(1) DEFAULT 0 COMMENT 'Her yıl aynı tarihte tekrarlar mı?',
    tekrar_kurali ENUM('Sabit Tarih', 'Ayın X. Günü', 'Hicri Takvim', 'Her Yıl Aynı Gün') NULL,
    
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    sehir VARCHAR(50) NULL COMMENT 'Sadece belirli bir şehir için geçerliyse',
    
    fiyat_carpani DECIMAL(4,2) DEFAULT 1.00 COMMENT 'Bu dönemde fiyatlar kaç katına çıkar?',
    minimum_geceleme_kurali TINYINT UNSIGNED NULL,
    
    aciklama VARCHAR(255) NULL,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    INDEX idx_tarih_aralik (baslangic_tarihi, bitis_tarihi),
    INDEX idx_tur (tur),
    INDEX idx_ulke (ulke),
    INDEX idx_sehir (sehir),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Özel gün, tatil ve sezon tanımları';

