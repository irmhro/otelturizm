SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.rezervasyon_durum_tanimlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rezervasyon_durum_tanimlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kod] nvarchar(40) NOT NULL,
        [ad] nvarchar(80) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [sira_no] int CONSTRAINT [DF_rezervasyon_durum_tanimlari_sira] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_aktif] DEFAULT ((1)) NOT NULL,
        [sistem_satir_mi] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_sistem] DEFAULT ((1)) NOT NULL,
        [iptal_mi] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_iptal] DEFAULT ((0)) NOT NULL,
        [tamamlandi_mi] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_tamamlandi] DEFAULT ((0)) NOT NULL,
        [bekleyen_mi] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_bekleyen] DEFAULT ((0)) NOT NULL,
        [gelir_sayilir_mi] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_gelir] DEFAULT ((0)) NOT NULL,
        [ozellik_json_sablonu] nvarchar(max) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_rezervasyon_durum_tanimlari_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__rezervas__3213E83FB7986080] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [kod] nvarchar(40) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [ad] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'sira_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [sira_no] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'sistem_satir_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [sistem_satir_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'iptal_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [iptal_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'tamamlandi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [tamamlandi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'bekleyen_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [bekleyen_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'gelir_sayilir_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [gelir_sayilir_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'ozellik_json_sablonu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [ozellik_json_sablonu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_durum_tanimlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_durum_tanimlari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
