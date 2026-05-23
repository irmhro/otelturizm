SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ulkeler', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ulkeler]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [ulke_adi] nvarchar(150) NOT NULL,
        [iso2_kodu] nchar(2) NULL,
        [iso3_kodu] nchar(3) NULL,
        [telefon_kodu] nvarchar(10) NULL,
        [para_birimi_kodu] nvarchar(10) NULL,
        [varsayilan_ulke] bit CONSTRAINT [DF__ulkeler__varsayi__693CA210] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__ulkeler__aktif_m__6A30C649] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__ulkeler__olustur__6B24EA82] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_ulkeler] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.ulkeler', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'ulke_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [ulke_adi] nvarchar(150) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'iso2_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [iso2_kodu] nchar(2) NULL;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'iso3_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [iso3_kodu] nchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'telefon_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [telefon_kodu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'para_birimi_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [para_birimi_kodu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'varsayilan_ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [varsayilan_ulke] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.ulkeler', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ulkeler] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
