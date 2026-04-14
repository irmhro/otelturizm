CREATE TABLE yetkiler (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    yetki_kodu VARCHAR(100) NOT NULL UNIQUE COMMENT 'örn: otel.sil, rezervasyon.iptal, finans.rapor.gor',
    modul VARCHAR(50) NOT NULL COMMENT 'Otel, Rezervasyon, Finans, Kullanici, Rapor, Sistem',
    eylem VARCHAR(50) NOT NULL COMMENT 'listele, goruntule, ekle, duzenle, sil, onayla, reddet, indir',
    aciklama VARCHAR(255),
    varsayilan_izin TINYINT(1) DEFAULT 0 COMMENT 'Tüm roller için varsayılan izin',
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_modul (modul),
    INDEX idx_eylem (eylem)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Sistemdeki tüm yetki tanımları';

