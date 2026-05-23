SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sozlesme_kabulleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sozlesme_kabulleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [sozlesme_id] bigint NOT NULL,
        [kabul_eden_tip] nvarchar(30) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [partner_id] bigint NULL,
        [firma_id] bigint NULL,
        [alici_eposta] nvarchar(255) NOT NULL,
        [sozlesme_baslik_snapshot] nvarchar(200) NOT NULL,
        [sozlesme_versiyon_snapshot] int NOT NULL,
        [kabul_kaynagi] nvarchar(80) NOT NULL,
        [kabul_ip] nvarchar(80) NULL,
        [kabul_user_agent] nvarchar(500) NULL,
        [eposta_dogrulandi_mi] bit CONSTRAINT [DF_sozlesme_kabulleri_eposta] DEFAULT ((0)) NOT NULL,
        [eposta_dogrulama_tarihi] datetime2(7) NULL,
        [kabul_tarihi] datetime2(7) CONSTRAINT [DF_sozlesme_kabulleri_tarih] DEFAULT (sysutcdatetime()) NOT NULL,
        [sona_erme_tarihi] datetime2(7) NULL,
        [durum] nvarchar(40) CONSTRAINT [DF_sozlesme_kabulleri_durum] DEFAULT ('KabulEdildi') NOT NULL,
        CONSTRAINT [PK__sozlesme__3213E83F471D909E] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'sozlesme_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [sozlesme_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'kabul_eden_tip') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [kabul_eden_tip] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [partner_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'alici_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [alici_eposta] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'sozlesme_baslik_snapshot') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [sozlesme_baslik_snapshot] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'sozlesme_versiyon_snapshot') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [sozlesme_versiyon_snapshot] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'kabul_kaynagi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [kabul_kaynagi] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'kabul_ip') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [kabul_ip] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'kabul_user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [kabul_user_agent] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'eposta_dogrulandi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [eposta_dogrulandi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'eposta_dogrulama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [eposta_dogrulama_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'kabul_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [kabul_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'sona_erme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [sona_erme_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_kabulleri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] ADD [durum] nvarchar(40) DEFAULT ('KabulEdildi') NOT NULL;
END
GO
