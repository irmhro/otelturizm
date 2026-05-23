-- Tablo: dbo.KAMPANYA_OTELLER
IF OBJECT_ID(N'dbo.KAMPANYA_OTELLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KAMPANYA_OTELLER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KAMPANYA_ID] bigint NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [PARTNER_ID] bigint NULL,
        [KATILIM_DURUMU] nvarchar(255) NOT NULL,
        [KATILIM_KAYNAGI] nvarchar(255) NOT NULL,
        [BASLANGIC_TARIHI] datetime2(0) NOT NULL,
        [BITIS_TARIHI] datetime2(0) NOT NULL,
        [OZEL_INDIRIM_ORANI] decimal(5,2) NULL,
        [OZEL_INDIRIM_TUTARI] decimal(10,2) NULL,
        [OZEL_KAMPANYALI_FIYAT] decimal(10,2) NULL,
        [KAMPANYA_ETIKETI] nvarchar(120) NULL,
        [LANDING_URL] nvarchar(500) NULL,
        [PARTNER_NOTU] nvarchar(max) NULL,
        [ONE_CIKAN] bit NOT NULL CONSTRAINT [DF__kampanya___one_c__51300E55] DEFAULT ((0)),
        [SIRALAMA] int NOT NULL CONSTRAINT [DF__kampanya___siral__5224328E] DEFAULT ((0)),
        [ADMIN_ONAY_TARIHI] datetime2(0) NULL,
        [PARTNER_ONAY_TARIHI] datetime2(0) NULL,
        [OLUSTURAN_KULLANICI_ID] bigint NULL,
        [GUNCELLEYEN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kampanya___olust__531856C7] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_KAMPANYA_OTELLER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
