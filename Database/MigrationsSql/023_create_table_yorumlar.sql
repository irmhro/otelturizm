CREATE TABLE yorumlar (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    otel_id BIGINT  NOT NULL,
    kullanici_id BIGINT  NOT NULL,
    rezervasyon_id BIGINT  NULL,
    
    -- Puanlamalar (1-5 arası)
    genel_puan TINYINT  NOT NULL CHECK (genel_puan BETWEEN 1 AND 5),
    temizlik_puani TINYINT  NOT NULL CHECK (temizlik_puani BETWEEN 1 AND 5),
    konfor_puani TINYINT  NOT NULL CHECK (konfor_puani BETWEEN 1 AND 5),
    konum_puani TINYINT  NOT NULL CHECK (konum_puani BETWEEN 1 AND 5),
    personel_puani TINYINT  NOT NULL CHECK (personel_puani BETWEEN 1 AND 5),
    fiyat_performans_puani TINYINT  NOT NULL CHECK (fiyat_performans_puani BETWEEN 1 AND 5),
    
    -- Yorum İçeriği
    yorum_basligi VARCHAR(200) NULL,
    yorum_metni NVARCHAR(MAX) NOT NULL,
    olumlu_yanlar NVARCHAR(MAX) NULL,
    olumsuz_yanlar NVARCHAR(MAX) NULL,
    
    -- Konaklama Detayları
    konaklama_tarihi DATE NULL,
    konaklama_turu ENUM('İş', 'Çift', 'Aile', 'Arkadaş Grubu', 'Yalnız') NULL,
    kaldigi_oda_tipi VARCHAR(100) NULL,
    gece_sayisi TINYINT  NULL,
    
    -- Doğrulama ve Onay
    dogrulanmis_konaklama BIT DEFAULT 0,
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi', 'İnceleniyor') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT  NULL,
    onay_tarihi DATETIME2 NULL,
    red_nedeni VARCHAR(500) NULL,
    
    -- Etkileşim
    faydali_oy_sayisi INT  DEFAULT 0,
    faydasiz_oy_sayisi INT  DEFAULT 0,
    rapor_sayisi SMALLINT  DEFAULT 0,
    
    -- Otel Yanıtı
    otel_yaniti NVARCHAR(MAX) NULL,
    otel_yaniti_tarihi DATETIME2 NULL,
    yanitlayan_kullanici_id BIGINT  NULL,
    
    -- Görseller
    yorum_gorselleri JSON NULL,
    
    -- Anonim
    anonim_mi BIT DEFAULT 0,
    
    -- Zaman Damgaları
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    
    -- İndeksler
    UNIQUE KEY uk_kullanici_otel_rezervasyon (kullanici_id, otel_id, rezervasyon_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_genel_puan (genel_puan DESC),
    INDEX idx_onay_durumu (onay_durumu),
    INDEX idx_olusturulma (olusturulma_tarihi DESC),
    INDEX idx_dogrulanmis (dogrulanmis_konaklama),
    INDEX idx_otel_puan (otel_id, genel_puan),
    
    -- Foreign Keys
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yanitlayan_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
);

