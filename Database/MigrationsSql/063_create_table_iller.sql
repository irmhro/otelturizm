CREATE TABLE iller (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    plaka_kodu SMALLINT  NOT NULL,
    il_adi VARCHAR(100) NOT NULL,
    seo_slug VARCHAR(120) NOT NULL,
    bolge VARCHAR(50) NULL,
    enlem DECIMAL(10,8) NULL,
    boylam DECIMAL(11,8) NULL,
    aktif_mi BIT NOT NULL DEFAULT 1,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NULL,
    UNIQUE KEY uk_iller_plaka (plaka_kodu),
    UNIQUE KEY uk_iller_slug (seo_slug),
    INDEX idx_iller_ad (il_adi)
);
