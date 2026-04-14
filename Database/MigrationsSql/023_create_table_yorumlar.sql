CREATE TABLE yorumlar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    rezervasyon_id BIGINT UNSIGNED NULL COMMENT 'Yorumun hangi rezervasyona ait olduğu (doğrulama için)',
    
    -- Puanlamalar (1-5 arası)
    genel_puan TINYINT UNSIGNED NOT NULL CHECK (genel_puan BETWEEN 1 AND 5),
    temizlik_puani TINYINT UNSIGNED NOT NULL CHECK (temizlik_puani BETWEEN 1 AND 5),
    konfor_puani TINYINT UNSIGNED NOT NULL CHECK (konfor_puani BETWEEN 1 AND 5),
    konum_puani TINYINT UNSIGNED NOT NULL CHECK (konum_puani BETWEEN 1 AND 5),
    personel_puani TINYINT UNSIGNED NOT NULL CHECK (personel_puani BETWEEN 1 AND 5),
    fiyat_performans_puani TINYINT UNSIGNED NOT NULL CHECK (fiyat_performans_puani BETWEEN 1 AND 5),
    
    -- Yorum İçeriği
    yorum_basligi VARCHAR(200) NULL,
    yorum_metni TEXT NOT NULL,
    olumlu_yanlar TEXT NULL,
    olumsuz_yanlar TEXT NULL,
    
    -- Konaklama Detayları
    konaklama_tarihi DATE NULL,
    konaklama_turu ENUM('İş', 'Çift', 'Aile', 'Arkadaş Grubu', 'Yalnız') NULL,
    kaldigi_oda_tipi VARCHAR(100) NULL,
    gece_sayisi TINYINT UNSIGNED NULL,
    
    -- Doğrulama ve Onay
    dogrulanmis_konaklama TINYINT(1) DEFAULT 0 COMMENT 'Rezervasyon ile eşleşti mi?',
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi', 'İnceleniyor') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    onay_tarihi TIMESTAMP NULL,
    red_nedeni VARCHAR(500) NULL,
    
    -- Etkileşim
    faydali_oy_sayisi INT UNSIGNED DEFAULT 0,
    faydasiz_oy_sayisi INT UNSIGNED DEFAULT 0,
    rapor_sayisi SMALLINT UNSIGNED DEFAULT 0,
    
    -- Otel Yanıtı
    otel_yaniti TEXT NULL,
    otel_yaniti_tarihi TIMESTAMP NULL,
    yanitlayan_kullanici_id BIGINT UNSIGNED NULL,
    
    -- Görseller
    yorum_gorselleri JSON NULL COMMENT '["url1", "url2"]',
    
    -- Anonim
    anonim_mi TINYINT(1) DEFAULT 0,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- İndeksler
    UNIQUE KEY uk_kullanici_otel_rezervasyon (kullanici_id, otel_id, rezervasyon_id) COMMENT 'Aynı rezervasyona bir kez yorum',
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellere yapılan kullanıcı değerlendirmeleri - 10M+ yorum için partition uygun';

