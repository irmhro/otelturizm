SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oda_gorselleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oda_gorselleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [oda_tip_id] bigint NOT NULL,
        [gorsel_url] nvarchar(500) NOT NULL,
        [thumbnail_url] nvarchar(500) NULL,
        [baslik] nvarchar(200) NULL,
        [aciklama] nvarchar(max) NULL,
        [kapak_fotografi_mi] bit CONSTRAINT [DF__oda_gorse__kapak__57A801BA] DEFAULT ((0)) NULL,
        [siralama] smallint CONSTRAINT [DF__oda_gorse__siral__589C25F3] DEFAULT ((0)) NULL,
        [boyut_kb] int NULL,
        [onay_durumu] nvarchar(255) NULL,
        [onaylayan_admin_id] bigint NULL,
        [onay_tarihi] datetime2(0) NULL,
        [yukleyen_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__oda_gorse__olust__59904A2C] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_oda_gorselleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oda_gorselleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'oda_tip_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [oda_tip_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'gorsel_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [gorsel_url] nvarchar(500) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'thumbnail_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [thumbnail_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [baslik] nvarchar(200) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [aciklama] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'kapak_fotografi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [kapak_fotografi_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [siralama] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'boyut_kb') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [boyut_kb] int NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [onay_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [onaylayan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'yukleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [yukleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_gorselleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_gorselleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
