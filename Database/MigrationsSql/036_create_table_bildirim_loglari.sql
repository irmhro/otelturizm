CREATE TABLE bildirim_loglari (
    id BIGINT  NOT NULL IDENTITY(1,1),
    kullanici_id BIGINT  NOT NULL,
    bildirim_sablon_id SMALLINT  NULL,
    
    tur ENUM('E-posta', 'SMS', 'Push Notification', 'Sistem İçi') NOT NULL,
    
    alici_eposta VARCHAR(100) NULL,
    alici_telefon VARCHAR(20) NULL,
    cihaz_token VARCHAR(255) NULL,
    
    konu VARCHAR(200) NULL,
    icerik NVARCHAR(MAX) NOT NULL,
    gonderilen_icerik NVARCHAR(MAX) NULL,
    
    durum ENUM('Beklemede', 'Gönderildi', 'İletildi', 'Okundu', 'Başarısız', 'İptal Edildi') DEFAULT 'Beklemede',
    
    saglayici ENUM('SMTP', 'SendGrid', 'Amazon SES', 'Netgsm', 'Telsam', 'Firebase', 'APNS', 'OneSignal') NULL,
    saglayici_mesaj_id VARCHAR(100) NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji VARCHAR(500) NULL,
    
    gonderme_denemesi TINYINT  DEFAULT 1,
    maksimum_deneme TINYINT  DEFAULT 3,
    
    gonderim_tarihi DATETIME2 NULL,
    okunma_tarihi DATETIME2 NULL,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    -- İlişkili Kayıt
    ilgili_tablo VARCHAR(50) NULL,
    ilgili_kayit_id BIGINT  NULL,
    
    PRIMARY KEY (id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_tur (tur),
    INDEX idx_durum (durum),
    INDEX idx_gonderim_tarihi (gonderim_tarihi DESC),
    INDEX idx_ilgili (ilgili_tablo, ilgili_kayit_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (bildirim_sablon_id) REFERENCES bildirim_sablonlari(id) ON DELETE SET NULL
);

