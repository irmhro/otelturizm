CREATE TABLE api_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    request_id VARCHAR(36) NOT NULL,
    
    -- API Bilgileri
    api_versiyonu VARCHAR(10) NULL,
    endpoint VARCHAR(500) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    
    -- İstek
    request_headers JSON NULL,
    request_body JSON NULL,
    request_ip VARCHAR(45) NULL,
    user_agent TEXT NULL,
    
    -- Yanıt
    response_status SMALLINT UNSIGNED NULL,
    response_headers JSON NULL,
    response_body JSON NULL,
    response_size INT UNSIGNED NULL,
    
    -- Kimlik
    kullanici_id BIGINT UNSIGNED NULL,
    api_key_id INT UNSIGNED NULL,
    partner_id BIGINT UNSIGNED NULL,
    
    -- Performans
    islem_suresi_ms INT UNSIGNED NULL COMMENT 'Milisaniye',
    bellek_kullanimi_kb INT UNSIGNED NULL,
    
    -- Durum
    basarili_mi TINYINT(1) DEFAULT 1,
    hata_mesaji TEXT NULL,
    hata_kodu VARCHAR(20) NULL,
    
    baslangic_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    bitis_tarihi TIMESTAMP NULL,
    
    PRIMARY KEY (id),
    UNIQUE KEY uk_request_id (request_id, baslangic_tarihi),
    INDEX idx_endpoint (endpoint(255)),
    INDEX idx_method (http_method),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_partner_id (partner_id),
    INDEX idx_response_status (response_status),
    INDEX idx_basarili (basarili_mi),
    INDEX idx_sure (islem_suresi_ms),
    INDEX idx_tarih (baslangic_tarihi DESC),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm API çağrılarının loglanması - Günlük partition zorunlu';


