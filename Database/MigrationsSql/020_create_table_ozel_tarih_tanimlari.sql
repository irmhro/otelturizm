CREATE TABLE ozel_tarih_tanimlari (
    id INT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    
    tur ENUM('Resmi Tatil', 'Dini Bayram', 'Özel Gün', 'Sezon Başlangıcı', 'Sezon Bitişi', 'Yılbaşı', 'Festival', 'Fuar') NOT NULL,
    ad VARCHAR(100) NOT NULL,
    
    baslangic_tarihi DATE NOT NULL,
    bitis_tarihi DATE NOT NULL,
    
    tekrar_eder_mi BIT DEFAULT 0,
    tekrar_kurali ENUM('Sabit Tarih', 'Ayın X. Günü', 'Hicri Takvim', 'Her Yıl Aynı Gün') NULL,
    
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    sehir VARCHAR(50) NULL,
    
    fiyat_carpani DECIMAL(4,2) DEFAULT 1.00,
    minimum_geceleme_kurali TINYINT  NULL,
    
    aciklama VARCHAR(255) NULL,
    aktif_mi BIT DEFAULT 1,
    
    INDEX idx_tarih_aralik (baslangic_tarihi, bitis_tarihi),
    INDEX idx_tur (tur),
    INDEX idx_ulke (ulke),
    INDEX idx_sehir (sehir),
    INDEX idx_aktif (aktif_mi)
);

