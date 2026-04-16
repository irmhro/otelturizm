CREATE TABLE mahalleler (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    il_id BIGINT  NOT NULL,
    ilce_id BIGINT  NOT NULL,
    mahalle_adi VARCHAR(120) NOT NULL,
    seo_slug VARCHAR(180) NOT NULL,
    posta_kodu VARCHAR(10) NULL,
    enlem DECIMAL(10,8) NULL,
    boylam DECIMAL(11,8) NULL,
    aktif_mi BIT NOT NULL DEFAULT 1,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    UNIQUE KEY uk_mahalle_ilce_slug (ilce_id, seo_slug),
    INDEX idx_mahalle_il (il_id),
    INDEX idx_mahalle_ilce (ilce_id),
    INDEX idx_mahalle_adi (mahalle_adi),
    CONSTRAINT fk_mahalle_il FOREIGN KEY (il_id) REFERENCES iller(id) ON DELETE CASCADE,
    CONSTRAINT fk_mahalle_ilce FOREIGN KEY (ilce_id) REFERENCES ilceler(id) ON DELETE CASCADE
);
