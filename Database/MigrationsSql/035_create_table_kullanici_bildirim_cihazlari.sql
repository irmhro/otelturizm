CREATE TABLE kullanici_bildirim_cihazlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    cihaz_turu ENUM('iOS', 'Android', 'Web', 'Huawei') NOT NULL,
    cihaz_token VARCHAR(255) NOT NULL,
    cihaz_adi VARCHAR(100) NULL,
    cihaz_modeli VARCHAR(50) NULL,
    isletim_sistemi_surumu VARCHAR(20) NULL,
    uygulama_surumu VARCHAR(10) NULL,
    
    bildirim_izinleri JSON NULL COMMENT '{"rezervasyon": true, "kampanya": false, "mesaj": true}',
    
    son_kullanim_tarihi TIMESTAMP NULL,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    son_bildirim_tarihi TIMESTAMP NULL,
    
    UNIQUE KEY uk_kullanici_token (kullanici_id, cihaz_token),
    INDEX idx_token (cihaz_token),
    INDEX idx_cihaz_turu (cihaz_turu),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Push notification tokenları';

