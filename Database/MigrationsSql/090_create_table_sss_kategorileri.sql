CREATE TABLE IF NOT EXISTS [sss_kategorileri] (
    [id] BIGINT  NOT NULL IDENTITY(1,1),
    [kategori_adi] VARCHAR(120) NOT NULL,
    [seo_slug] VARCHAR(150) NOT NULL,
    [ikon] VARCHAR(80) NOT NULL,
    [siralama] INT NOT NULL DEFAULT 0,
    [aktif_mi] BIT NOT NULL DEFAULT 1,
    [olusturulma_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [guncellenme_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY ([id]),
    UNIQUE KEY [uk_sss_kategorileri_slug] ([seo_slug])
);
