SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS satis_musteri_notlari
(
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    satis_musteri_id BIGINT UNSIGNED NOT NULL,
    sales_user_id BIGINT UNSIGNED NULL,
    not_turu ENUM('Çağrı','Teklif','Hatırlatma','Rezervasyon','Genel') NOT NULL DEFAULT 'Genel',
    not_metni TEXT NOT NULL,
    planlanan_geri_donus_tarihi DATETIME NULL,
    olusturulma_tarihi TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_satis_musteri_notlari_musteri (satis_musteri_id, olusturulma_tarihi),
    KEY idx_satis_musteri_notlari_sales (sales_user_id),
    CONSTRAINT fk_satis_musteri_notlari_musteri FOREIGN KEY (satis_musteri_id) REFERENCES satis_musterileri(id) ON DELETE CASCADE,
    CONSTRAINT fk_satis_musteri_notlari_sales_user FOREIGN KEY (sales_user_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
