-- Tablo: dbo.YARDIM_MERKEZI_ICERIKLER
IF OBJECT_ID(N'dbo.YARDIM_MERKEZI_ICERIKLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[YARDIM_MERKEZI_ICERIKLER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ICERIK_TURU] nvarchar(40) NOT NULL,
        [BASLIK] nvarchar(180) NOT NULL,
        [SEO_SLUG] nvarchar(180) NOT NULL,
        [OZET] nvarchar(320) NULL,
        [HERO_BASLIK] nvarchar(160) NULL,
        [HERO_ALT_BASLIK] nvarchar(260) NULL,
        [HERO_GORSEL_URL] nvarchar(400) NULL,
        [ICERIK] nvarchar(max) NOT NULL,
        [IKON] nvarchar(80) NULL,
        [SIRALAMA] int NOT NULL CONSTRAINT [DF_ym_icerik_sira] DEFAULT ((0)),
        [ONE_CIKAN_MI] bit NOT NULL CONSTRAINT [DF_ym_icerik_onecikan] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_ym_icerik_aktif] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_ym_icerik_olustur] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_ym_icerik_guncel] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_YARDIM_MERKEZI_ICERIKLER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
