SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sadakat_seviyeleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sadakat_seviyeleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kod] nvarchar(40) NOT NULL,
        [ad] nvarchar(100) NOT NULL,
        [minimum_puan] int CONSTRAINT [DF__sadakat_s__minim__090A5324] DEFAULT ((0)) NOT NULL,
        [maximum_puan] int NULL,
        [renk_kodu] nvarchar(20) NULL,
        [ikon] nvarchar(120) NULL,
        [avantajlar_metin] nvarchar(max) NULL,
        [sira_no] int CONSTRAINT [DF__sadakat_s__sira___09FE775D] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__sadakat_s__aktif__0AF29B96] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__sadakat_s__olust__0BE6BFCF] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__sadakat_s__gunce__0CDAE408] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_sadakat_seviyeleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [kod] nvarchar(40) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [ad] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'minimum_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [minimum_puan] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'maximum_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [maximum_puan] int NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'renk_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [renk_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [ikon] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'avantajlar_metin') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [avantajlar_metin] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'sira_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [sira_no] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_seviyeleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_seviyeleri] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
