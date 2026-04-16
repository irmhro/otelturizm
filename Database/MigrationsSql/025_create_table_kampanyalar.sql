CREATE TABLE kampanyalar (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    kampanya_kodu VARCHAR(50) NOT NULL UNIQUE,
    kampanya_adi VARCHAR(200) NOT NULL,
    kampanya_aciklamasi NVARCHAR(MAX) NULL,
    
    tur ENUM('Yüzde İndirim', 'Sabit İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel', 'Kupon Kodu') NOT NULL,
    
    indirim_orani DECIMAL(5,2) NULL,
    indirim_tutari DECIMAL(10,2) NULL,
    maksimum_indirim_tutari DECIMAL(10,2) NULL,
    minimum_sepet_tutari DECIMAL(10,2) NULL,
    
    -- Hedefleme
    hedef_otel_turu ENUM('Tümü', 'Belirli Oteller', 'Belirli Şehirler', 'Belirli Bölgeler', 'Zincir Oteller') DEFAULT 'Tümü',
    hedef_otel_idleri JSON NULL,
    hedef_sehirler JSON NULL,
    
    hedef_kullanici_turu ENUM('Tümü', 'Yeni Üye', 'Sadık Müşteri', 'Belirli Ülkeler') DEFAULT 'Tümü',
    minimum_gecmis_rezervasyon TINYINT  NULL,
    
    -- Tarih Aralığı
    baslangic_tarihi DATETIME2 NOT NULL,
    bitis_tarihi DATETIME2 NOT NULL,
    rezervasyon_tarih_araligi_baslangic DATE NULL,
    rezervasyon_tarih_araligi_bitis DATE NULL,
    konaklama_tarih_araligi_baslangic DATE NULL,
    konaklama_tarih_araligi_bitis DATE NULL,
    
    -- Konaklama Şartları
    minimum_geceleme TINYINT  DEFAULT 1,
    maksimum_geceleme SMALLINT  NULL,
    erken_rezervasyon_gun_sayisi SMALLINT  NULL,
    
    -- Kullanım Limitleri
    toplam_kullanim_limiti INT  NULL,
    kullanici_basina_limit TINYINT  DEFAULT 1,
    kullanilan_adet INT  DEFAULT 0,
    
    -- Durum
    aktif_mi BIT DEFAULT 1,
    one_cikan_kampanya BIT DEFAULT 0,
    banner_gorseli VARCHAR(255) NULL,
    
    olusturan_admin_id BIGINT  NULL,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    
    INDEX idx_kod (kampanya_kodu),
    INDEX idx_tarih (baslangic_tarihi, bitis_tarihi),
    INDEX idx_aktif (aktif_mi),
    INDEX idx_tur (tur),
    
    FOREIGN KEY (olusturan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
);

