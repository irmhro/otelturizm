SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oda_fiyat_musaitlik', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oda_fiyat_musaitlik]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [oda_tip_id] bigint NOT NULL,
        [otel_id] bigint NOT NULL,
        [tarih] date NOT NULL,
        [gecelik_fiyat] decimal(10,2) NOT NULL,
        [indirimli_fiyat] decimal(10,2) NULL,
        [kampanya_id] bigint NULL,
        [toplam_oda_sayisi] smallint NOT NULL,
        [satilan_oda_sayisi] smallint CONSTRAINT [DF__oda_fiyat__satil__4F12BBB9] DEFAULT ((0)) NULL,
        [bloke_oda_sayisi] smallint CONSTRAINT [DF__oda_fiyat__bloke__5006DFF2] DEFAULT ((0)) NULL,
        [minimum_geceleme] tinyint CONSTRAINT [DF__oda_fiyat__minim__50FB042B] DEFAULT ((1)) NULL,
        [maksimum_geceleme] smallint CONSTRAINT [DF__oda_fiyat__maksi__51EF2864] DEFAULT ((30)) NULL,
        [kapali_satis] bit CONSTRAINT [DF__oda_fiyat__kapal__52E34C9D] DEFAULT ((0)) NULL,
        [kampanya_etiketi] nvarchar(120) NULL,
        [fiyat_notu] nvarchar(255) NULL,
        [guncelleyen_kullanici_id] bigint NULL,
        [sadece_gunubirlik] bit CONSTRAINT [DF__oda_fiyat__sadec__53D770D6] DEFAULT ((0)) NULL,
        [iptal_politikasi_override] nvarchar(max) NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__oda_fiyat__gunce__54CB950F] DEFAULT (sysutcdatetime()) NULL,
        [indirim_id] bigint NULL,
        CONSTRAINT [PK_oda_fiyat_musaitlik] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'oda_tip_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [oda_tip_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'tarih') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [tarih] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'gecelik_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [gecelik_fiyat] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'indirimli_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [indirimli_fiyat] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'kampanya_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [kampanya_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'toplam_oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [toplam_oda_sayisi] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'satilan_oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [satilan_oda_sayisi] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'bloke_oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [bloke_oda_sayisi] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'minimum_geceleme') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [minimum_geceleme] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'maksimum_geceleme') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [maksimum_geceleme] smallint DEFAULT ((30)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'kapali_satis') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [kapali_satis] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'kampanya_etiketi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [kampanya_etiketi] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'fiyat_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [fiyat_notu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'guncelleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [guncelleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'sadece_gunubirlik') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [sadece_gunubirlik] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'iptal_politikasi_override') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [iptal_politikasi_override] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_fiyat_musaitlik', N'indirim_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] ADD [indirim_id] bigint NULL;
END
GO
