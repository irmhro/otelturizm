ALTER TABLE oda_fiyat_musaitlik
    ADD COLUMN kampanya_etiketi VARCHAR(120) NULL AFTER kapali_satis,
    ADD COLUMN fiyat_notu VARCHAR(255) NULL AFTER kampanya_etiketi,
    ADD COLUMN guncelleyen_kullanici_id BIGINT UNSIGNED NULL AFTER fiyat_notu,
    ADD INDEX idx_ofm_updated_by (guncelleyen_kullanici_id),
    ADD INDEX idx_ofm_partner_calendar (tarih, kapali_satis, indirimli_fiyat),
    ADD CONSTRAINT fk_ofm_updated_by_user FOREIGN KEY (guncelleyen_kullanici_id) REFERENCES users(id) ON DELETE SET NULL;
