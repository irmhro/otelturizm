CREATE TABLE basarisiz_odeme_denemeleri (
    id BIGINT  NOT NULL IDENTITY(1,1),
    rezervasyon_id BIGINT  NOT NULL,
    kullanici_id BIGINT  NOT NULL,
    
    deneme_tarihi DATETIME2 DEFAULT GETDATE(),
    
    tutar DECIMAL(10,2) NOT NULL,
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    odeme_yontemi ENUM('Kredi Kartı', 'Banka Havalesi/EFT', 'Sanal POS', 'Dijital Cüzdan') NOT NULL,
    kart_tipi ENUM('Visa', 'Mastercard', 'American Express', 'Troy', 'UnionPay', 'Diğer') NULL,
    kart_numarasi_masked VARCHAR(20) NULL,
    
    odeme_saglayici ENUM('İyzico', 'PayTR', 'Stripe', 'Garanti POS', 'Yapı Kredi POS', 'İş Bankası POS') NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji VARCHAR(500) NOT NULL,
    hata_detayi NVARCHAR(MAX) NULL,
    
    uc_d_secure_durumu ENUM('Başarılı', 'Başarısız', 'Kullanılmadı') DEFAULT 'Kullanılmadı',
    
    ip_adresi VARCHAR(45) NULL,
    cihaz_bilgisi VARCHAR(255) NULL,
    
    cozuldu_mu BIT DEFAULT 0,
    cozulme_tarihi DATETIME2 NULL,
    
    PRIMARY KEY (id),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_hata_kodu (hata_kodu),
    INDEX idx_tarih (deneme_tarihi DESC),
    INDEX idx_cozuldu (cozuldu_mu),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE CASCADE,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
);

