-- Tablo: dbo.ODEME_DURUMU_TANIMLARI
IF OBJECT_ID(N'dbo.ODEME_DURUMU_TANIMLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ODEME_DURUMU_TANIMLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(40) NOT NULL,
        [AD] nvarchar(80) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [SIRA_NO] int NOT NULL CONSTRAINT [DF_odeme_durumu_tanimlari_sira] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_odeme_durumu_tanimlari_aktif] DEFAULT ((1)),
        [BEKLEYEN_MI] bit NOT NULL CONSTRAINT [DF_odeme_durumu_tanimlari_bekleyen] DEFAULT ((0)),
        [BASARI_MI] bit NOT NULL CONSTRAINT [DF_odeme_durumu_tanimlari_basari] DEFAULT ((0)),
        [TAM_MI] bit NOT NULL CONSTRAINT [DF_odeme_durumu_tanimlari_tam] DEFAULT ((0)),
        [IADE_MI] bit NOT NULL CONSTRAINT [DF_odeme_durumu_tanimlari_iade] DEFAULT ((0)),
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_odeme_durumu_tanimlari_olustur] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ODEME_DURUMU_TANIMLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
