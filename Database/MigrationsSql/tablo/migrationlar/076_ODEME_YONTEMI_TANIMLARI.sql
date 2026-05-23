-- Tablo: dbo.ODEME_YONTEMI_TANIMLARI
IF OBJECT_ID(N'dbo.ODEME_YONTEMI_TANIMLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ODEME_YONTEMI_TANIMLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(40) NOT NULL,
        [AD] nvarchar(80) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [SIRA_NO] int NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_sira] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_aktif] DEFAULT ((1)),
        [SISTEM_SATIR_MI] bit NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_sistem] DEFAULT ((1)),
        [KAPIDA_MI] bit NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_kapida] DEFAULT ((0)),
        [KART_MI] bit NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_kart] DEFAULT ((0)),
        [HAVALE_MI] bit NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_havale] DEFAULT ((0)),
        [ONLINE_MI] bit NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_online] DEFAULT ((0)),
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_odeme_yontemi_tanimlari_olustur] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ODEME_YONTEMI_TANIMLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
