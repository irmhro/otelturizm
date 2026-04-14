CREATE TABLE sistem_hata_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    
    hata_seviyesi ENUM('DEBUG', 'INFO', 'NOTICE', 'WARNING', 'ERROR', 'CRITICAL', 'ALERT', 'EMERGENCY') NOT NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji TEXT NOT NULL,
    hata_detayi LONGTEXT NULL COMMENT 'Stack trace, context',
    
    -- Kaynak
    dosya_yolu VARCHAR(500) NULL,
    satir_no INT UNSIGNED NULL,
    fonksiyon_adi VARCHAR(100) NULL,
    sinif_adi VARCHAR(100) NULL,
    
    -- İstek Bilgileri
    url VARCHAR(2000) NULL,
    http_method VARCHAR(10) NULL,
    ip_adresi VARCHAR(45) NULL,
    user_agent TEXT NULL,
    referer VARCHAR(2000) NULL,
    
    -- Kullanıcı
    kullanici_id BIGINT UNSIGNED NULL,
    session_id VARCHAR(100) NULL,
    request_id VARCHAR(36) NULL COMMENT 'İstek takibi için UUID',
    
    -- Ek Veri
    request_verisi JSON NULL COMMENT 'POST/GET parametreleri (hassas veriler maskelenmiş)',
    response_verisi JSON NULL,
    ek_bilgiler JSON NULL,
    
    -- Zaman
    olusma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Durum
    cozuldu_mu TINYINT(1) DEFAULT 0,
    cozulme_tarihi TIMESTAMP NULL,
    cozen_admin_id BIGINT UNSIGNED NULL,
    cozum_notu TEXT NULL,
    
    PRIMARY KEY (id),
    INDEX idx_hata_seviyesi (hata_seviyesi),
    INDEX idx_hata_kodu (hata_kodu),
    INDEX idx_url (url(255)),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_ip (ip_adresi),
    INDEX idx_olusma_tarihi (olusma_tarihi DESC),
    INDEX idx_request_id (request_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (cozen_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Sistem hataları ve exception logları - Günlük partition önerilir';


