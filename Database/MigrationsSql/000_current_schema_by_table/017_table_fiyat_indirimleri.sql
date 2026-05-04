SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.fiyat_indirimleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[fiyat_indirimleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [indirim_adi] nvarchar(140) NOT NULL,
        [kisa_aciklama] nvarchar(220) NULL,
        [detay_html] nvarchar(max) NULL,
        [gorsel_url] nvarchar(500) NULL,
        [ikon_class] nvarchar(80) NULL,
        [renk_kodu] nvarchar(30) NULL,
        [aktif_mi] bit CONSTRAINT [DF_fiyat_indirimleri_aktif] DEFAULT ((1)) NOT NULL,
        [siralama] smallint CONSTRAINT [DF_fiyat_indirimleri_siralama] DEFAULT ((100)) NOT NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_fiyat_indirimleri_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_fiyat_indirimleri_guncellenme] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__fiyat_in__3213E83F93559AF3] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'indirim_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [indirim_adi] nvarchar(140) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'kisa_aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [kisa_aciklama] nvarchar(220) NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'detay_html') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [detay_html] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'gorsel_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [gorsel_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'ikon_class') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [ikon_class] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'renk_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [renk_kodu] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [siralama] smallint DEFAULT ((100)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.fiyat_indirimleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[fiyat_indirimleri] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
