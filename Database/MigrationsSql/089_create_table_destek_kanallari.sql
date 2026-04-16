CREATE TABLE IF NOT EXISTS [destek_kanallari] (
    [id] BIGINT  NOT NULL IDENTITY(1,1),
    [kanal_adi] VARCHAR(120) NOT NULL,
    [kanal_turu] VARCHAR(40) NOT NULL,
    [ikon] VARCHAR(80) NOT NULL,
    [aciklama] VARCHAR(255) NOT NULL,
    [buton_metin] VARCHAR(120) NOT NULL,
    [baglanti_url] VARCHAR(255) NOT NULL,
    [ek_bilgi] VARCHAR(180) NULL,
    [renk_tonu] VARCHAR(30) NOT NULL DEFAULT 'primary',
    [siralama] INT NOT NULL DEFAULT 0,
    [aktif_mi] BIT NOT NULL DEFAULT 1,
    [olusturulma_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [guncellenme_tarihi] DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY ([id]),
    KEY [idx_destek_kanallari_tur] ([kanal_turu])
);
