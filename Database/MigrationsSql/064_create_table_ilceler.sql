CREATE TABLE ilceler (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    il_id BIGINT  NOT NULL,
    dis_kod INT  NULL,
    ilce_adi VARCHAR(100) NOT NULL,
    seo_slug VARCHAR(140) NOT NULL,
    merkez_mi BIT NOT NULL DEFAULT 0,
    enlem DECIMAL(10,8) NULL,
    boylam DECIMAL(11,8) NULL,
    aktif_mi BIT NOT NULL DEFAULT 1,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    UNIQUE KEY uk_ilceler_il_slug (il_id, seo_slug),
    UNIQUE KEY uk_ilceler_il_diskod (il_id, dis_kod),
    INDEX idx_ilceler_il_id (il_id),
    INDEX idx_ilceler_ad (ilce_adi),
    CONSTRAINT fk_ilceler_il FOREIGN KEY (il_id) REFERENCES iller(id) ON DELETE CASCADE
);
