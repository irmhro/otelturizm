SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.odeme_yontemi_tanimlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[odeme_yontemi_tanimlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kod] nvarchar(40) NOT NULL,
        [ad] nvarchar(80) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [sira_no] int CONSTRAINT [DF_odeme_yontemi_tanimlari_sira] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_odeme_yontemi_tanimlari_aktif] DEFAULT ((1)) NOT NULL,
        [sistem_satir_mi] bit CONSTRAINT [DF_odeme_yontemi_tanimlari_sistem] DEFAULT ((1)) NOT NULL,
        [kapida_mi] bit CONSTRAINT [DF_odeme_yontemi_tanimlari_kapida] DEFAULT ((0)) NOT NULL,
        [kart_mi] bit CONSTRAINT [DF_odeme_yontemi_tanimlari_kart] DEFAULT ((0)) NOT NULL,
        [havale_mi] bit CONSTRAINT [DF_odeme_yontemi_tanimlari_havale] DEFAULT ((0)) NOT NULL,
        [online_mi] bit CONSTRAINT [DF_odeme_yontemi_tanimlari_online] DEFAULT ((0)) NOT NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_odeme_yontemi_tanimlari_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__odeme_yo__3213E83FBC148859] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [kod] nvarchar(40) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [ad] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'sira_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [sira_no] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'sistem_satir_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [sistem_satir_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'kapida_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [kapida_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'kart_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [kart_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'havale_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [havale_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'online_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [online_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_yontemi_tanimlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_yontemi_tanimlari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
