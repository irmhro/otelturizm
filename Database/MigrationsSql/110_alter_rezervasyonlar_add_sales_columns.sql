SET NAMES utf8mb4;

ALTER TABLE rezervasyonlar
ADD satis_temsilcisi_id BIGINT  NULL,
ADD satis_musteri_id BIGINT  NULL,
ADD rezervasyon_kanali ENUM('Web','Mobil App','Telefon','Acente','Kurumsal','Satış Paneli') NULL,
ADD musteri_talep_notu NVARCHAR(MAX) NULL;

ALTER TABLE rezervasyonlar
ADD INDEX idx_rez_sales_user (satis_temsilcisi_id, olusturulma_tarihi),
ADD INDEX idx_rez_sales_customer (satis_musteri_id),
ADD CONSTRAINT fk_rez_sales_user FOREIGN KEY (satis_temsilcisi_id) REFERENCES users(id) ON DELETE SET NULL,
ADD CONSTRAINT fk_rez_sales_customer FOREIGN KEY (satis_musteri_id) REFERENCES satis_musterileri(id) ON DELETE SET NULL;
