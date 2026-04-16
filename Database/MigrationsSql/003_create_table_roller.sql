CREATE TABLE roller (
    id SMALLINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    rol_kodu VARCHAR(30) NOT NULL UNIQUE,
    rol_adi VARCHAR(50) NOT NULL,
    departman VARCHAR(50) NOT NULL,
    seviye TINYINT  NOT NULL,
    ust_rol_id SMALLINT  NULL,
    varsayilan_mi BIT DEFAULT 0,
    aciklama VARCHAR(255),
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),

    INDEX idx_departman (departman),
    INDEX idx_seviye (seviye),
    INDEX idx_rol_kodu (rol_kodu),
    FOREIGN KEY (ust_rol_id) REFERENCES roller(id) ON DELETE SET NULL
);

