SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sozlesme_gonderim_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sozlesme_gonderim_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [sozlesme_id] bigint NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [partner_id] bigint NULL,
        [firma_id] bigint NULL,
        [alici_eposta] nvarchar(255) NOT NULL,
        [gonderim_nedeni] nvarchar(80) NOT NULL,
        [bildirim_log_id] bigint NULL,
        [konu_snapshot] nvarchar(255) NOT NULL,
        [icerik_snapshot] nvarchar(max) NULL,
        [durum] nvarchar(40) CONSTRAINT [DF_sozlesme_gonderim_durum] DEFAULT ('KuyrugaAlindi') NOT NULL,
        [gonderim_tarihi] datetime2(7) CONSTRAINT [DF_sozlesme_gonderim_tarih] DEFAULT (sysutcdatetime()) NOT NULL,
        [ip_adresi] nvarchar(80) NULL,
        [user_agent] nvarchar(500) NULL,
        [olusturan_admin_id] bigint NULL,
        CONSTRAINT [PK__sozlesme__3213E83F87A4278B] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'sozlesme_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [sozlesme_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [partner_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'alici_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [alici_eposta] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'gonderim_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [gonderim_nedeni] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'bildirim_log_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [bildirim_log_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'konu_snapshot') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [konu_snapshot] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'icerik_snapshot') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [icerik_snapshot] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [durum] nvarchar(40) DEFAULT ('KuyrugaAlindi') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'gonderim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [gonderim_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [ip_adresi] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [user_agent] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_gonderim_loglari', N'olusturan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] ADD [olusturan_admin_id] bigint NULL;
END
GO
