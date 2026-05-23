-- Tablo: dbo.SISTEM_ICI_BILDIRIMLER
IF OBJECT_ID(N'dbo.SISTEM_ICI_BILDIRIMLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SISTEM_ICI_BILDIRIMLER]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [BILDIRIM_TURU] nvarchar(255) NOT NULL,
        [BASLIK] nvarchar(100) NOT NULL,
        [MESAJ] nvarchar(max) NOT NULL,
        [IKON] nvarchar(50) NULL,
        [RENK] nvarchar(20) NULL,
        [AKSIYON_URL] nvarchar(500) NULL,
        [AKSIYON_METNI] nvarchar(50) NULL,
        [OKUNDU_MU] bit CONSTRAINT [DF__sistem_ic__okund__297722B6] DEFAULT ((0)) NULL,
        [OKUNMA_TARIHI] datetime2(0) NULL,
        [ARSIVLENDI_MI] bit CONSTRAINT [DF__sistem_ic__arsiv__2A6B46EF] DEFAULT ((0)) NULL,
        [ONEM_DERECESI] nvarchar(255) NULL,
        [ILGILI_TABLO] nvarchar(50) NULL,
        [ILGILI_KAYIT_ID] bigint NULL,
        [GECERLILIK_BASLANGIC] datetime2(0) NULL,
        [GECERLILIK_BITIS] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__sistem_ic__olust__2B5F6B28] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_SISTEM_ICI_BILDIRIMLER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
