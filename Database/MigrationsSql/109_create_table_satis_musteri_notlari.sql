SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS satis_musteri_notlari
(
    id BIGINT  NOT NULL IDENTITY(1,1),
    satis_musteri_id BIGINT  NOT NULL,
    sales_user_id BIGINT  NULL,
    not_turu ENUM('Çağrı','Teklif','Hatırlatma','Rezervasyon','Genel') NOT NULL DEFAULT 'Genel',
    not_metni NVARCHAR(MAX) NOT NULL,
    planlanan_geri_donus_tarihi DATETIME2 NULL,
    olusturulma_tarihi DATETIME2 NULL DEFAULT GETDATE(),
    PRIMARY KEY (id),
    KEY idx_satis_musteri_notlari_musteri (satis_musteri_id, olusturulma_tarihi),
    KEY idx_satis_musteri_notlari_sales (sales_user_id),
    CONSTRAINT fk_satis_musteri_notlari_musteri FOREIGN KEY (satis_musteri_id) REFERENCES satis_musterileri(id) ON DELETE CASCADE,
    CONSTRAINT fk_satis_musteri_notlari_sales_user FOREIGN KEY (sales_user_id) REFERENCES users(id) ON DELETE SET NULL
);
