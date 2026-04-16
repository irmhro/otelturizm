CREATE TABLE kullanici_rolleri (
    kullanici_id BIGINT  NOT NULL,
    rol_id SMALLINT  NOT NULL,
    atayan_kullanici_id BIGINT  NULL,
    atama_tarihi DATETIME2 DEFAULT GETDATE(),
    bitis_tarihi DATETIME2 NULL,
    
    PRIMARY KEY (kullanici_id, rol_id),
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (rol_id) REFERENCES roller(id) ON DELETE CASCADE,
    FOREIGN KEY (atayan_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    
    INDEX idx_rol (rol_id),
    INDEX idx_bitis (bitis_tarihi)
);

