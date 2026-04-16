CREATE TABLE yetkiler (
    id INT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    yetki_kodu VARCHAR(100) NOT NULL UNIQUE,
    modul VARCHAR(50) NOT NULL,
    eylem VARCHAR(50) NOT NULL,
    aciklama VARCHAR(255),
    varsayilan_izin BIT DEFAULT 0,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    INDEX idx_modul (modul),
    INDEX idx_eylem (eylem)
);

