-- Tablo: dbo.SSS_KATEGORILERI
IF OBJECT_ID(N'dbo.SSS_KATEGORILERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SSS_KATEGORILERI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KATEGORI_ADI] nvarchar(120) NOT NULL,
        [SEO_SLUG] nvarchar(150) NOT NULL,
        [IKON] nvarchar(80) NOT NULL,
        [SIRALAMA] int CONSTRAINT [DF__sss_kateg__siral__2E3BD7D3] DEFAULT ((0)) NOT NULL,
        [AKTIF_MI] bit CONSTRAINT [DF__sss_kateg__aktif__2F2FFC0C] DEFAULT ((1)) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__sss_kateg__olust__30242045] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(0) CONSTRAINT [DF__sss_kateg__gunce__3118447E] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_SSS_KATEGORILERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
