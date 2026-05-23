-- Tablo: dbo.KULLANICI_OZEL_TEKLIFLERI
IF OBJECT_ID(N'dbo.KULLANICI_OZEL_TEKLIFLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_OZEL_TEKLIFLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NULL,
        [TEKLIF_TIPI] nvarchar(60) NOT NULL,
        [BASLIK] nvarchar(180) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [KAMPANYA_KODU] nvarchar(80) NOT NULL,
        [INDIRIM_ORANI] decimal(5,2) NULL,
        [MINIMUM_SEPET_TUTARI] decimal(12,2) NULL,
        [GECERLILIK_BASLANGIC] date NOT NULL,
        [GECERLILIK_BITIS] date NOT NULL,
        [BUTON_URL] nvarchar(255) NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__kullanici__aktif__13F1F5EB] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__olust__14E61A24] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__gunce__15DA3E5D] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_OZEL_TEKLIFLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
