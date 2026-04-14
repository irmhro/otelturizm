CREATE TABLE mesajlar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    konusma_id BIGINT UNSIGNED NOT NULL,
    
    -- Gönderici Bilgisi
    gonderen_turu ENUM('Misafir', 'Otel', 'Sistem', 'Destek') NOT NULL,
    gonderen_kullanici_id BIGINT UNSIGNED NULL,
    gonderen_otel_id BIGINT UNSIGNED NULL,
    
    -- Mesaj İçeriği
    mesaj_metni TEXT NOT NULL,
    mesaj_tipi ENUM('Metin', 'Resim', 'Dosya', 'Konum', 'Teklif', 'Sistem Bildirimi') DEFAULT 'Metin',
    
    -- Medya İçerikleri
    medya_urls JSON NULL COMMENT '["url1", "url2"]',
    medya_tipleri JSON NULL COMMENT '["image/jpeg", "application/pdf"]',
    
    -- Özel Teklif (Otelden misafire fiyat teklifi)
    ozel_teklif_var_mi TINYINT(1) DEFAULT 0,
    teklif_tutari DECIMAL(10,2) NULL,
    teklif_para_birimi VARCHAR(3) NULL,
    teklif_gecerlilik_suresi DATETIME NULL,
    teklif_durumu ENUM('Beklemede', 'Kabul Edildi', 'Reddedildi', 'Süresi Doldu') NULL,
    teklif_kabul_tarihi TIMESTAMP NULL,
    
    -- Okunma Bilgileri
    okundu_mu TINYINT(1) DEFAULT 0,
    okunma_tarihi TIMESTAMP NULL,
    
    -- Durum
    durum ENUM('Gönderildi', 'İletildi', 'Okundu', 'Silindi', 'Spam') DEFAULT 'Gönderildi',
    
    -- IP ve Cihaz
    ip_adresi VARCHAR(45) NULL,
    cihaz_bilgisi VARCHAR(255) NULL,
    
    -- Zaman Damgası
    gonderim_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    duzenlenme_tarihi TIMESTAMP NULL,
    silinme_tarihi TIMESTAMP NULL,
    
    -- İndeksler
    PRIMARY KEY (id),
    INDEX idx_konusma_id (konusma_id),
    INDEX idx_gonderen_kullanici (gonderen_kullanici_id),
    INDEX idx_gonderen_otel (gonderen_otel_id),
    INDEX idx_gonderim_tarihi (gonderim_tarihi DESC),
    INDEX idx_okundu (okundu_mu),
    INDEX idx_teklif_durumu (teklif_durumu),
    
    FOREIGN KEY (konusma_id) REFERENCES mesaj_konusmalari(id) ON DELETE CASCADE,
    FOREIGN KEY (gonderen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (gonderen_otel_id) REFERENCES oteller(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm mesajlar - Aylık partition zorunlu';


