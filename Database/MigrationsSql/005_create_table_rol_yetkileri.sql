CREATE TABLE rol_yetkileri (
    rol_id SMALLINT  NOT NULL,
    yetki_id INT  NOT NULL,
    izin_var BIT DEFAULT 1,
    atayan_kullanici_id BIGINT  NULL,
    atama_tarihi DATETIME2 DEFAULT GETDATE(),
    
    PRIMARY KEY (rol_id, yetki_id),
    FOREIGN KEY (rol_id) REFERENCES roller(id) ON DELETE CASCADE,
    FOREIGN KEY (yetki_id) REFERENCES yetkiler(id) ON DELETE CASCADE
);

