SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ilceler', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ilceler]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [il_id] bigint NOT NULL,
        [dis_kod] int NULL,
        [api_kodu] int NULL,
        [ilce_adi] nvarchar(100) NOT NULL,
        [seo_slug] nvarchar(140) NOT NULL,
        [merkez_mi] bit CONSTRAINT [DF__ilceler__merkez___5CD6CB2B] DEFAULT ((0)) NOT NULL,
        [enlem] decimal(10,8) NULL,
        [boylam] decimal(11,8) NULL,
        [nufus] int NULL,
        [aktif_mi] bit CONSTRAINT [DF__ilceler__aktif_m__5DCAEF64] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__ilceler__olustur__5EBF139D] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_ilceler] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.ilceler', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'il_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [il_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'dis_kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [dis_kod] int NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'api_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [api_kodu] int NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'ilce_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [ilce_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'seo_slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [seo_slug] nvarchar(140) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'merkez_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [merkez_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'enlem') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [enlem] decimal(10,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'boylam') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [boylam] decimal(11,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'nufus') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [nufus] int NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.ilceler', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ilceler] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
