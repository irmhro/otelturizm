CREATE TABLE kullanici_bildirim_cihazlari (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    kullanici_id BIGINT  NOT NULL,
    
    cihaz_turu ENUM('iOS', 'Android', 'Web', 'Huawei') NOT NULL,
    cihaz_token VARCHAR(255) NOT NULL,
    cihaz_adi VARCHAR(100) NULL,
    cihaz_modeli VARCHAR(50) NULL,
    isletim_sistemi_surumu VARCHAR(20) NULL,
    uygulama_surumu VARCHAR(10) NULL,
    
    bildirim_izinleri JSON NULL,
    
    son_kullanim_tarihi DATETIME2 NULL,
    aktif_mi BIT DEFAULT 1,
    
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    son_bildirim_tarihi DATETIME2 NULL,
    
    UNIQUE KEY uk_kullanici_token (kullanici_id, cihaz_token),
    INDEX idx_token (cihaz_token),
    INDEX idx_cihaz_turu (cihaz_turu),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
);

