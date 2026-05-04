SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_bildirim_tercihleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_bildirim_tercihleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [rezervasyon_eposta] bit CONSTRAINT [DF__kullanici__rezer__72910220] DEFAULT ((1)) NOT NULL,
        [rezervasyon_push] bit CONSTRAINT [DF__kullanici__rezer__73852659] DEFAULT ((1)) NOT NULL,
        [checkin_hatirlatma] bit CONSTRAINT [DF__kullanici__check__74794A92] DEFAULT ((1)) NOT NULL,
        [iptal_degisim] bit CONSTRAINT [DF__kullanici__iptal__756D6ECB] DEFAULT ((1)) NOT NULL,
        [kampanya_eposta] bit CONSTRAINT [DF__kullanici__kampa__76619304] DEFAULT ((0)) NOT NULL,
        [kampanya_sms] bit CONSTRAINT [DF__kullanici__kampa__7755B73D] DEFAULT ((0)) NOT NULL,
        [sistem_bildirimi] bit CONSTRAINT [DF__kullanici__siste__7849DB76] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__793DFFAF] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_kullanici_bildirim_tercihleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'rezervasyon_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [rezervasyon_eposta] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'rezervasyon_push') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [rezervasyon_push] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'checkin_hatirlatma') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [checkin_hatirlatma] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'iptal_degisim') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [iptal_degisim] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'kampanya_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [kampanya_eposta] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'kampanya_sms') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [kampanya_sms] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'sistem_bildirimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [sistem_bildirimi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'giris_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [giris_eposta] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
