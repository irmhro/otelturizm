SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[firma_oda_fiyat_musaitlik]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [oda_tip_id] bigint NOT NULL,
        [tarih] date NOT NULL,
        [firma_gecelik_fiyat] decimal(10,2) NOT NULL,
        [minimum_geceleme] tinyint NULL,
        [maksimum_geceleme] smallint NULL,
        [kapali_satis] bit CONSTRAINT [DF_firma_ofm_kapali] DEFAULT ((0)) NOT NULL,
        [fiyat_notu] nvarchar(255) NULL,
        [guncelleyen_kullanici_id] bigint NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_firma_ofm_guncellenme] DEFAULT (sysutcdatetime()) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_firma_ofm_aktif] DEFAULT ((1)) NOT NULL,
        CONSTRAINT [PK__firma_od__3213E83F233B799D] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'oda_tip_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [oda_tip_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'tarih') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [tarih] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'firma_gecelik_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [firma_gecelik_fiyat] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'minimum_geceleme') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [minimum_geceleme] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'maksimum_geceleme') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [maksimum_geceleme] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'kapali_satis') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [kapali_satis] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'fiyat_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [fiyat_notu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'guncelleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [guncelleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_oda_fiyat_musaitlik', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
