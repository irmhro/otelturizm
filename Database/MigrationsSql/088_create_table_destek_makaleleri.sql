CREATE TABLE IF NOT EXISTS [destek_makaleleri] (
    [id] BIGINT  NOT NULL IDENTITY(1,1),
    [destek_kategori_id] BIGINT  NOT NULL,
    [baslik] VARCHAR(180) NOT NULL,
    [seo_slug] VARCHAR(180) NOT NULL,
    [ozet] VARCHAR(300) NULL,
    [icerik] NVARCHAR(MAX) NOT NULL,
    [ikon] VARCHAR(80) NULL,
    [one_cikan_mi] BIT NOT NULL DEFAULT 0,
    [yardim_merkezinde_goster] BIT NOT NULL DEFAULT 1,
    [siralama] INT NOT NULL DEFAULT 0,
    [durum] BIT NOT NULL DEFAULT 1,
    [olusturulma_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [guncellenme_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY ([id]),
    UNIQUE KEY [uk_destek_makaleleri_seo_slug] ([seo_slug]),
    KEY [idx_destek_makaleleri_kategori] ([destek_kategori_id]),
    CONSTRAINT [fk_destek_makaleleri_kategori] FOREIGN KEY ([destek_kategori_id]) REFERENCES [destek_kategorileri] ([id]) ON DELETE CASCADE ON UPDATE CASCADE
);
