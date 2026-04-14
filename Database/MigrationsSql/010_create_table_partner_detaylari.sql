CREATE TABLE partner_detaylari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kullanici_id BIGINT UNSIGNED NOT NULL UNIQUE COMMENT 'Her partnerin bir kullanıcı hesabı olmalı',
    
    -- Firma Bilgileri
    firma_unvani VARCHAR(200) NOT NULL COMMENT 'Ticaret sicilindeki tam unvan',
    firma_turu ENUM('Anonim Şirketi', 'Limited Şirketi', 'Şahıs Firması', 'Adi Ortaklık', 'Vakıf', 'Dernek') NOT NULL,
    ticaret_sicil_no VARCHAR(50) NULL,
    ticaret_odasi VARCHAR(100) NULL,
    kurulus_yili YEAR NULL,
    
    -- Vergi Bilgileri
    vergi_dairesi VARCHAR(100) NOT NULL,
    vergi_numarasi VARCHAR(20) NOT NULL UNIQUE,
    tc_kimlik_no VARCHAR(11) NULL COMMENT 'Şahıs firması için zorunlu',
    
    -- İletişim ve Adres
    fatura_adresi TEXT NOT NULL,
    fatura_il VARCHAR(50) NOT NULL,
    fatura_ilce VARCHAR(50) NOT NULL,
    fatura_posta_kodu VARCHAR(10) NULL,
    
    -- Yetkili Kişi Bilgileri
    yetkili_ad_soyad VARCHAR(100) NOT NULL,
    yetkili_tc_no VARCHAR(11) NOT NULL,
    yetkili_telefon VARCHAR(20) NOT NULL,
    yetkili_eposta VARCHAR(100) NOT NULL,
    yetkili_gorev VARCHAR(100) NULL COMMENT 'Genel Müdür, Sahibi, Satış Müdürü vb.',
    
    -- Banka Bilgileri (Komisyon Ödemeleri İçin)
    banka_adi VARCHAR(100) NOT NULL,
    banka_subesi VARCHAR(100) NULL,
    iban VARCHAR(26) NOT NULL UNIQUE,
    hesap_sahibi_adi VARCHAR(150) NOT NULL,
    hesap_para_birimi ENUM('TRY', 'USD', 'EUR') DEFAULT 'TRY',
    
    -- Sözleşme Bilgileri
    sozlesme_no VARCHAR(50) NULL UNIQUE,
    sozlesme_baslangic_tarihi DATE NULL,
    sozlesme_bitis_tarihi DATE NULL,
    sozlesme_pdf_yolu VARCHAR(255) NULL,
    
    -- Onay ve Durum
    onay_durumu ENUM('Beklemede', 'Onaylandi', 'Reddedildi', 'Askida', 'Kara Liste') DEFAULT 'Beklemede',
    onay_tarihi TIMESTAMP NULL,
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    red_nedeni VARCHAR(500) NULL,
    
    -- Ek Bilgiler
    web_sitesi VARCHAR(255) NULL,
    logo_yolu VARCHAR(255) NULL,
    aciklama TEXT NULL,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- İndeksler (10M+ veri için kritik)
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_vergi_numarasi (vergi_numarasi),
    INDEX idx_onay_durumu (onay_durumu),
    INDEX idx_iban (iban),
    INDEX idx_olusturulma (olusturulma_tarihi),
    
    -- Foreign Key
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Platforma otel tanımlayan işletme sahipleri/yetkilileri';

