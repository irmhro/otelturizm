SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.destek_kategorileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[destek_kategorileri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kategori_adi] nvarchar(120) NOT NULL,
        [seo_slug] nvarchar(150) NOT NULL,
        [kategori_ikon] nvarchar(80) NOT NULL,
        [kisa_aciklama] nvarchar(255) NULL,
        [renk_kodu] nvarchar(20) NOT NULL,
        [siralama] int CONSTRAINT [DF__destek_ka__siral__0C85DE4D] DEFAULT ((0)) NOT NULL,
        [durum] bit CONSTRAINT [DF__destek_ka__durum__0D7A0286] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__destek_ka__olust__0E6E26BF] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__destek_ka__gunce__0F624AF8] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_destek_kategorileri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.destek_kategorileri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'kategori_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [kategori_adi] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'seo_slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [seo_slug] nvarchar(150) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'kategori_ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [kategori_ikon] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'kisa_aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [kisa_aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'renk_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [renk_kodu] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [durum] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kategorileri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kategorileri] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
