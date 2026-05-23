-- Tablo: dbo.ILLER
IF OBJECT_ID(N'dbo.ILLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ILLER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ULKE_ID] bigint NOT NULL,
        [BOLGE_TIPI] nvarchar(20) NOT NULL CONSTRAINT [DF__iller__bolge_tipi] DEFAULT (N'IL'),
        [DIS_KOD] nvarchar(16) NULL,
        [PLAKA_KODU] smallint NOT NULL,
        [IL_ADI] nvarchar(100) NOT NULL,
        [SEO_SLUG] nvarchar(120) NOT NULL,
        [BOLGE] nvarchar(50) NULL,
        [ENLEM] decimal(10,8) NULL,
        [BOYLAM] decimal(11,8) NULL,
        [NUFUS] int NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__iller__aktif_mi__619B8048] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__iller__olusturul__628FA481] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_ILLER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
