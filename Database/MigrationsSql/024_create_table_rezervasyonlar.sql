CREATE TABLE rezervasyonlar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    rezervasyon_no VARCHAR(20) NOT NULL UNIQUE COMMENT 'Platform geneli benzersiz no',
    
    -- İlişkiler
    otel_id BIGINT UNSIGNED NOT NULL,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    -- Misafir Bilgileri
    misafir_ad_soyad VARCHAR(100) NOT NULL,
    misafir_eposta VARCHAR(100) NOT NULL,
    misafir_telefon VARCHAR(20) NOT NULL,
    misafir_ulke VARCHAR(50) NULL,
    misafir_notu TEXT NULL,
    
    -- Konaklama Detayları
    giris_tarihi DATE NOT NULL,
    cikis_tarihi DATE NOT NULL,
    gece_sayisi SMALLINT UNSIGNED GENERATED ALWAYS AS (DATEDIFF(cikis_tarihi, giris_tarihi)) STORED,
    
    yetiskin_sayisi TINYINT UNSIGNED NOT NULL,
    cocuk_sayisi TINYINT UNSIGNED DEFAULT 0,
    bebek_sayisi TINYINT UNSIGNED DEFAULT 0,
    cocuk_yaslari JSON NULL COMMENT '[5, 8, 12]',
    
    oda_sayisi TINYINT UNSIGNED DEFAULT 1,
    
    -- Fiyatlandırma (Finansal Kayıt)
    gecelik_fiyat DECIMAL(10,2) NOT NULL COMMENT 'Rezervasyon anındaki gecelik fiyat',
    toplam_oda_tutari DECIMAL(10,2) NOT NULL COMMENT 'gecelik_fiyat * gece_sayisi * oda_sayisi',
    
    ek_hizmet_tutari DECIMAL(10,2) DEFAULT 0.00,
    vergi_tutari DECIMAL(10,2) DEFAULT 0.00,
    indirim_tutari DECIMAL(10,2) DEFAULT 0.00,
    kupon_indirimi DECIMAL(10,2) DEFAULT 0.00,
    
    toplam_tutar DECIMAL(10,2) NOT NULL COMMENT 'Müşteriden tahsil edilecek net tutar',
    
    komisyon_orani DECIMAL(5,2) NOT NULL COMMENT 'Rezervasyon anındaki geçerli komisyon oranı',
    komisyon_tutari DECIMAL(10,2) GENERATED ALWAYS AS (toplam_tutar * komisyon_orani / 100) STORED,
    otele_odenecek_tutar DECIMAL(10,2) GENERATED ALWAYS AS (toplam_tutar - (toplam_tutar * komisyon_orani / 100)) STORED,
    
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    -- Ödeme Bilgileri
    odeme_durumu ENUM('Beklemede', 'Ön Ödeme Alındı', 'Tamamlandı', 'İade Edildi', 'Kısmi İade', 'Başarısız') DEFAULT 'Beklemede',
    odeme_yontemi ENUM('Kredi Kartı', 'Banka Havalesi', 'Kapıda Ödeme', 'Sanal POS') NULL,
    odeme_tarihi TIMESTAMP NULL,
    on_odeme_tutari DECIMAL(10,2) NULL,
    kalan_odeme_tutari DECIMAL(10,2) NULL,
    
    -- Rezervasyon Durumu
    durum ENUM('Onay Bekliyor', 'Onaylandı', 'İptal Edildi', 'No-Show', 'Tamamlandı', 'Değişiklik Bekliyor') DEFAULT 'Onay Bekliyor',
    iptal_tarihi TIMESTAMP NULL,
    iptal_nedeni VARCHAR(500) NULL,
    iptal_eden ENUM('Misafir', 'Otel', 'Platform') NULL,
    iptal_kesintisi DECIMAL(10,2) NULL,
    iade_tutari DECIMAL(10,2) NULL,
    
    -- Otel Onayı
    otel_onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    otel_onay_tarihi TIMESTAMP NULL,
    otel_red_nedeni VARCHAR(500) NULL,
    
    -- Özel İstekler
    erken_giris_talebi TINYINT(1) DEFAULT 0,
    gec_cikis_talebi TINYINT(1) DEFAULT 0,
    transfer_talebi TINYINT(1) DEFAULT 0,
    ozel_istekler TEXT NULL,
    
    -- Kaynak / Kanal
    kaynak ENUM('Web', 'Mobil App', 'Telefon', 'Acente', 'Kurumsal') DEFAULT 'Web',
    kampanya_kodu VARCHAR(50) NULL,
    referans_kodu VARCHAR(50) NULL,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    check_in_yapildi_mi TINYINT(1) DEFAULT 0,
    check_in_tarihi TIMESTAMP NULL,
    check_out_yapildi_mi TINYINT(1) DEFAULT 0,
    check_out_tarihi TIMESTAMP NULL,
    
    -- İndeksler ve Partition
    PRIMARY KEY (id),
    UNIQUE KEY uk_rezervasyon_no (rezervasyon_no, giris_tarihi),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_giris_tarihi (giris_tarihi),
    INDEX idx_durum (durum),
    INDEX idx_odeme_durumu (odeme_durumu),
    INDEX idx_otel_tarih (otel_id, giris_tarihi),
    INDEX idx_olusturulma (olusturulma_tarihi DESC),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE RESTRICT,
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE RESTRICT,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm rezervasyonlar - Aylık partition zorunlu';


