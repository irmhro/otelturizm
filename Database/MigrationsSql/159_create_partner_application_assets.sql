CREATE TABLE IF NOT EXISTS partner_basvuru_hareketleri (
    id BIGINT IDENTITY(1,1) NOT NULL,
    partner_id BIGINT  NOT NULL,
    onceki_durum VARCHAR(40) NULL,
    yeni_durum VARCHAR(40) NOT NULL,
    islem_tipi VARCHAR(60) NOT NULL,
    aciklama VARCHAR(500) NULL,
    islem_yapan_kullanici_id BIGINT  NULL,
    olusturulma_tarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (id),
    KEY idx_partner_basvuru_hareketleri_partner (partner_id),
    KEY idx_partner_basvuru_hareketleri_user (islem_yapan_kullanici_id),
    CONSTRAINT fk_partner_basvuru_hareketleri_partner FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE CASCADE,
    CONSTRAINT fk_partner_basvuru_hareketleri_user FOREIGN KEY (islem_yapan_kullanici_id) REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS partner_basvuru_evraklari (
    id BIGINT IDENTITY(1,1) NOT NULL,
    partner_id BIGINT  NOT NULL,
    guvenli_dosya_id BIGINT  NOT NULL,
    evrak_tipi VARCHAR(80) NOT NULL,
    belge_basligi VARCHAR(150) NULL,
    durum ENUM('Yuklendi','Beklemede','Onaylandi','Reddedildi') NOT NULL DEFAULT 'Beklemede',
    red_nedeni VARCHAR(500) NULL,
    yukleyen_kullanici_id BIGINT  NULL,
    inceleyen_admin_id BIGINT  NULL,
    incelenme_tarihi DATETIME2 NULL DEFAULT NULL,
    olusturulma_tarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL DEFAULT NULL,
    PRIMARY KEY (id),
    KEY idx_partner_basvuru_evraklari_partner (partner_id),
    KEY idx_partner_basvuru_evraklari_dosya (guvenli_dosya_id),
    KEY idx_partner_basvuru_evraklari_user (yukleyen_kullanici_id),
    KEY idx_partner_basvuru_evraklari_admin (inceleyen_admin_id),
    CONSTRAINT fk_partner_basvuru_evraklari_partner FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE CASCADE,
    CONSTRAINT fk_partner_basvuru_evraklari_dosya FOREIGN KEY (guvenli_dosya_id) REFERENCES guvenli_dosya_varliklari(id) ON DELETE CASCADE,
    CONSTRAINT fk_partner_basvuru_evraklari_user FOREIGN KEY (yukleyen_kullanici_id) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_partner_basvuru_evraklari_admin FOREIGN KEY (inceleyen_admin_id) REFERENCES users(id) ON DELETE SET NULL
);
