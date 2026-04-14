CREATE TABLE kullanicilar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ad_soyad VARCHAR(100) NOT NULL,
    eposta VARCHAR(100) NOT NULL UNIQUE,
    telefon VARCHAR(20) NULL UNIQUE,
    sifre VARCHAR(255) NOT NULL,
    profil_fotografi VARCHAR(255) NULL,
    email_dogrulama_tarihi TIMESTAMP NULL,
    telefon_dogrulama_tarihi TIMESTAMP NULL,
    son_giris_tarihi TIMESTAMP NULL,
    son_giris_ip VARCHAR(45) NULL,
    hesap_durumu TINYINT NOT NULL DEFAULT 1 COMMENT '1:Aktif, 0:Pasif, 2:Askıda, 3:Banlı',
    dil_tercihi VARCHAR(5) DEFAULT 'tr',
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_eposta (eposta),
    INDEX idx_telefon (telefon),
    INDEX idx_hesap_durumu (hesap_durumu)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm kullanıcıların ana tablosu';

