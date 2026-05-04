SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_aktivite_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_aktivite_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [aktivite_turu] nvarchar(255) NOT NULL,
        [aktivite_detayi] nvarchar(max) NULL,
        [ip_adresi] nvarchar(45) NOT NULL,
        [user_agent] nvarchar(max) NULL,
        [cihaz_turu] nvarchar(255) NULL,
        [isletim_sistemi] nvarchar(50) NULL,
        [tarayici] nvarchar(50) NULL,
        [ulke] nvarchar(50) NULL,
        [sehir] nvarchar(50) NULL,
        [session_id] nvarchar(100) NULL,
        [basarili_mi] bit CONSTRAINT [DF__kullanici__basar__6AEFE058] DEFAULT ((1)) NULL,
        [hata_nedeni] nvarchar(255) NULL,
        [olusma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olusm__6BE40491] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_kullanici_aktivite_loglari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'aktivite_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [aktivite_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'aktivite_detayi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [aktivite_detayi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [ip_adresi] nvarchar(45) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [user_agent] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'cihaz_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [cihaz_turu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'isletim_sistemi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [isletim_sistemi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'tarayici') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [tarayici] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [ulke] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [sehir] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'session_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [session_id] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'basarili_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [basarili_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'hata_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [hata_nedeni] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_aktivite_loglari', N'olusma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_aktivite_loglari] ADD [olusma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
