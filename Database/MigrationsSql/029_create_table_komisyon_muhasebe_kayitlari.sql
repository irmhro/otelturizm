CREATE TABLE komisyon_muhasebe_kayitlari (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    kayit_no VARCHAR(30) NOT NULL UNIQUE,
    kayit_tarihi DATE NOT NULL,
    donem VARCHAR(7) NOT NULL,
    
    -- İlişkiler
    rezervasyon_id BIGINT  NOT NULL,
    otel_id BIGINT  NOT NULL,
    partner_id BIGINT  NOT NULL,
    fatura_id BIGINT  NULL,
    
    -- Finansal Bilgiler
    toplam_rezervasyon_tutari DECIMAL(10,2) NOT NULL,
    komisyon_orani DECIMAL(5,2) NOT NULL,
    komisyon_tutari DECIMAL(10,2) NOT NULL,
    ek_kesintiler DECIMAL(10,2) DEFAULT 0.00,
    net_otele_odenecek DECIMAL(10,2) NOT NULL,
    
    -- Ödeme Durumu
    otele_odeme_durumu ENUM('Beklemede', 'Ödeme Emri Oluşturuldu', 'Ödendi', 'Mahsuplaşıldı', 'Askıda') DEFAULT 'Beklemede',
    otele_odeme_tarihi DATE NULL,
    otele_odeme_referansi VARCHAR(50) NULL,
    odeme_emri_no VARCHAR(30) NULL,
    
    -- Muhasebe Hesapları
    muhasebe_hesap_kodu VARCHAR(20) NULL,
    karsi_hesap_kodu VARCHAR(20) NULL,
    yevmiye_no VARCHAR(20) NULL,
    fis_no VARCHAR(20) NULL,
    
    -- Mutabakat
    mutabakat_durumu ENUM('Beklemede', 'Otele Gönderildi', 'Otel Onayladı', 'İtiraz Var', 'Çözüldü') DEFAULT 'Beklemede',
    mutabakat_gonderim_tarihi DATETIME2 NULL,
    mutabakat_onay_tarihi DATETIME2 NULL,
    mutabakat_notu NVARCHAR(MAX) NULL,
    
    -- İtiraz ve Düzeltme
    itiraz_var_mi BIT DEFAULT 0,
    itiraz_nedeni VARCHAR(500) NULL,
    itiraz_tarihi DATETIME2 NULL,
    itiraz_cozum_tarihi DATETIME2 NULL,
    itiraz_cozum_aciklamasi NVARCHAR(MAX) NULL,
    duzeltme_tutari DECIMAL(10,2) NULL,
    
    -- Vergi
    stopaj_orani DECIMAL(5,2) DEFAULT 0.00,
    stopaj_tutari DECIMAL(10,2) DEFAULT 0.00,
    kdv_orani DECIMAL(5,2) DEFAULT 20.00,
    kdv_tutari DECIMAL(10,2) DEFAULT 0.00,
    
    -- Zaman Damgaları
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    olusturan_admin_id BIGINT  NULL,
    onaylayan_finans_admin_id BIGINT  NULL,
    
    -- İndeksler
    INDEX idx_kayit_no (kayit_no),
    INDEX idx_donem (donem),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_partner_id (partner_id),
    INDEX idx_otele_odeme_durumu (otele_odeme_durumu),
    INDEX idx_mutabakat_durumu (mutabakat_durumu),
    INDEX idx_kayit_tarihi (kayit_tarihi DESC),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE RESTRICT,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE RESTRICT,
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE RESTRICT,
    FOREIGN KEY (fatura_id) REFERENCES faturalar(id) ON DELETE SET NULL,
    FOREIGN KEY (olusturan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (onaylayan_finans_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
);

