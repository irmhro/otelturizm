SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_giris_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_giris_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [hesap_tipi] nvarchar(20) CONSTRAINT [DF_kullanici_giris_loglari_hesap] DEFAULT ('user') NOT NULL,
        [ip_adresi] nvarchar(80) NULL,
        [user_agent] nvarchar(500) NULL,
        [cihaz_etiketi] nvarchar(150) NULL,
        [giris_tarihi] datetime2(7) CONSTRAINT [DF_kullanici_giris_loglari_giris] DEFAULT (sysutcdatetime()) NOT NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_kullanici_giris_loglari_created] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__kullanic__3213E83FAD9C627A] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'hesap_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [hesap_tipi] nvarchar(20) DEFAULT ('user') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [ip_adresi] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [user_agent] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'cihaz_etiketi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [cihaz_etiketi] nvarchar(150) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'giris_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [giris_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_loglari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
