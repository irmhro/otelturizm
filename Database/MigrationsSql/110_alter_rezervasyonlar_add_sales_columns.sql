SET NAMES utf8mb4;

ALTER TABLE rezervasyonlar
ADD COLUMN satis_temsilcisi_id BIGINT UNSIGNED NULL AFTER firma_calisan_id,
ADD COLUMN satis_musteri_id BIGINT UNSIGNED NULL AFTER satis_temsilcisi_id,
ADD COLUMN rezervasyon_kanali ENUM('Web','Mobil App','Telefon','Acente','Kurumsal','Satış Paneli') NULL AFTER kaynak,
ADD COLUMN musteri_talep_notu TEXT NULL AFTER ozel_istekler;

ALTER TABLE rezervasyonlar
ADD INDEX idx_rez_sales_user (satis_temsilcisi_id, olusturulma_tarihi),
ADD INDEX idx_rez_sales_customer (satis_musteri_id),
ADD CONSTRAINT fk_rez_sales_user FOREIGN KEY (satis_temsilcisi_id) REFERENCES users(id) ON DELETE SET NULL,
ADD CONSTRAINT fk_rez_sales_customer FOREIGN KEY (satis_musteri_id) REFERENCES satis_musterileri(id) ON DELETE SET NULL;
