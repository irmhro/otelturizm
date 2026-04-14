CREATE TABLE otel_ozellikleri (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kategori_id SMALLINT UNSIGNED NOT NULL,
    ozellik_adi VARCHAR(100) NOT NULL,
    ozellik_ikon VARCHAR(50) NULL,
    ucretli_mi TINYINT(1) DEFAULT 0 COMMENT 'Bu özellik ek ücrete tabi olabilir mi?',
    one_cikan_ozellik TINYINT(1) DEFAULT 0 COMMENT 'Filtrelerde öne çıksın mı?',
    siralama SMALLINT UNSIGNED DEFAULT 0,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    UNIQUE KEY uk_kategori_ozellik (kategori_id, ozellik_adi),
    INDEX idx_kategori_id (kategori_id),
    INDEX idx_one_cikan (one_cikan_ozellik),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (kategori_id) REFERENCES otel_ozellik_kategorileri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm otel özellikleri havuzu';

