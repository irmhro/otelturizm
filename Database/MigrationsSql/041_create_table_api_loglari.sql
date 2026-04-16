CREATE TABLE api_loglari (
    id BIGINT  NOT NULL IDENTITY(1,1),
    request_id VARCHAR(36) NOT NULL,
    
    -- API Bilgileri
    api_versiyonu VARCHAR(10) NULL,
    endpoint VARCHAR(500) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    
    -- İstek
    request_headers JSON NULL,
    request_body JSON NULL,
    request_ip VARCHAR(45) NULL,
    user_agent NVARCHAR(MAX) NULL,
    
    -- Yanıt
    response_status SMALLINT  NULL,
    response_headers JSON NULL,
    response_body JSON NULL,
    response_size INT  NULL,
    
    -- Kimlik
    kullanici_id BIGINT  NULL,
    api_key_id INT  NULL,
    partner_id BIGINT  NULL,
    
    -- Performans
    islem_suresi_ms INT  NULL,
    bellek_kullanimi_kb INT  NULL,
    
    -- Durum
    basarili_mi BIT DEFAULT 1,
    hata_mesaji NVARCHAR(MAX) NULL,
    hata_kodu VARCHAR(20) NULL,
    
    baslangic_tarihi DATETIME2 DEFAULT GETDATE(),
    bitis_tarihi DATETIME2 NULL,
    
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
);

