CREATE TABLE IF NOT EXISTS [destek_kategorileri] (
    [id] BIGINT  NOT NULL IDENTITY(1,1),
    [kategori_adi] VARCHAR(120) NOT NULL,
    [seo_slug] VARCHAR(150) NOT NULL,
    [kategori_ikon] VARCHAR(80) NOT NULL,
    [kisa_aciklama] VARCHAR(255) NULL,
    [renk_kodu] VARCHAR(20) NOT NULL DEFAULT '#003B95',
    [siralama] INT NOT NULL DEFAULT 0,
    [durum] BIT NOT NULL DEFAULT 1,
    [olusturulma_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [guncellenme_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY ([id]),
    UNIQUE KEY [uk_destek_kategorileri_seo_slug] ([seo_slug])
);
