CREATE TABLE kullanici_aktivite_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    aktivite_turu ENUM(
        'Giriş', 'Çıkış', 'Başarısız Giriş', 'Şifre Değiştirme', 'Şifre Sıfırlama',
        'Profil Güncelleme', 'E-posta Doğrulama', 'Telefon Doğrulama',
        'Rezervasyon Oluşturma', 'Rezervasyon İptal', 'Rezervasyon Görüntüleme',
        'Ödeme Başlatma', 'Ödeme Tamamlama', 'Ödeme Başarısız',
        'Mesaj Gönderme', 'Mesaj Okuma',
        'Yorum Yazma', 'Yorum Düzenleme', 'Yorum Silme',
        'Otel Favorilere Ekleme', 'Otel Favorilerden Çıkarma',
        'Arama Yapma', 'Filtreleme',
        'Dosya Yükleme', 'Dosya Silme',
        'Hesap Silme Talebi', 'KVKK Onayı', 'KVKK Reddi'
    ) NOT NULL,
    
    aktivite_detayi JSON NULL COMMENT '{"otel_id": 123, "arama_kriterleri": {...}}',
    
    -- Cihaz ve Konum
    ip_adresi VARCHAR(45) NOT NULL,
    user_agent TEXT NULL,
    cihaz_turu ENUM('Mobil', 'Tablet', 'Masaüstü', 'Bot', 'Bilinmiyor') DEFAULT 'Bilinmiyor',
    isletim_sistemi VARCHAR(50) NULL,
    tarayici VARCHAR(50) NULL,
    ulke VARCHAR(50) NULL,
    sehir VARCHAR(50) NULL,
    
    -- Oturum
    session_id VARCHAR(100) NULL,
    
    -- Durum
    basarili_mi TINYINT(1) DEFAULT 1,
    hata_nedeni VARCHAR(255) NULL,
    
    olusma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_aktivite_turu (aktivite_turu),
    INDEX idx_ip (ip_adresi),
    INDEX idx_olusma_tarihi (olusma_tarihi DESC),
    INDEX idx_basarili (basarili_mi),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kullanıcıların tüm aktiviteleri - Aylık partition zorunlu';


