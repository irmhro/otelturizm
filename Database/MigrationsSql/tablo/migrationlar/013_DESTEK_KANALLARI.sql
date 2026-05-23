-- Tablo: dbo.DESTEK_KANALLARI
IF OBJECT_ID(N'dbo.DESTEK_KANALLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DESTEK_KANALLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KANAL_ADI] nvarchar(120) NOT NULL,
        [KANAL_TURU] nvarchar(40) NOT NULL,
        [IKON] nvarchar(80) NOT NULL,
        [ACIKLAMA] nvarchar(255) NOT NULL,
        [BUTON_METIN] nvarchar(120) NOT NULL,
        [BAGLANTI_URL] nvarchar(255) NOT NULL,
        [EK_BILGI] nvarchar(180) NULL,
        [RENK_TONU] nvarchar(30) NOT NULL,
        [SIRALAMA] int NOT NULL CONSTRAINT [DF__destek_ka__siral__06CD04F7] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__destek_ka__aktif__07C12930] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__destek_ka__olust__08B54D69] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__destek_ka__gunce__09A971A2] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_DESTEK_KANALLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
