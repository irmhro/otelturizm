CREATE TABLE oda_ozellikleri (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kategori VARCHAR(50) NOT NULL COMMENT 'Genel, Yatak Odası, Banyo, Teknoloji, Mutfak',
    ozellik_adi VARCHAR(100) NOT NULL,
    ozellik_ikon VARCHAR(50) NULL,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    INDEX idx_kategori (kategori),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda içi özellikler havuzu';

