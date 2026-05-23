-- Tablo: dbo.DESTEK_MAKALELERI
IF OBJECT_ID(N'dbo.DESTEK_MAKALELERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DESTEK_MAKALELERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [DESTEK_KATEGORI_ID] bigint NOT NULL,
        [BASLIK] nvarchar(180) NOT NULL,
        [SEO_SLUG] nvarchar(180) NOT NULL,
        [OZET] nvarchar(300) NULL,
        [ICERIK] nvarchar(max) NOT NULL,
        [IKON] nvarchar(80) NULL,
        [ONE_CIKAN_MI] bit NOT NULL CONSTRAINT [DF__destek_ma__one_c__123EB7A3] DEFAULT ((0)),
        [YARDIM_MERKEZINDE_GOSTER] bit NOT NULL CONSTRAINT [DF__destek_ma__yardi__1332DBDC] DEFAULT ((1)),
        [SIRALAMA] int NOT NULL CONSTRAINT [DF__destek_ma__siral__14270015] DEFAULT ((0)),
        [DURUM] bit NOT NULL CONSTRAINT [DF__destek_ma__durum__151B244E] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__destek_ma__olust__160F4887] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__destek_ma__gunce__17036CC0] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_DESTEK_MAKALELERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
