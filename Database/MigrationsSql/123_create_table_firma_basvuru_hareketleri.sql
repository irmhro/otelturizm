CREATE TABLE IF NOT EXISTS firma_basvuru_hareketleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    firma_id BIGINT UNSIGNED NOT NULL,
    onceki_durum ENUM('Beklemede','Onaylandı','Reddedildi','Askıda') NULL,
    yeni_durum ENUM('Beklemede','Onaylandı','Reddedildi','Askıda') NOT NULL,
    hareket_tipi ENUM('Basvuru Alindi','Incelemeye Alindi','Bilgi Talebi','Onaylandi','Reddedildi','Askida','Aktiflestirildi','Giris Yetkisi Kapatildi','Not Eklendi') NOT NULL,
    aciklama TEXT NULL,
    islem_yapan_kullanici_id BIGINT UNSIGNED NULL,
    islem_kaynagi VARCHAR(50) NOT NULL DEFAULT 'system',
    ip_adresi VARCHAR(45) NULL,
    olusturulma_tarihi TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_firma_basvuru_hareketleri_firma_tarih (firma_id, olusturulma_tarihi DESC),
    KEY idx_firma_basvuru_hareketleri_durum (yeni_durum, olusturulma_tarihi DESC),
    KEY idx_firma_basvuru_hareketleri_islem_yapan (islem_yapan_kullanici_id),
    CONSTRAINT fk_firma_basvuru_hareketleri_firma FOREIGN KEY (firma_id) REFERENCES firmalar (id) ON DELETE CASCADE,
    CONSTRAINT fk_firma_basvuru_hareketleri_user FOREIGN KEY (islem_yapan_kullanici_id) REFERENCES users (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
