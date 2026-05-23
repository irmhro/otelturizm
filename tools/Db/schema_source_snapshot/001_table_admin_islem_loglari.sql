SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.admin_islem_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[admin_islem_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [admin_kullanici_id] bigint NOT NULL,
        [islem_turu] nvarchar(255) NOT NULL,
        [hedef_tablo] nvarchar(50) NOT NULL,
        [hedef_kayit_id] bigint NULL,
        [onceki_deger] nvarchar(max) NULL,
        [yeni_deger] nvarchar(max) NULL,
        [degisiklik_ozeti] nvarchar(max) NULL,
        [islem_nedeni] nvarchar(500) NULL,
        [islem_notu] nvarchar(max) NULL,
        [ip_adresi] nvarchar(45) NOT NULL,
        [islem_tarihi] datetime2(0) CONSTRAINT [DF__admin_isl__islem__6FE99F9F] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_admin_islem_loglari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'admin_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [admin_kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'islem_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [islem_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'hedef_tablo') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [hedef_tablo] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'hedef_kayit_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [hedef_kayit_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'onceki_deger') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [onceki_deger] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'yeni_deger') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [yeni_deger] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'degisiklik_ozeti') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [degisiklik_ozeti] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'islem_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [islem_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'islem_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [islem_notu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [ip_adresi] nvarchar(45) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.admin_islem_loglari', N'islem_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[admin_islem_loglari] ADD [islem_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
