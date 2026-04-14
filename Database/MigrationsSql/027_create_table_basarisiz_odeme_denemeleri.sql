CREATE TABLE basarisiz_odeme_denemeleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    rezervasyon_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    deneme_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    tutar DECIMAL(10,2) NOT NULL,
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    odeme_yontemi ENUM('Kredi Kartı', 'Banka Havalesi/EFT', 'Sanal POS', 'Dijital Cüzdan') NOT NULL,
    kart_tipi ENUM('Visa', 'Mastercard', 'American Express', 'Troy', 'UnionPay', 'Diğer') NULL,
    kart_numarasi_masked VARCHAR(20) NULL,
    
    odeme_saglayici ENUM('İyzico', 'PayTR', 'Stripe', 'Garanti POS', 'Yapı Kredi POS', 'İş Bankası POS') NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji VARCHAR(500) NOT NULL,
    hata_detayi TEXT NULL,
    
    uc_d_secure_durumu ENUM('Başarılı', 'Başarısız', 'Kullanılmadı') DEFAULT 'Kullanılmadı',
    
    ip_adresi VARCHAR(45) NULL,
    cihaz_bilgisi VARCHAR(255) NULL,
    
    cozuldu_mu TINYINT(1) DEFAULT 0 COMMENT 'Sonraki denemede başarılı oldu mu?',
    cozulme_tarihi TIMESTAMP NULL,
    
    PRIMARY KEY (id),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_hata_kodu (hata_kodu),
    INDEX idx_tarih (deneme_tarihi DESC),
    INDEX idx_cozuldu (cozuldu_mu),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE CASCADE,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Başarısız ödeme denemelerinin kaydı - Aylık partition önerilir';


