SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.iller', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[iller]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [plaka_kodu] smallint NOT NULL,
        [il_adi] nvarchar(100) NOT NULL,
        [seo_slug] nvarchar(120) NOT NULL,
        [bolge] nvarchar(50) NULL,
        [enlem] decimal(10,8) NULL,
        [boylam] decimal(11,8) NULL,
        [nufus] int NULL,
        [aktif_mi] bit CONSTRAINT [DF__iller__aktif_mi__619B8048] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__iller__olusturul__628FA481] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_iller] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.iller', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.iller', N'plaka_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [plaka_kodu] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.iller', N'il_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [il_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.iller', N'seo_slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [seo_slug] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.iller', N'bolge') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [bolge] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.iller', N'enlem') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [enlem] decimal(10,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.iller', N'boylam') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [boylam] decimal(11,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.iller', N'nufus') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [nufus] int NULL;
END
GO
IF COL_LENGTH(N'dbo.iller', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.iller', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.iller', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[iller] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
