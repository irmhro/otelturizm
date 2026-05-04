SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_konum_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_konum_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [user_id] bigint NULL,
        [enlem] decimal(10,7) NOT NULL,
        [boylam] decimal(10,7) NOT NULL,
        [kaynak] nvarchar(50) NULL,
        [kullanici_ajan] nvarchar(500) NULL,
        [ip_adresi] nvarchar(64) NULL,
        [kayit_tarihi] datetime2(0) CONSTRAINT [DF_kullanici_konum_loglari_kayit_tarihi] DEFAULT (sysutcdatetime()) NOT NULL,
        [session_key] nvarchar(120) NULL,
        [yaricap_km] int NULL,
        [gorunen_otel_sayisi] int NULL,
        [arama_metni] nvarchar(250) NULL,
        [arama_bolgesi] nvarchar(200) NULL,
        [cihaz_tipi] nvarchar(50) NULL,
        [cihaz_modeli] nvarchar(120) NULL,
        [platform] nvarchar(80) NULL,
        [tarayici] nvarchar(80) NULL,
        [telefon_ipucu] nvarchar(80) NULL,
        [sayfa_url] nvarchar(500) NULL,
        [listelenen_otel_idleri] nvarchar(max) NULL,
        CONSTRAINT [PK__kullanic__3213E83F921EA3B8] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'enlem') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [enlem] decimal(10,7) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'boylam') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [boylam] decimal(10,7) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'kaynak') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [kaynak] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'kullanici_ajan') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [kullanici_ajan] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [ip_adresi] nvarchar(64) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'kayit_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [kayit_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'session_key') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [session_key] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'yaricap_km') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [yaricap_km] int NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'gorunen_otel_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [gorunen_otel_sayisi] int NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'arama_metni') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [arama_metni] nvarchar(250) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'arama_bolgesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [arama_bolgesi] nvarchar(200) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'cihaz_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [cihaz_tipi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'cihaz_modeli') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [cihaz_modeli] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'platform') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [platform] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'tarayici') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [tarayici] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'telefon_ipucu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [telefon_ipucu] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'sayfa_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [sayfa_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'listelenen_otel_idleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_konum_loglari] ADD [listelenen_otel_idleri] nvarchar(max) NULL;
END
GO
