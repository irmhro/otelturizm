SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.ozel_tarih_tanimlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ozel_tarih_tanimlari]
    (
        [id] int IDENTITY(1,1) NOT NULL,
        [tur] nvarchar(255) NOT NULL,
        [ad] nvarchar(100) NOT NULL,
        [baslangic_tarihi] date NOT NULL,
        [bitis_tarihi] date NOT NULL,
        [tekrar_eder_mi] bit CONSTRAINT [DF__ozel_tari__tekra__3BCADD1B] DEFAULT ((0)) NULL,
        [tekrar_kurali] nvarchar(255) NULL,
        [ulke] nvarchar(50) NULL,
        [sehir] nvarchar(50) NULL,
        [fiyat_carpani] decimal(4,2) CONSTRAINT [DF__ozel_tari__fiyat__3CBF0154] DEFAULT ((1.00)) NULL,
        [minimum_geceleme_kurali] tinyint NULL,
        [aciklama] nvarchar(255) NULL,
        [aktif_mi] bit CONSTRAINT [DF__ozel_tari__aktif__3DB3258D] DEFAULT ((1)) NULL,
        CONSTRAINT [PK_ozel_tarih_tanimlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'tur') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [tur] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [ad] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [baslangic_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [bitis_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'tekrar_eder_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [tekrar_eder_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'tekrar_kurali') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [tekrar_kurali] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [ulke] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [sehir] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'fiyat_carpani') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [fiyat_carpani] decimal(4,2) DEFAULT ((1.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'minimum_geceleme_kurali') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [minimum_geceleme_kurali] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.ozel_tarih_tanimlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[ozel_tarih_tanimlari] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
