CREATE TABLE departmanlar (
    id SMALLINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    departman_kodu VARCHAR(30) NOT NULL UNIQUE,
    departman_adi VARCHAR(50) NOT NULL,
    ust_departman_id SMALLINT  NULL,
    yonetici_rol_id SMALLINT  NULL,
    bina_kat VARCHAR(20) NULL,
    dahili_telefon VARCHAR(10) NULL,
    aciklama VARCHAR(255),
    aktif_mi BIT DEFAULT 1,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    FOREIGN KEY (ust_departman_id) REFERENCES departmanlar(id) ON DELETE SET NULL,
    FOREIGN KEY (yonetici_rol_id) REFERENCES roller(id) ON DELETE SET NULL,
    
    INDEX idx_departman_kodu (departman_kodu),
    INDEX idx_aktif (aktif_mi)
);

