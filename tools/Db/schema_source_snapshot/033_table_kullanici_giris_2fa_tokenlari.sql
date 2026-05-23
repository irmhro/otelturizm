SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_giris_2fa_tokenlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_giris_2fa_tokenlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [telefon_e164] nvarchar(32) NOT NULL,
        [dogrulama_kodu_hash] nvarchar(128) NOT NULL,
        [deneme_sayisi] smallint CONSTRAINT [DF_kullanici_giris_2fa_deneme] DEFAULT ((0)) NOT NULL,
        [maksimum_deneme] smallint CONSTRAINT [DF_kullanici_giris_2fa_max] DEFAULT ((5)) NOT NULL,
        [kullanildi_mi] bit CONSTRAINT [DF_kullanici_giris_2fa_kullanildi] DEFAULT ((0)) NOT NULL,
        [kullanilma_tarihi] datetime2(7) NULL,
        [gecerlilik_suresi] datetime2(7) NOT NULL,
        [ip_adresi] nvarchar(80) NULL,
        [user_agent] nvarchar(500) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_kullanici_giris_2fa_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_kullanici_giris_2fa_updated] DEFAULT (sysutcdatetime()) NOT NULL,
        [kanal] nvarchar(20) CONSTRAINT [DF_kullanici_giris_2fa_kanal] DEFAULT (N'whatsapp') NOT NULL,
        [eposta] nvarchar(255) NULL,
        CONSTRAINT [PK__kullanic__3213E83F6F73BABC] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'telefon_e164') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [telefon_e164] nvarchar(32) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'dogrulama_kodu_hash') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [dogrulama_kodu_hash] nvarchar(128) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'deneme_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [deneme_sayisi] smallint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'maksimum_deneme') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [maksimum_deneme] smallint DEFAULT ((5)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'kullanildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [kullanildi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'kullanilma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [kullanilma_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'gecerlilik_suresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [gecerlilik_suresi] datetime2(7) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [ip_adresi] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [user_agent] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'kanal') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [kanal] nvarchar(20) DEFAULT (N'whatsapp') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_giris_2fa_tokenlari', N'eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] ADD [eposta] nvarchar(255) NULL;
END
GO
