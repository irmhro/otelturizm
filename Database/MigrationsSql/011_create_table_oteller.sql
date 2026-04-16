CREATE TABLE oteller (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    otel_kodu VARCHAR(20) NOT NULL UNIQUE,
    partner_id BIGINT  NOT NULL,
    
    -- Temel Bilgiler
    otel_adi VARCHAR(255) NOT NULL,
    otel_turu ENUM('Otel', 'Butik Otel', 'Apart Otel', 'Villa', 'Pansiyon', 'Tatil Köyü', 'Hostel', 'Kamping', 'Apartman Dairesi') NOT NULL,
    yildiz_sayisi TINYINT  NULL,
    turizm_belge_no VARCHAR(50) NULL,
    turizm_belge_turu ENUM('Turizm İşletme Belgeli', 'Turizm Yatırım Belgeli', 'Belediye Belgeli', 'Basit Konaklama Belgeli', 'Belgesiz') NULL,
    
    -- Konum Bilgileri (Arama optimizasyonu için ayrı sütunlar)
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    sehir VARCHAR(50) NOT NULL,
    ilce VARCHAR(50) NOT NULL,
    mahalle VARCHAR(100) NULL,
    tam_adres NVARCHAR(MAX) NOT NULL,
    posta_kodu VARCHAR(10) NULL,
    enlem DECIMAL(10, 8) NULL,
    boylam DECIMAL(11, 8) NULL,
    
    -- Coğrafi Hiyerarşi ID'leri (Ayrı bir lokasyon tablosundan)
    ulke_id SMALLINT  NULL,
    sehir_id INT  NULL,
    ilce_id INT  NULL,
    bolge_id INT  NULL,
    
    -- İletişim Bilgileri
    telefon_1 VARCHAR(20) NOT NULL,
    telefon_2 VARCHAR(20) NULL,
    faks VARCHAR(20) NULL,
    eposta VARCHAR(100) NOT NULL,
    web_sitesi VARCHAR(255) NULL,
    
    -- Operasyonel Bilgiler
    check_in_saati TIME DEFAULT '14:00:00',
    check_out_saati TIME DEFAULT '12:00:00',
    gec_check_out_mumkun_mu BIT DEFAULT 0,
    gec_check_out_ucreti DECIMAL(10,2) NULL,
    erken_check_in_mumkun_mu BIT DEFAULT 0,
    erken_check_in_ucreti DECIMAL(10,2) NULL,
    
    toplam_oda_sayisi SMALLINT  NOT NULL,
    toplam_yatak_kapasitesi SMALLINT  NULL,
    kat_sayisi TINYINT  NULL,
    asansor_var_mi BIT DEFAULT 0,
    asansor_sayisi TINYINT  DEFAULT 0,
    
    -- Açıklamalar
    kisa_aciklama VARCHAR(500) NULL,
    uzun_aciklama NVARCHAR(MAX) NULL,
    konum_aciklamasi NVARCHAR(MAX) NULL,
    
    -- Finansal ve Komisyon Ayarları
    komisyon_turu ENUM('sabit_oran', 'oda_bazli', 'sezon_bazli', 'karma') DEFAULT 'sabit_oran',
    varsayilan_komisyon_orani DECIMAL(5,2) NOT NULL,
    komisyon_hesaplama_tipi ENUM('gecelik_fiyat_uzerinden', 'toplam_tutar_uzerinden', 'kira_bedeli_uzerinden') DEFAULT 'toplam_tutar_uzerinden',
    
    odeme_vadesi ENUM('Rezervasyon Anında', 'Giriş Günü', 'Çıkış Günü', 'Haftalık', 'Aylık', '15 Günde Bir') NOT NULL DEFAULT 'Çıkış Günü',
    odeme_yontemi ENUM('Havale/EFT', 'Sanal POS (Platform Tahsilat)', 'Otel Tahsilatı') NOT NULL DEFAULT 'Havale/EFT',
    fatura_kesim_turu ENUM('Platform Keser', 'Otel Keser') NOT NULL DEFAULT 'Otel Keser',
    
    depozito_tutari DECIMAL(10,2) NULL,
    depozito_iade_suresi TINYINT  NULL,
    
    minimum_konaklama_gecesi TINYINT  DEFAULT 1,
    maksimum_konaklama_gecesi SMALLINT  DEFAULT 30,
    
    -- Dil Seçenekleri
    konusulan_diller SET('Türkçe', 'İngilizce', 'Almanca', 'Rusça', 'Arapça', 'Fransızca', 'İspanyolca', 'İtalyanca') DEFAULT 'Türkçe',
    
    -- Puanlamalar (Cache - performans için)
    ortalama_puan DECIMAL(3,2) DEFAULT 0.00,
    toplam_yorum_sayisi INT  DEFAULT 0,
    temizlik_puani DECIMAL(3,2) DEFAULT 0.00,
    konfor_puani DECIMAL(3,2) DEFAULT 0.00,
    konum_puani DECIMAL(3,2) DEFAULT 0.00,
    personel_puani DECIMAL(3,2) DEFAULT 0.00,
    fiyat_performans_puani DECIMAL(3,2) DEFAULT 0.00,
    
    -- Görseller
    kapak_fotografi VARCHAR(255) NULL,
    galeri JSON NULL,
    video_url VARCHAR(255) NULL,
    sanal_tur_url VARCHAR(255) NULL,
    
    -- Durum Bilgileri
    yayin_durumu ENUM('Taslak', 'Yayında', 'Bakımda', 'Sezon Dışı', 'Kapatıldı', 'Askıda') DEFAULT 'Taslak',
    onay_durumu ENUM('Beklemede', 'İçerik Eksik', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onay_tarihi DATETIME2 NULL,
    onaylayan_admin_id BIGINT  NULL,
    
    populerlik_sirasi INT  DEFAULT 0,
    one_cikan_otel BIT DEFAULT 0,
    tavsiye_edilen_otel BIT DEFAULT 0,
    
    -- Zaman Damgaları
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    
    -- İndeksler (10M+ otel verisi için performans kritik)
    INDEX idx_otel_kodu (otel_kodu),
    INDEX idx_partner_id (partner_id),
    INDEX idx_sehir (sehir),
    INDEX idx_ilce (ilce),
    INDEX idx_sehir_ilce (sehir, ilce),
    INDEX idx_bolge_id (bolge_id),
    INDEX idx_yildiz_sayisi (yildiz_sayisi),
    INDEX idx_otel_turu (otel_turu),
    INDEX idx_yayin_durumu (yayin_durumu),
    INDEX idx_onay_durumu (onay_durumu),
    INDEX idx_populerlik (populerlik_sirasi DESC),
    INDEX idx_one_cikan (one_cikan_otel),
    INDEX idx_enlem_boylam (enlem, boylam),
    INDEX idx_ortalama_puan (ortalama_puan DESC),
    
    -- Foreign Keys
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE RESTRICT,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
);

