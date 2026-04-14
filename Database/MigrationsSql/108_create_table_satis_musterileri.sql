SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS satis_musterileri
(
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    musteri_kodu VARCHAR(24) NOT NULL,
    ad_soyad VARCHAR(120) NOT NULL,
    eposta VARCHAR(100) NULL,
    telefon VARCHAR(20) NULL,
    ulke VARCHAR(60) NULL,
    sehir VARCHAR(100) NULL,
    uyelik_seviyesi ENUM('Standart','Silver','Gold','Platinum') NOT NULL DEFAULT 'Standart',
    toplam_rezervasyon_sayisi INT UNSIGNED NOT NULL DEFAULT 0,
    toplam_harcama DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    son_rezervasyon_tarihi DATE NULL,
    son_talep_ozeti VARCHAR(255) NULL,
    pazarlama_izni TINYINT(1) NOT NULL DEFAULT 0,
    notlar TEXT NULL,
    olusturan_sales_user_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_satis_musterileri_kod (musteri_kodu),
    KEY idx_satis_musterileri_ad_tel (ad_soyad, telefon),
    KEY idx_satis_musterileri_eposta (eposta),
    KEY idx_satis_musterileri_olusturan (olusturan_sales_user_id),
    CONSTRAINT fk_satis_musterileri_user FOREIGN KEY (olusturan_sales_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
