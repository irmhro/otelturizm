SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.destek_makaleleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[destek_makaleleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [destek_kategori_id] bigint NOT NULL,
        [baslik] nvarchar(180) NOT NULL,
        [seo_slug] nvarchar(180) NOT NULL,
        [ozet] nvarchar(300) NULL,
        [icerik] nvarchar(max) NOT NULL,
        [ikon] nvarchar(80) NULL,
        [one_cikan_mi] bit CONSTRAINT [DF__destek_ma__one_c__123EB7A3] DEFAULT ((0)) NOT NULL,
        [yardim_merkezinde_goster] bit CONSTRAINT [DF__destek_ma__yardi__1332DBDC] DEFAULT ((1)) NOT NULL,
        [siralama] int CONSTRAINT [DF__destek_ma__siral__14270015] DEFAULT ((0)) NOT NULL,
        [durum] bit CONSTRAINT [DF__destek_ma__durum__151B244E] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__destek_ma__olust__160F4887] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__destek_ma__gunce__17036CC0] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_destek_makaleleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.destek_makaleleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'destek_kategori_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [destek_kategori_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [baslik] nvarchar(180) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'seo_slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [seo_slug] nvarchar(180) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'ozet') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [ozet] nvarchar(300) NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'icerik') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [icerik] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [ikon] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'one_cikan_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [one_cikan_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'yardim_merkezinde_goster') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [yardim_merkezinde_goster] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [durum] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_makaleleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_makaleleri] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
