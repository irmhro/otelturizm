CREATE TABLE bildirim_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    bildirim_sablon_id SMALLINT UNSIGNED NULL,
    
    tur ENUM('E-posta', 'SMS', 'Push Notification', 'Sistem İçi') NOT NULL,
    
    alici_eposta VARCHAR(100) NULL,
    alici_telefon VARCHAR(20) NULL,
    cihaz_token VARCHAR(255) NULL,
    
    konu VARCHAR(200) NULL,
    icerik TEXT NOT NULL,
    gonderilen_icerik TEXT NULL COMMENT 'Değişkenler işlenmiş son hali',
    
    durum ENUM('Beklemede', 'Gönderildi', 'İletildi', 'Okundu', 'Başarısız', 'İptal Edildi') DEFAULT 'Beklemede',
    
    saglayici ENUM('SMTP', 'SendGrid', 'Amazon SES', 'Netgsm', 'Telsam', 'Firebase', 'APNS', 'OneSignal') NULL,
    saglayici_mesaj_id VARCHAR(100) NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji VARCHAR(500) NULL,
    
    gonderme_denemesi TINYINT UNSIGNED DEFAULT 1,
    maksimum_deneme TINYINT UNSIGNED DEFAULT 3,
    
    gonderim_tarihi TIMESTAMP NULL,
    okunma_tarihi TIMESTAMP NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- İlişkili Kayıt
    ilgili_tablo VARCHAR(50) NULL COMMENT 'rezervasyonlar, mesajlar',
    ilgili_kayit_id BIGINT UNSIGNED NULL,
    
    PRIMARY KEY (id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_tur (tur),
    INDEX idx_durum (durum),
    INDEX idx_gonderim_tarihi (gonderim_tarihi DESC),
    INDEX idx_ilgili (ilgili_tablo, ilgili_kayit_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (bildirim_sablon_id) REFERENCES bildirim_sablonlari(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm bildirim gönderim logları - Aylık partition zorunlu';


