CREATE TABLE sepet_blokajlari (
    id BIGINT  NOT NULL IDENTITY(1,1),
    blokaj_kodu VARCHAR(30) NOT NULL UNIQUE,
    
    -- İlişkiler
    otel_id BIGINT  NOT NULL,
    oda_tip_id BIGINT  NOT NULL,
    kullanici_id BIGINT  NULL,
    session_id VARCHAR(100) NOT NULL,
    
    -- Blokaj Detayları
    giris_tarihi DATE NOT NULL,
    cikis_tarihi DATE NOT NULL,
    oda_sayisi TINYINT  DEFAULT 1,
    yetiskin_sayisi TINYINT  NOT NULL,
    cocuk_sayisi TINYINT  DEFAULT 0,
    
    -- Fiyat Bilgisi (Blokaj anındaki)
    gecelik_fiyat DECIMAL(10,2) NOT NULL,
    toplam_tutar DECIMAL(10,2) NOT NULL,
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    -- Durum
    durum ENUM('Aktif', 'Ödemeye Geçildi', 'Süresi Doldu', 'İptal Edildi', 'Rezervasyona Dönüştü') DEFAULT 'Aktif',
    rezervasyon_id BIGINT  NULL,
    
    -- Süre
    blokaj_baslangic_tarihi DATETIME2 DEFAULT GETDATE(),
    blokaj_bitis_tarihi DATETIME2 NULL,
    sure_dakika SMALLINT  DEFAULT 15,
    
    -- Hatırlatma
    hatirlatma_gonderildi_mi BIT DEFAULT 0,
    hatirlatma_gonderilme_tarihi DATETIME2 NULL,
    
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
);

