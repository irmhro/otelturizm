CREATE TABLE odeme_islemleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    islem_no VARCHAR(30) NOT NULL UNIQUE COMMENT 'Platform geneli benzersiz işlem numarası',
    
    -- İlişkiler
    rezervasyon_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL COMMENT 'Ödemeyi yapan kullanıcı',
    otel_id BIGINT UNSIGNED NOT NULL,
    
    -- Ödeme Detayları
    odeme_turu ENUM('Ön Ödeme', 'Kalan Ödeme', 'Tam Ödeme', 'İade', 'Kısmi İade', 'Komisyon Kesintisi', 'Taksit') NOT NULL,
    odeme_yontemi ENUM('Kredi Kartı', 'Banka Havalesi/EFT', 'Sanal POS', 'Kapıda Ödeme', 'Hediye Kartı', 'Puan Kullanımı', 'Hızlı Havale', 'Dijital Cüzdan') NOT NULL,
    odeme_durumu ENUM('Beklemede', 'İşleniyor', 'Başarılı', 'Başarısız', 'İptal Edildi', 'Geri Ödendi', 'Kısmi Geri Ödendi', 'Askıda') DEFAULT 'Beklemede',
    
    -- Tutar Bilgileri
    tutar DECIMAL(10,2) NOT NULL COMMENT 'İşlem tutarı',
    komisyon_tutari DECIMAL(10,2) DEFAULT 0.00 COMMENT 'Platform komisyonu',
    vergi_tutari DECIMAL(10,2) DEFAULT 0.00 COMMENT 'KDV vb.',
    toplam_tahsilat DECIMAL(10,2) NOT NULL COMMENT 'Müşteriden çekilen toplam tutar',
    
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    kur_orani DECIMAL(10,6) DEFAULT 1.000000 COMMENT 'Yabancı para biriminden çevrim için',
    orijinal_tutar DECIMAL(10,2) NULL COMMENT 'Yabancı para birimindeki tutar',
    orijinal_para_birimi VARCHAR(3) NULL,
    
    -- Taksit Bilgileri (Taksitli ödeme için)
    taksit_sayisi TINYINT UNSIGNED DEFAULT 1,
    taksit_sirasi TINYINT UNSIGNED DEFAULT 1,
    ana_odeme_id BIGINT UNSIGNED NULL COMMENT 'Taksitli ödemenin ana kaydı',
    
    -- Kart / Banka Bilgileri (Maskelenmiş)
    kart_sahibi_adi VARCHAR(100) NULL,
    kart_numarasi_masked VARCHAR(20) NULL COMMENT '**** **** **** 1234',
    kart_tipi ENUM('Visa', 'Mastercard', 'American Express', 'Troy', 'UnionPay', 'Diğer') NULL,
    kart_son_kullanma VARCHAR(5) NULL COMMENT 'MM/YY',
    banka_adi VARCHAR(100) NULL,
    iban_masked VARCHAR(30) NULL COMMENT 'TR*****1234',
    
    -- Sanal POS / Ödeme Sağlayıcı
    odeme_saglayici ENUM('İyzico', 'PayTR', 'Stripe', 'PayPal', 'Garanti POS', 'Yapı Kredi POS', 'İş Bankası POS', 'Akbank POS', 'Halkbank POS', 'Vakıfbank POS') NULL,
    saglayici_islem_no VARCHAR(100) NULL,
    saglayici_onay_kodu VARCHAR(50) NULL,
    saglayici_hata_kodu VARCHAR(20) NULL,
    saglayici_hata_mesaji VARCHAR(500) NULL,
    
    -- 3D Secure
    uc_d_secure_kullanildi TINYINT(1) DEFAULT 0,
    uc_d_secure_durumu ENUM('Başarılı', 'Başarısız', 'Kullanılmadı') DEFAULT 'Kullanılmadı',
    
    -- İade Bilgileri
    iade_edilebilir_tutar DECIMAL(10,2) GENERATED ALWAYS AS (tutar - COALESCE(iade_edilen_tutar, 0)) STORED,
    iade_edilen_tutar DECIMAL(10,2) DEFAULT 0.00,
    iade_nedeni ENUM('İptal', 'Değişiklik', 'Mükerrer Ödeme', 'Anlaşmazlık', 'Diğer') NULL,
    iade_aciklamasi TEXT NULL,
    iade_tarihi TIMESTAMP NULL,
    iade_eden_admin_id BIGINT UNSIGNED NULL,
    
    -- Kesinti / Ceza
    iptal_kesintisi_orani DECIMAL(5,2) NULL,
    iptal_kesintisi_tutari DECIMAL(10,2) NULL,
    
    -- Fatura İlişkisi
    fatura_id BIGINT UNSIGNED NULL,
    
    -- IP ve Cihaz Bilgileri (Güvenlik)
    odeme_ip_adresi VARCHAR(45) NULL,
    odeme_cihaz_bilgisi VARCHAR(255) NULL,
    odeme_konum VARCHAR(100) NULL,
    
    -- Risk Değerlendirmesi
    risk_puani TINYINT UNSIGNED DEFAULT 0 COMMENT '0-100 arası',
    risk_kontrolu_sonucu ENUM('Düşük', 'Orta', 'Yüksek', 'İnceleniyor') DEFAULT 'İnceleniyor',
    manuel_onay_gerektirir TINYINT(1) DEFAULT 0,
    manuel_onaylayan_admin_id BIGINT UNSIGNED NULL,
    manuel_onay_tarihi TIMESTAMP NULL,
    
    -- Zaman Damgaları
    odeme_baslangic_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    odeme_tamamlanma_tarihi TIMESTAMP NULL,
    son_durum_degisikligi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- İndeksler
    PRIMARY KEY (id),
    UNIQUE KEY uk_islem_no (islem_no, odeme_baslangic_tarihi),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_odeme_durumu (odeme_durumu),
    INDEX idx_odeme_turu (odeme_turu),
    INDEX idx_tarih (odeme_baslangic_tarihi DESC),
    INDEX idx_saglayici_islem (saglayici_islem_no),
    INDEX idx_risk (risk_puani, manuel_onay_gerektirir),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE RESTRICT,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE RESTRICT,
    FOREIGN KEY (ana_odeme_id) REFERENCES odeme_islemleri(id) ON DELETE SET NULL,
    FOREIGN KEY (iade_eden_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (manuel_onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm ödeme işlemleri - Aylık partition zorunlu';


