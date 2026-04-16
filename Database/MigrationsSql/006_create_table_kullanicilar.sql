CREATE TABLE kullanicilar (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ad_soyad VARCHAR(100) NOT NULL,
    eposta VARCHAR(100) NOT NULL UNIQUE,
    telefon VARCHAR(20) NULL UNIQUE,
    sifre VARCHAR(255) NOT NULL,
    profil_fotografi VARCHAR(255) NULL,
    email_dogrulama_tarihi DATETIME2 NULL,
    telefon_dogrulama_tarihi DATETIME2 NULL,
    son_giris_tarihi DATETIME2 NULL,
    son_giris_ip VARCHAR(45) NULL,
    hesap_durumu TINYINT NOT NULL DEFAULT 1,
    dil_tercihi VARCHAR(5) DEFAULT 'tr',
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    
    INDEX idx_eposta (eposta),
    INDEX idx_telefon (telefon),
    INDEX idx_hesap_durumu (hesap_durumu)
);

