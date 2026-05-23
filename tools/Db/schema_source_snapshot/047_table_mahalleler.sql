SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.mahalleler', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[mahalleler]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [il_id] bigint NOT NULL,
        [ilce_id] bigint NOT NULL,
        [api_kodu] int NULL,
        [mahalle_adi] nvarchar(120) NOT NULL,
        [seo_slug] nvarchar(180) NOT NULL,
        [posta_kodu] nvarchar(10) NULL,
        [enlem] decimal(10,8) NULL,
        [boylam] decimal(11,8) NULL,
        [nufus] int NULL,
        [aktif_mi] bit CONSTRAINT [DF__mahallele__aktif__656C112C] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__mahallele__olust__66603565] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_mahalleler] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.mahalleler', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'il_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [il_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'ilce_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [ilce_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'api_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [api_kodu] int NULL;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'mahalle_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [mahalle_adi] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'seo_slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [seo_slug] nvarchar(180) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'posta_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [posta_kodu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'enlem') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [enlem] decimal(10,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'boylam') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [boylam] decimal(11,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'nufus') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [nufus] int NULL;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.mahalleler', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mahalleler] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
