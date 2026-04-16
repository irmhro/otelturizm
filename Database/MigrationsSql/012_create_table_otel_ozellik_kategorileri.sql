CREATE TABLE otel_ozellik_kategorileri (
    id SMALLINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    kategori_adi VARCHAR(50) NOT NULL,
    kategori_ikon VARCHAR(50) NULL,
    siralama TINYINT  DEFAULT 0,
    aktif_mi BIT DEFAULT 1,
    
    INDEX idx_siralama (siralama),
    INDEX idx_aktif (aktif_mi)
);

