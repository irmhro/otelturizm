CREATE TABLE IF NOT EXISTS [sss_sorulari] (
    [id] BIGINT  NOT NULL IDENTITY(1,1),
    [sss_kategori_id] BIGINT  NOT NULL,
    [soru] VARCHAR(255) NOT NULL,
    [cevap] NVARCHAR(MAX) NOT NULL,
    [one_cikan_mi] BIT NOT NULL DEFAULT 0,
    [siralama] INT NOT NULL DEFAULT 0,
    [aktif_mi] BIT NOT NULL DEFAULT 1,
    [olusturulma_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [guncellenme_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY ([id]),
    KEY [idx_sss_sorulari_kategori] ([sss_kategori_id]),
    CONSTRAINT [fk_sss_sorulari_kategori] FOREIGN KEY ([sss_kategori_id]) REFERENCES [sss_kategorileri] ([id]) ON DELETE CASCADE ON UPDATE CASCADE
);
