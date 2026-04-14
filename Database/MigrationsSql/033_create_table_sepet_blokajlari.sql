CREATE TABLE sepet_blokajlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    blokaj_kodu VARCHAR(30) NOT NULL UNIQUE,
    
    -- İlişkiler
    otel_id BIGINT UNSIGNED NOT NULL,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NULL COMMENT 'Giriş yapmamış kullanıcı için NULL olabilir',
    session_id VARCHAR(100) NOT NULL COMMENT 'PHP/Laravel session ID',
    
    -- Blokaj Detayları
    giris_tarihi DATE NOT NULL,
    cikis_tarihi DATE NOT NULL,
    oda_sayisi TINYINT UNSIGNED DEFAULT 1,
    yetiskin_sayisi TINYINT UNSIGNED NOT NULL,
    cocuk_sayisi TINYINT UNSIGNED DEFAULT 0,
    
    -- Fiyat Bilgisi (Blokaj anındaki)
    gecelik_fiyat DECIMAL(10,2) NOT NULL,
    toplam_tutar DECIMAL(10,2) NOT NULL,
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    -- Durum
    durum ENUM('Aktif', 'Ödemeye Geçildi', 'Süresi Doldu', 'İptal Edildi', 'Rezervasyona Dönüştü') DEFAULT 'Aktif',
    rezervasyon_id BIGINT UNSIGNED NULL COMMENT 'Dönüşen rezervasyon ID',
    
    -- Süre
    blokaj_baslangic_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    blokaj_bitis_tarihi TIMESTAMP NULL COMMENT 'Otomatik serbest kalma zamanı',
    sure_dakika SMALLINT UNSIGNED DEFAULT 15 COMMENT 'Blokaj süresi',
    
    -- Hatırlatma
    hatirlatma_gonderildi_mi TINYINT(1) DEFAULT 0,
    hatirlatma_gonderilme_tarihi TIMESTAMP NULL,
    
    -- IP
    ip_adresi VARCHAR(45) NULL,
    
    -- İndeksler
    PRIMARY KEY (id),
    INDEX idx_blokaj_kodu (blokaj_kodu),
    INDEX idx_session_id (session_id),
    INDEX idx_otel_oda_tarih (otel_id, oda_tip_id, giris_tarihi),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_durum (durum),
    INDEX idx_bitis_tarihi (blokaj_bitis_tarihi),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Ödeme öncesi geçici oda blokajları - Günlük partition önerilir';


