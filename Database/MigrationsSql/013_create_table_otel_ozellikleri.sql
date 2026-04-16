CREATE TABLE otel_ozellikleri (
    id INT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    kategori_id SMALLINT  NOT NULL,
    ozellik_adi VARCHAR(100) NOT NULL,
    ozellik_ikon VARCHAR(50) NULL,
    ucretli_mi BIT DEFAULT 0,
    one_cikan_ozellik BIT DEFAULT 0,
    siralama SMALLINT  DEFAULT 0,
    aktif_mi BIT DEFAULT 1,
    
    UNIQUE KEY uk_kategori_ozellik (kategori_id, ozellik_adi),
    INDEX idx_kategori_id (kategori_id),
    INDEX idx_one_cikan (one_cikan_ozellik),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (kategori_id) REFERENCES otel_ozellik_kategorileri(id) ON DELETE CASCADE
);

