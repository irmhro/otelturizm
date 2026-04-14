CREATE TABLE otel_gorselleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    
    gorsel_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NULL,
    gorsel_turu ENUM('Dış Cephe', 'Lobi', 'Restoran', 'Havuz', 'Plaj', 'Oda', 'Banyo', 'Spor Salonu', 'SPA', 'Toplantı Odası', 'Genel Alan', 'Yemek', 'Manzara') NOT NULL,
    
    baslik VARCHAR(200) NULL,
    aciklama TEXT NULL,
    
    kapak_fotografi_mi TINYINT(1) DEFAULT 0,
    one_cikan TINYINT(1) DEFAULT 0,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    
    boyut_kb INT UNSIGNED NULL,
    genislik SMALLINT UNSIGNED NULL,
    yukseklik SMALLINT UNSIGNED NULL,
    
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    onay_tarihi TIMESTAMP NULL,
    
    yukleyen_kullanici_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_otel_id (otel_id),
    INDEX idx_gorsel_turu (gorsel_turu),
    INDEX idx_kapak (otel_id, kapak_fotografi_mi),
    INDEX idx_onay (onay_durumu),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yukleyen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellere ait detaylı görsel galerisi';

