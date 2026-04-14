CREATE TABLE oda_tipleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    
    oda_tip_kodu VARCHAR(30) NOT NULL COMMENT 'Otel içi benzersiz kod',
    oda_adi VARCHAR(100) NOT NULL COMMENT 'Standart Oda, Deluxe Oda, Aile Odası vb.',
    oda_kategorisi ENUM('Standart', 'Superior', 'Deluxe', 'Junior Suite', 'Suite', 'Executive Suite', 'Presidential Suite', 'Aile Odası', 'Engelli Odası', 'Villa') NOT NULL,
    
    -- Kapasite
    maksimum_kisi_sayisi TINYINT UNSIGNED NOT NULL,
    maksimum_yetiskin_sayisi TINYINT UNSIGNED NOT NULL,
    maksimum_cocuk_sayisi TINYINT UNSIGNED DEFAULT 0,
    
    -- Yatak Düzeni
    yatak_tipi ENUM('Tek Kişilik', 'Çift Kişilik', 'Queen Size', 'King Size', 'Super King Size', 'Ranza', 'Çekyat') NULL,
    yatak_sayisi TINYINT UNSIGNED NULL,
    ek_yatak_eklenebilir_mi TINYINT(1) DEFAULT 0,
    
    -- Oda Ölçüleri
    oda_metrekare SMALLINT UNSIGNED NULL,
    balkon_var_mi TINYINT(1) DEFAULT 0,
    balkon_metrekare SMALLINT UNSIGNED NULL,
    manzara_tipi ENUM('Yok', 'Deniz', 'Havuz', 'Bahçe', 'Dağ', 'Şehir', 'Göl', 'İç Avlu') DEFAULT 'Yok',
    
    -- Banyo
    ozel_banyo_var_mi TINYINT(1) DEFAULT 1,
    banyo_tipi ENUM('Duş', 'Küvet', 'Jakuzi', 'Duş ve Küvet') DEFAULT 'Duş',
    
    -- Fiyatlandırma
    standart_gecelik_fiyat DECIMAL(10,2) NOT NULL COMMENT 'Baz fiyat - takvimde değişebilir',
    haftasonu_fark_orani DECIMAL(5,2) DEFAULT 0.00 COMMENT 'Yüzde',
    cocuk_indirim_orani DECIMAL(5,2) DEFAULT 0.00 COMMENT 'Yüzde',
    bebek_ucretsiz_mi TINYINT(1) DEFAULT 1,
    bebek_yas_siniri TINYINT UNSIGNED DEFAULT 2,
    cocuk_yas_siniri TINYINT UNSIGNED DEFAULT 12,
    
    -- Stok Yönetimi
    toplam_oda_sayisi SMALLINT UNSIGNED NOT NULL COMMENT 'Bu tipten kaç oda var',
    overbooking_limit TINYINT UNSIGNED DEFAULT 0 COMMENT 'Kaç oda fazla satış yapılabilir',
    
    -- Görseller
    kapak_fotografi VARCHAR(255) NULL,
    galeri JSON NULL,
    
    -- Özellikler
    ozellikler JSON NULL COMMENT '{"klima": true, "minibar": true, "tv": "LCD"}',
    
    -- Durum
    aktif_mi TINYINT(1) DEFAULT 1,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    UNIQUE KEY uk_otel_oda_kodu (otel_id, oda_tip_kodu),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kategori (oda_kategorisi),
    INDEX idx_kapasite (maksimum_kisi_sayisi),
    INDEX idx_aktif (aktif_mi),
    INDEX idx_fiyat (standart_gecelik_fiyat),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellerin oda tipi tanımları - 10M+ veri için partition uygun';

