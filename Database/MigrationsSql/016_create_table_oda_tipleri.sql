CREATE TABLE oda_tipleri (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    otel_id BIGINT  NOT NULL,
    
    oda_tip_kodu VARCHAR(30) NOT NULL,
    oda_adi VARCHAR(100) NOT NULL,
    oda_kategorisi ENUM('Standart', 'Superior', 'Deluxe', 'Junior Suite', 'Suite', 'Executive Suite', 'Presidential Suite', 'Aile Odası', 'Engelli Odası', 'Villa') NOT NULL,
    
    -- Kapasite
    maksimum_kisi_sayisi TINYINT  NOT NULL,
    maksimum_yetiskin_sayisi TINYINT  NOT NULL,
    maksimum_cocuk_sayisi TINYINT  DEFAULT 0,
    
    -- Yatak Düzeni
    yatak_tipi ENUM('Tek Kişilik', 'Çift Kişilik', 'Queen Size', 'King Size', 'Super King Size', 'Ranza', 'Çekyat') NULL,
    yatak_sayisi TINYINT  NULL,
    ek_yatak_eklenebilir_mi BIT DEFAULT 0,
    
    -- Oda Ölçüleri
    oda_metrekare SMALLINT  NULL,
    balkon_var_mi BIT DEFAULT 0,
    balkon_metrekare SMALLINT  NULL,
    manzara_tipi ENUM('Yok', 'Deniz', 'Havuz', 'Bahçe', 'Dağ', 'Şehir', 'Göl', 'İç Avlu') DEFAULT 'Yok',
    
    -- Banyo
    ozel_banyo_var_mi BIT DEFAULT 1,
    banyo_tipi ENUM('Duş', 'Küvet', 'Jakuzi', 'Duş ve Küvet') DEFAULT 'Duş',
    
    -- Fiyatlandırma
    standart_gecelik_fiyat DECIMAL(10,2) NOT NULL,
    haftasonu_fark_orani DECIMAL(5,2) DEFAULT 0.00,
    cocuk_indirim_orani DECIMAL(5,2) DEFAULT 0.00,
    bebek_ucretsiz_mi BIT DEFAULT 1,
    bebek_yas_siniri TINYINT  DEFAULT 2,
    cocuk_yas_siniri TINYINT  DEFAULT 12,
    
    -- Stok Yönetimi
    toplam_oda_sayisi SMALLINT  NOT NULL,
    overbooking_limit TINYINT  DEFAULT 0,
    
    -- Görseller
    kapak_fotografi VARCHAR(255) NULL,
    galeri JSON NULL,
    
    -- Özellikler
    ozellikler JSON NULL,
    
    -- Durum
    aktif_mi BIT DEFAULT 1,
    siralama SMALLINT  DEFAULT 0,
    
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    
    UNIQUE KEY uk_otel_oda_kodu (otel_id, oda_tip_kodu),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kategori (oda_kategorisi),
    INDEX idx_kapasite (maksimum_kisi_sayisi),
    INDEX idx_aktif (aktif_mi),
    INDEX idx_fiyat (standart_gecelik_fiyat),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
);

