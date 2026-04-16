CREATE TABLE sistem_hata_loglari (
    id BIGINT  NOT NULL IDENTITY(1,1),
    
    hata_seviyesi ENUM('DEBUG', 'INFO', 'NOTICE', 'WARNING', 'ERROR', 'CRITICAL', 'ALERT', 'EMERGENCY') NOT NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji NVARCHAR(MAX) NOT NULL,
    hata_detayi NVARCHAR(MAX) NULL,
    
    -- Kaynak
    dosya_yolu VARCHAR(500) NULL,
    satir_no INT  NULL,
    fonksiyon_adi VARCHAR(100) NULL,
    sinif_adi VARCHAR(100) NULL,
    
    -- İstek Bilgileri
    url VARCHAR(2000) NULL,
    http_method VARCHAR(10) NULL,
    ip_adresi VARCHAR(45) NULL,
    user_agent NVARCHAR(MAX) NULL,
    referer VARCHAR(2000) NULL,
    
    -- Kullanıcı
    kullanici_id BIGINT  NULL,
    session_id VARCHAR(100) NULL,
    request_id VARCHAR(36) NULL,
    
    -- Ek Veri
    request_verisi JSON NULL,
    response_verisi JSON NULL,
    ek_bilgiler JSON NULL,
    
    -- Zaman
    olusma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    -- Durum
    cozuldu_mu BIT DEFAULT 0,
    cozulme_tarihi DATETIME2 NULL,
    cozen_admin_id BIGINT  NULL,
    cozum_notu NVARCHAR(MAX) NULL,
    
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
);

