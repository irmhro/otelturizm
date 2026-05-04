SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sss_kategorileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sss_kategorileri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kategori_adi] nvarchar(120) NOT NULL,
        [seo_slug] nvarchar(150) NOT NULL,
        [ikon] nvarchar(80) NOT NULL,
        [siralama] int CONSTRAINT [DF__sss_kateg__siral__2E3BD7D3] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__sss_kateg__aktif__2F2FFC0C] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__sss_kateg__olust__30242045] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__sss_kateg__gunce__3118447E] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_sss_kategorileri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sss_kategorileri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_kategorileri', N'kategori_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [kategori_adi] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_kategorileri', N'seo_slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [seo_slug] nvarchar(150) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_kategorileri', N'ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [ikon] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_kategorileri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sss_kategorileri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sss_kategorileri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sss_kategorileri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_kategorileri] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
