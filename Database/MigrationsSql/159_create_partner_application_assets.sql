CREATE TABLE IF NOT EXISTS partner_basvuru_hareketleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    partner_id BIGINT UNSIGNED NOT NULL,
    onceki_durum VARCHAR(40) NULL,
    yeni_durum VARCHAR(40) NOT NULL,
    islem_tipi VARCHAR(60) NOT NULL,
    aciklama VARCHAR(500) NULL,
    islem_yapan_kullanici_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_partner_basvuru_hareketleri_partner (partner_id),
    KEY idx_partner_basvuru_hareketleri_user (islem_yapan_kullanici_id),
    CONSTRAINT fk_partner_basvuru_hareketleri_partner FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE CASCADE,
    CONSTRAINT fk_partner_basvuru_hareketleri_user FOREIGN KEY (islem_yapan_kullanici_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS partner_basvuru_evraklari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    partner_id BIGINT UNSIGNED NOT NULL,
    guvenli_dosya_id BIGINT UNSIGNED NOT NULL,
    evrak_tipi VARCHAR(80) NOT NULL,
    belge_basligi VARCHAR(150) NULL,
    durum ENUM('Yuklendi','Beklemede','Onaylandi','Reddedildi') NOT NULL DEFAULT 'Beklemede',
    red_nedeni VARCHAR(500) NULL,
    yukleyen_kullanici_id BIGINT UNSIGNED NULL,
    inceleyen_admin_id BIGINT UNSIGNED NULL,
    incelenme_tarihi TIMESTAMP NULL DEFAULT NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_partner_basvuru_evraklari_partner (partner_id),
    KEY idx_partner_basvuru_evraklari_dosya (guvenli_dosya_id),
    KEY idx_partner_basvuru_evraklari_user (yukleyen_kullanici_id),
    KEY idx_partner_basvuru_evraklari_admin (inceleyen_admin_id),
    CONSTRAINT fk_partner_basvuru_evraklari_partner FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE CASCADE,
    CONSTRAINT fk_partner_basvuru_evraklari_dosya FOREIGN KEY (guvenli_dosya_id) REFERENCES guvenli_dosya_varliklari(id) ON DELETE CASCADE,
    CONSTRAINT fk_partner_basvuru_evraklari_user FOREIGN KEY (yukleyen_kullanici_id) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_partner_basvuru_evraklari_admin FOREIGN KEY (inceleyen_admin_id) REFERENCES users(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
