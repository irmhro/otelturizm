CREATE TABLE faturalar (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    fatura_no VARCHAR(30) NOT NULL UNIQUE,
    fatura_tarihi DATE NOT NULL,
    fatura_turu ENUM('Satış Faturası', 'İade Faturası', 'Komisyon Faturası', 'Proforma', 'e-Fatura', 'e-Arşiv') NOT NULL,
    
    -- İlişkiler
    rezervasyon_id BIGINT  NULL,
    otel_id BIGINT  NULL,
    kullanici_id BIGINT  NULL,
    partner_id BIGINT  NULL,
    odeme_islem_id BIGINT  NULL,
    
    -- Fatura Bilgileri
    fatura_kesen ENUM('Platform', 'Otel') NOT NULL,
    fatura_kesen_unvan VARCHAR(200) NOT NULL,
    fatura_kesen_vergi_dairesi VARCHAR(100) NOT NULL,
    fatura_kesen_vergi_no VARCHAR(20) NOT NULL,
    fatura_kesen_adres NVARCHAR(MAX) NOT NULL,
    
    fatura_alici_unvan VARCHAR(200) NOT NULL,
    fatura_alici_vergi_dairesi VARCHAR(100) NULL,
    fatura_alici_vergi_no VARCHAR(20) NULL,
    fatura_alici_tc_no VARCHAR(11) NULL,
    fatura_alici_adres NVARCHAR(MAX) NOT NULL,
    fatura_alici_eposta VARCHAR(100) NULL,
    
    -- Tutar Bilgileri
    ara_toplam DECIMAL(10,2) NOT NULL,
    kdv_orani DECIMAL(5,2) DEFAULT 20.00,
    kdv_tutari DECIMAL(10,2) NOT NULL,
    diger_vergiler DECIMAL(10,2) DEFAULT 0.00,
    konaklama_vergisi_orani DECIMAL(5,2) DEFAULT 2.00,
    konaklama_vergisi_tutari DECIMAL(10,2) DEFAULT 0.00,
    genel_toplam DECIMAL(10,2) NOT NULL,
    
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    yalniz_yaziyla VARCHAR(500) NULL,
    
    -- e-Fatura / e-Arşiv Bilgileri
    e_fatura_uuid VARCHAR(36) NULL UNIQUE,
    e_fatura_durumu ENUM('Taslak', 'Oluşturuldu', 'Gönderildi', 'Onaylandı', 'Reddedildi', 'İptal Edildi') NULL,
    e_fatura_gonderim_tarihi DATETIME2 NULL,
    e_fatura_onay_tarihi DATETIME2 NULL,
    e_fatura_entegrasyon_turu ENUM('GİB Portal', 'Özel Entegratör', 'Doğrudan Entegrasyon') NULL,
    entegrator_adi VARCHAR(50) NULL,
    
    -- PDF / Görüntü
    fatura_pdf_yolu VARCHAR(500) NULL,
    fatura_html_yolu VARCHAR(500) NULL,
    fatura_xml_yolu VARCHAR(500) NULL,
    
    -- Durum
    fatura_durumu ENUM('Taslak', 'Kesildi', 'İptal Edildi', 'İade Faturası Kesildi') DEFAULT 'Kesildi',
    iptal_nedeni VARCHAR(500) NULL,
    iptal_tarihi DATETIME2 NULL,
    iptal_eden_admin_id BIGINT  NULL,
    
    -- Notlar
    fatura_notu NVARCHAR(MAX) NULL,
    siparis_no VARCHAR(50) NULL,
    
    -- Zaman Damgaları
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    vade_tarihi DATE NULL,
    odeme_tarihi DATE NULL,
    
    -- İndeksler
    INDEX idx_fatura_no (fatura_no),
    INDEX idx_fatura_tarihi (fatura_tarihi DESC),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_partner_id (partner_id),
    INDEX idx_fatura_turu (fatura_turu),
    INDEX idx_e_fatura_uuid (e_fatura_uuid),
    INDEX idx_durum (fatura_durumu),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE SET NULL,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE SET NULL,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE SET NULL,
    FOREIGN KEY (odeme_islem_id) REFERENCES odeme_islemleri(id) ON DELETE SET NULL,
    FOREIGN KEY (iptal_eden_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
);

