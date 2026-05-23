-- Tablo: dbo.FIYAT_INDIRIMLERI
IF OBJECT_ID(N'dbo.FIYAT_INDIRIMLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FIYAT_INDIRIMLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [INDIRIM_ADI] nvarchar(140) NOT NULL,
        [KISA_ACIKLAMA] nvarchar(220) NULL,
        [DETAY_HTML] nvarchar(max) NULL,
        [GORSEL_URL] nvarchar(500) NULL,
        [IKON_CLASS] nvarchar(80) NULL,
        [RENK_KODU] nvarchar(30) NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_fiyat_indirimleri_aktif] DEFAULT ((1)),
        [SIRALAMA] smallint NOT NULL CONSTRAINT [DF_fiyat_indirimleri_siralama] DEFAULT ((100)),
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_fiyat_indirimleri_olusturulma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_fiyat_indirimleri_guncellenme] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_FIYAT_INDIRIMLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
