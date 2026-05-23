SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.odeme_durumu_tanimlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[odeme_durumu_tanimlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kod] nvarchar(40) NOT NULL,
        [ad] nvarchar(80) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [sira_no] int CONSTRAINT [DF_odeme_durumu_tanimlari_sira] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_odeme_durumu_tanimlari_aktif] DEFAULT ((1)) NOT NULL,
        [bekleyen_mi] bit CONSTRAINT [DF_odeme_durumu_tanimlari_bekleyen] DEFAULT ((0)) NOT NULL,
        [basari_mi] bit CONSTRAINT [DF_odeme_durumu_tanimlari_basari] DEFAULT ((0)) NOT NULL,
        [tam_mi] bit CONSTRAINT [DF_odeme_durumu_tanimlari_tam] DEFAULT ((0)) NOT NULL,
        [iade_mi] bit CONSTRAINT [DF_odeme_durumu_tanimlari_iade] DEFAULT ((0)) NOT NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_odeme_durumu_tanimlari_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__odeme_du__3213E83F8A83202D] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [kod] nvarchar(40) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [ad] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'sira_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [sira_no] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'bekleyen_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [bekleyen_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'basari_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [basari_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'tam_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [tam_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'iade_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [iade_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_durumu_tanimlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_durumu_tanimlari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
