-- Tablo: dbo.MAHALLELER
IF OBJECT_ID(N'dbo.MAHALLELER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MAHALLELER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ULKE_ID] bigint NOT NULL,
        [IL_ID] bigint NOT NULL,
        [ILCE_ID] bigint NOT NULL,
        [API_KODU] int NULL,
        [MAHALLE_ADI] nvarchar(120) NOT NULL,
        [SEO_SLUG] nvarchar(180) NOT NULL,
        [POSTA_KODU] nvarchar(10) NULL,
        [ENLEM] decimal(10,8) NULL,
        [BOYLAM] decimal(11,8) NULL,
        [NUFUS] int NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__mahallele__aktif__656C112C] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__mahallele__olust__66603565] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_MAHALLELER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
