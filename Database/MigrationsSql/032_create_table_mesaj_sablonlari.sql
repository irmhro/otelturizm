CREATE TABLE mesaj_sablonlari (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    sablon_kodu VARCHAR(30) NOT NULL UNIQUE,
    sablon_adi VARCHAR(100) NOT NULL,
    
    -- Sahiplik
    otel_id BIGINT UNSIGNED NULL COMMENT 'Otele özel şablon',
    sistem_geneli_mi TINYINT(1) DEFAULT 0,
    
    -- Şablon İçeriği
    kategori ENUM('Hoş Geldin', 'Rezervasyon Onayı', 'Ödeme Hatırlatma', 'Giriş Bilgileri', 'Teşekkür', 'Özel Teklif', 'İptal', 'Diğer') NOT NULL,
    konu_basligi VARCHAR(200) NOT NULL,
    mesaj_icerigi TEXT NOT NULL,
    
    -- Değişkenler
    kullanilabilir_degiskenler JSON NULL COMMENT '["{misafir_adi}", "{giris_tarihi}", "{otel_adi}"]',
    
    -- Dil
    dil VARCHAR(5) DEFAULT 'tr',
    
    aktif_mi TINYINT(1) DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_otel_id (otel_id),
    INDEX idx_kategori (kategori),
    INDEX idx_dil (dil),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Hızlı mesaj şablonları';

