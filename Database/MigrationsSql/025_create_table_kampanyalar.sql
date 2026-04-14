CREATE TABLE kampanyalar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kampanya_kodu VARCHAR(50) NOT NULL UNIQUE,
    kampanya_adi VARCHAR(200) NOT NULL,
    kampanya_aciklamasi TEXT NULL,
    
    tur ENUM('Yüzde İndirim', 'Sabit İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel', 'Kupon Kodu') NOT NULL,
    
    indirim_orani DECIMAL(5,2) NULL COMMENT 'Yüzde indirim için',
    indirim_tutari DECIMAL(10,2) NULL COMMENT 'Sabit indirim için',
    maksimum_indirim_tutari DECIMAL(10,2) NULL,
    minimum_sepet_tutari DECIMAL(10,2) NULL,
    
    -- Hedefleme
    hedef_otel_turu ENUM('Tümü', 'Belirli Oteller', 'Belirli Şehirler', 'Belirli Bölgeler', 'Zincir Oteller') DEFAULT 'Tümü',
    hedef_otel_idleri JSON NULL COMMENT '[1, 5, 10, 15]',
    hedef_sehirler JSON NULL COMMENT '["Antalya", "Muğla"]',
    
    hedef_kullanici_turu ENUM('Tümü', 'Yeni Üye', 'Sadık Müşteri', 'Belirli Ülkeler') DEFAULT 'Tümü',
    minimum_gecmis_rezervasyon TINYINT UNSIGNED NULL COMMENT 'En az X rezervasyon yapmış olanlar',
    
    -- Tarih Aralığı
    baslangic_tarihi DATETIME NOT NULL,
    bitis_tarihi DATETIME NOT NULL,
    rezervasyon_tarih_araligi_baslangic DATE NULL COMMENT 'Sadece bu tarihler arası yapılan rezervasyonlar',
    rezervasyon_tarih_araligi_bitis DATE NULL,
    konaklama_tarih_araligi_baslangic DATE NULL COMMENT 'Sadece bu tarihler arası konaklamalar',
    konaklama_tarih_araligi_bitis DATE NULL,
    
    -- Konaklama Şartları
    minimum_geceleme TINYINT UNSIGNED DEFAULT 1,
    maksimum_geceleme SMALLINT UNSIGNED NULL,
    erken_rezervasyon_gun_sayisi SMALLINT UNSIGNED NULL COMMENT 'En az X gün önce rezervasyon',
    
    -- Kullanım Limitleri
    toplam_kullanim_limiti INT UNSIGNED NULL,
    kullanici_basina_limit TINYINT UNSIGNED DEFAULT 1,
    kullanilan_adet INT UNSIGNED DEFAULT 0,
    
    -- Durum
    aktif_mi TINYINT(1) DEFAULT 1,
    one_cikan_kampanya TINYINT(1) DEFAULT 0,
    banner_gorseli VARCHAR(255) NULL,
    
    olusturan_admin_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_kod (kampanya_kodu),
    INDEX idx_tarih (baslangic_tarihi, bitis_tarihi),
    INDEX idx_aktif (aktif_mi),
    INDEX idx_tur (tur),
    
    FOREIGN KEY (olusturan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Platform geneli kampanya ve indirim tanımları';

