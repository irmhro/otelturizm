SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS satis_musterileri
(
    id BIGINT  NOT NULL IDENTITY(1,1),
    musteri_kodu VARCHAR(24) NOT NULL,
    ad_soyad VARCHAR(120) NOT NULL,
    eposta VARCHAR(100) NULL,
    telefon VARCHAR(20) NULL,
    ulke VARCHAR(60) NULL,
    sehir VARCHAR(100) NULL,
    uyelik_seviyesi ENUM('Standart','Silver','Gold','Platinum') NOT NULL DEFAULT 'Standart',
    toplam_rezervasyon_sayisi INT  NOT NULL DEFAULT 0,
    toplam_harcama DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    son_rezervasyon_tarihi DATE NULL,
    son_talep_ozeti VARCHAR(255) NULL,
    pazarlama_izni BIT NOT NULL DEFAULT 0,
    notlar NVARCHAR(MAX) NULL,
    olusturan_sales_user_id BIGINT  NULL,
    olusturulma_tarihi DATETIME2 NULL DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL DEFAULT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_satis_musterileri_kod (musteri_kodu),
    KEY idx_satis_musterileri_ad_tel (ad_soyad, telefon),
    KEY idx_satis_musterileri_eposta (eposta),
    KEY idx_satis_musterileri_olusturan (olusturan_sales_user_id),
    CONSTRAINT fk_satis_musterileri_user FOREIGN KEY (olusturan_sales_user_id) REFERENCES users(id) ON DELETE SET NULL
);
