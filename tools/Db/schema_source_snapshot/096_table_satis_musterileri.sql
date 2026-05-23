SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.satis_musterileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[satis_musterileri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [musteri_kodu] nvarchar(24) NOT NULL,
        [ad_soyad] nvarchar(120) NOT NULL,
        [eposta] nvarchar(100) NULL,
        [telefon] nvarchar(20) NULL,
        [ulke] nvarchar(60) NULL,
        [sehir] nvarchar(100) NULL,
        [ilce] nvarchar(100) NULL,
        [mahalle] nvarchar(120) NULL,
        [adres] nvarchar(max) NULL,
        [uyelik_seviyesi] nvarchar(255) NOT NULL,
        [toplam_rezervasyon_sayisi] int CONSTRAINT [DF__satis_mus__topla__1293BD5E] DEFAULT ((0)) NOT NULL,
        [toplam_harcama] decimal(12,2) CONSTRAINT [DF__satis_mus__topla__1387E197] DEFAULT ((0.00)) NOT NULL,
        [son_rezervasyon_tarihi] date NULL,
        [son_talep_ozeti] nvarchar(255) NULL,
        [pazarlama_izni] bit CONSTRAINT [DF__satis_mus__pazar__147C05D0] DEFAULT ((0)) NOT NULL,
        [notlar] nvarchar(max) NULL,
        [olusturan_sales_user_id] bigint NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__satis_mus__olust__15702A09] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_satis_musterileri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.satis_musterileri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'musteri_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [musteri_kodu] nvarchar(24) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [ad_soyad] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [eposta] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [telefon] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [ulke] nvarchar(60) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [sehir] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [ilce] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'mahalle') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [mahalle] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [adres] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'uyelik_seviyesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [uyelik_seviyesi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'toplam_rezervasyon_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [toplam_rezervasyon_sayisi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'toplam_harcama') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [toplam_harcama] decimal(12,2) DEFAULT ((0.00)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'son_rezervasyon_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [son_rezervasyon_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'son_talep_ozeti') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [son_talep_ozeti] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'pazarlama_izni') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [pazarlama_izni] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [notlar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'olusturan_sales_user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [olusturan_sales_user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musterileri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musterileri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
