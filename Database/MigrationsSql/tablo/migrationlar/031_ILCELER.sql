-- Tablo: dbo.ILCELER
IF OBJECT_ID(N'dbo.ILCELER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ILCELER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ULKE_ID] bigint NOT NULL,
        [IL_ID] bigint NOT NULL,
        [DIS_KOD] int NULL,
        [API_KODU] int NULL,
        [ILCE_ADI] nvarchar(100) NOT NULL,
        [SEO_SLUG] nvarchar(140) NOT NULL,
        [MERKEZ_MI] bit NOT NULL CONSTRAINT [DF__ilceler__merkez___5CD6CB2B] DEFAULT ((0)),
        [ENLEM] decimal(10,8) NULL,
        [BOYLAM] decimal(11,8) NULL,
        [NUFUS] int NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__ilceler__aktif_m__5DCAEF64] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__ilceler__olustur__5EBF139D] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_ILCELER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
