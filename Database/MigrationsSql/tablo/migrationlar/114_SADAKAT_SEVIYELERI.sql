-- Tablo: dbo.SADAKAT_SEVIYELERI
IF OBJECT_ID(N'dbo.SADAKAT_SEVIYELERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SADAKAT_SEVIYELERI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(40) NOT NULL,
        [AD] nvarchar(100) NOT NULL,
        [MINIMUM_PUAN] int CONSTRAINT [DF__sadakat_s__minim__090A5324] DEFAULT ((0)) NOT NULL,
        [MAXIMUM_PUAN] int NULL,
        [RENK_KODU] nvarchar(20) NULL,
        [IKON] nvarchar(120) NULL,
        [AVANTAJLAR_METIN] nvarchar(max) NULL,
        [SIRA_NO] int CONSTRAINT [DF__sadakat_s__sira___09FE775D] DEFAULT ((0)) NOT NULL,
        [AKTIF_MI] bit CONSTRAINT [DF__sadakat_s__aktif__0AF29B96] DEFAULT ((1)) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__sadakat_s__olust__0BE6BFCF] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(0) CONSTRAINT [DF__sadakat_s__gunce__0CDAE408] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_SADAKAT_SEVIYELERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
