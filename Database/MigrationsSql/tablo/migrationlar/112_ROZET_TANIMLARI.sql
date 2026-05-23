-- Tablo: dbo.ROZET_TANIMLARI
IF OBJECT_ID(N'dbo.ROZET_TANIMLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ROZET_TANIMLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(60) NOT NULL,
        [AD] nvarchar(120) NOT NULL,
        [ACIKLAMA] nvarchar(255) NULL,
        [IKON] nvarchar(120) NULL,
        [KATEGORI] nvarchar(80) NULL,
        [ROZET_RENGI] nvarchar(20) NULL,
        [HEDEF_DEGER] int CONSTRAINT [DF__rozet_tan__hedef__7F80E8EA] DEFAULT ((1)) NOT NULL,
        [SIRALAMA] int CONSTRAINT [DF__rozet_tan__siral__00750D23] DEFAULT ((0)) NOT NULL,
        [AKTIF_MI] bit CONSTRAINT [DF__rozet_tan__aktif__0169315C] DEFAULT ((1)) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__rozet_tan__olust__025D5595] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_ROZET_TANIMLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
