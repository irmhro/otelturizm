CREATE TABLE otel_ozellik_kategorileri (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kategori_adi VARCHAR(50) NOT NULL,
    kategori_ikon VARCHAR(50) NULL COMMENT 'Font Awesome ikon adı',
    siralama TINYINT UNSIGNED DEFAULT 0,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    INDEX idx_siralama (siralama),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otel özellikleri kategorileri';

