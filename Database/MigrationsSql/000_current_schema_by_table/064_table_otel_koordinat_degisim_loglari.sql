SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_koordinat_degisim_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_koordinat_degisim_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [admin_kullanici_id] bigint NOT NULL,
        [admin_ad_soyad] nvarchar(160) NULL,
        [otel_id] bigint NOT NULL,
        [otel_adi] nvarchar(250) NULL,
        [onceki_enlem] decimal(10,7) NULL,
        [onceki_boylam] decimal(10,7) NULL,
        [yeni_enlem] decimal(10,7) NULL,
        [yeni_boylam] decimal(10,7) NULL,
        [ip_adresi] nvarchar(64) NULL,
        [notlar] nvarchar(500) NULL,
        [kayit_tarihi] datetime2(7) CONSTRAINT [DF_otel_koordinat_degisim_loglari_kayit_tarihi] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__otel_koo__3213E83F0DD809D4] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'admin_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [admin_kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'admin_ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [admin_ad_soyad] nvarchar(160) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'otel_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [otel_adi] nvarchar(250) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'onceki_enlem') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [onceki_enlem] decimal(10,7) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'onceki_boylam') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [onceki_boylam] decimal(10,7) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'yeni_enlem') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [yeni_enlem] decimal(10,7) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'yeni_boylam') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [yeni_boylam] decimal(10,7) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [ip_adresi] nvarchar(64) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [notlar] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_koordinat_degisim_loglari', N'kayit_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_koordinat_degisim_loglari] ADD [kayit_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
