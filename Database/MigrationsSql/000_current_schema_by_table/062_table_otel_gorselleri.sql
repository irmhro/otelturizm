SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_gorselleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_gorselleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [gorsel_url] nvarchar(500) NOT NULL,
        [thumbnail_url] nvarchar(500) NULL,
        [gorsel_turu] nvarchar(255) NOT NULL,
        [baslik] nvarchar(200) NULL,
        [aciklama] nvarchar(max) NULL,
        [kapak_fotografi_mi] bit CONSTRAINT [DF__otel_gors__kapak__7CD98669] DEFAULT ((0)) NULL,
        [one_cikan] bit CONSTRAINT [DF__otel_gors__one_c__7DCDAAA2] DEFAULT ((0)) NULL,
        [siralama] smallint CONSTRAINT [DF__otel_gors__siral__7EC1CEDB] DEFAULT ((0)) NULL,
        [boyut_kb] int NULL,
        [genislik] smallint NULL,
        [yukseklik] smallint NULL,
        [onay_durumu] nvarchar(255) NULL,
        [onaylayan_admin_id] bigint NULL,
        [onay_tarihi] datetime2(0) NULL,
        [yukleyen_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__otel_gors__olust__7FB5F314] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_otel_gorselleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_gorselleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'gorsel_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [gorsel_url] nvarchar(500) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'thumbnail_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [thumbnail_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'gorsel_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [gorsel_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [baslik] nvarchar(200) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [aciklama] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'kapak_fotografi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [kapak_fotografi_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'one_cikan') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [one_cikan] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [siralama] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'boyut_kb') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [boyut_kb] int NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'genislik') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [genislik] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'yukseklik') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [yukseklik] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [onay_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [onaylayan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'yukleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [yukleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_gorselleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_gorselleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
