CREATE TABLE oda_ozellikleri (
    id SMALLINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    kategori VARCHAR(50) NOT NULL,
    ozellik_adi VARCHAR(100) NOT NULL,
    ozellik_ikon VARCHAR(50) NULL,
    siralama SMALLINT  DEFAULT 0,
    aktif_mi BIT DEFAULT 1,
    
    INDEX idx_kategori (kategori),
    INDEX idx_aktif (aktif_mi)
);

