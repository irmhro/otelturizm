-- Tablo: dbo.DESTEK_KATEGORILERI
IF OBJECT_ID(N'dbo.DESTEK_KATEGORILERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DESTEK_KATEGORILERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KATEGORI_ADI] nvarchar(120) NOT NULL,
        [SEO_SLUG] nvarchar(150) NOT NULL,
        [KATEGORI_IKON] nvarchar(80) NOT NULL,
        [KISA_ACIKLAMA] nvarchar(255) NULL,
        [RENK_KODU] nvarchar(20) NOT NULL,
        [SIRALAMA] int NOT NULL CONSTRAINT [DF__destek_ka__siral__0C85DE4D] DEFAULT ((0)),
        [DURUM] bit NOT NULL CONSTRAINT [DF__destek_ka__durum__0D7A0286] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__destek_ka__olust__0E6E26BF] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__destek_ka__gunce__0F624AF8] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_DESTEK_KATEGORILERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
