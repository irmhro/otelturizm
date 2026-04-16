CREATE TABLE kullanici_departman (
    kullanici_id BIGINT  NOT NULL,
    departman_id SMALLINT  NOT NULL,
    unvan VARCHAR(100) NULL,
    ise_baslama_tarihi DATE NULL,
    yonetici_mi BIT DEFAULT 0,
    
    PRIMARY KEY (kullanici_id, departman_id),
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (departman_id) REFERENCES departmanlar(id) ON DELETE CASCADE,
    
    INDEX idx_departman (departman_id)
);

